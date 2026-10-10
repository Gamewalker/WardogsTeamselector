using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using WardogsTeamselector.Core;

namespace WardogsTeamselector.Platform;

/// <summary>All bounds are physical desktop pixels (the application is PerMonitorV2 aware).</summary>
public sealed class ScreenService : IScreenService
{
    public IReadOnlyList<MonitorInfo> GetMonitors() => Screen.AllScreens
        .Select(s => new MonitorInfo(s.DeviceName, s.DeviceName + (s.Primary ? " (Hauptmonitor)" : ""), s.Bounds, s.Primary)).ToArray();

    public TargetGeometry? ResolveTarget(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.WindowTitleContains) || string.IsNullOrWhiteSpace(settings.ProcessNameContains)) return null;
        var candidates = FindGameWindows(settings.WindowTitleContains, settings.ProcessNameContains);
        var foreground = GetForegroundWindow();
        var monitors = GetMonitors();
        foreach (var candidate in candidates.OrderByDescending(c => c.Handle == foreground).ThenByDescending(c => (long)c.Bounds.Width * c.Bounds.Height))
        {
            var geometry = ResolveGeometry(candidate, settings, foreground, monitors);
            if (geometry != null) return geometry;
        }
        return null;
    }

    public TargetGeometry? RevalidateTarget(TargetGeometry target, AppSettings settings)
    {
        var candidate = ReadGameWindow(target.WindowHandle, settings.WindowTitleContains, settings.ProcessNameContains);
        return candidate == null ? null : ResolveGeometry(candidate, settings, GetForegroundWindow(), GetMonitors());
    }

    private static TargetGeometry? ResolveGeometry(GameWindow candidate, AppSettings settings, IntPtr foreground, IReadOnlyList<MonitorInfo> monitors)
    {
        MonitorInfo? selected = null;
        if (!string.IsNullOrEmpty(settings.MonitorId))
        {
            selected = monitors.FirstOrDefault(m => m.Id == settings.MonitorId);
            if (selected is null) return null; // Never silently substitute another monitor.
        }

        var bounds = candidate.Bounds;
        if (settings.ManualBounds is Rectangle manual)
        {
            if (!bounds.Contains(manual) || (selected is not null && !selected.Bounds.Contains(manual))) return null;
            bounds = manual;
        }
        else if (selected is not null)
        {
            // Binding chooses the display; scaling still uses the game's actual
            // client area, including in ordinary windowed mode.
            if (!selected.Bounds.Contains(bounds)) return null;
        }
        if (bounds.Width < 100 || bounds.Height < 100 || !IsOnVisibleDesktop(bounds, monitors)) return null;
        var ratio = (double)bounds.Width / bounds.Height;
        var calibrated = settings.GeometryCalibrated || Math.Abs(ratio - 16d / 9) < 0.025;
        return new TargetGeometry(bounds, candidate.Handle, candidate.Handle == foreground,
            $"{candidate.Title} · {bounds.Width}×{bounds.Height} · ({bounds.X}, {bounds.Y})", calibrated);
    }

    public CaptureFrame Capture(TargetGeometry target)
    {
        if (!IsWindow(target.WindowHandle) || !IsWindowVisible(target.WindowHandle) || IsIconic(target.WindowHandle))
            throw new InvalidOperationException("Spielfenster ist nicht mehr sichtbar.");
        if (target.Bounds.Width < 1 || target.Bounds.Height < 1 || !IsOnVisibleDesktop(target.Bounds, GetMonitors()))
            throw new InvalidOperationException("Spielbereich liegt außerhalb der sichtbaren Monitore.");
        var bitmap = new Bitmap(target.Bounds.Width, target.Bounds.Height, PixelFormat.Format32bppArgb);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(target.Bounds.Location, Point.Empty, target.Bounds.Size, CopyPixelOperation.SourceCopy);
            return new CaptureFrame(bitmap, target);
        }
        catch { bitmap.Dispose(); throw; }
    }

    public bool TryBringGameToForeground(AppSettings settings)
    {
        var target = ResolveTarget(settings);
        if (target is null)
        {
            // ResolveTarget deliberately excludes minimized windows from capture.
            // Restore matching game windows only for this explicit activation.
            if (string.IsNullOrWhiteSpace(settings.WindowTitleContains) || string.IsNullOrWhiteSpace(settings.ProcessNameContains)) return false;
            foreach (var candidate in FindGameWindows(settings.WindowTitleContains, settings.ProcessNameContains, includeMinimized: true).Where(c => IsIconic(c.Handle)))
            {
                ShowWindow(candidate.Handle, 9); // SW_RESTORE
                target = ResolveTarget(settings);
                if (target is not null) break;
            }
        }
        if (target is null) return false;
        if (GetForegroundWindow() == target.WindowHandle) return true;
        // Respect Windows foreground restrictions; never synthesize input to bypass them.
        return SetForegroundWindow(target.WindowHandle) && GetForegroundWindow() == target.WindowHandle;
    }

    private static bool IsOnVisibleDesktop(Rectangle bounds, IReadOnlyList<MonitorInfo> monitors)
    {
        // The virtual bounding rectangle alone may contain gaps between monitors.
        using var uncovered = new Region(bounds);
        foreach (var monitor in monitors) uncovered.Exclude(monitor.Bounds);
        using var transform = new System.Drawing.Drawing2D.Matrix();
        return uncovered.GetRegionScans(transform).Length == 0;
    }

    private static List<GameWindow> FindGameWindows(string titleFilter, string processFilter, bool includeMinimized = false)
    {
        var result = new List<GameWindow>();
        EnumWindows((window, _) =>
        {
            var candidate = ReadGameWindow(window, titleFilter, processFilter, includeMinimized);
            if (candidate != null) result.Add(candidate);
            return true;
        }, IntPtr.Zero);
        return result;
    }

    private static GameWindow? ReadGameWindow(IntPtr window, string titleFilter, string processFilter, bool includeMinimized = false)
    {
        if (string.IsNullOrWhiteSpace(titleFilter) || string.IsNullOrWhiteSpace(processFilter)
            || !IsWindow(window) || !IsWindowVisible(window) || (!includeMinimized && IsIconic(window))) return null;
        GetWindowThreadProcessId(window, out var processId);
        if (processId == (uint)Environment.ProcessId) return null;
        var titleLength = GetWindowTextLength(window);
        if (titleLength <= 0) return null;
        var text = new StringBuilder(titleLength + 1);
        GetWindowText(window, text, text.Capacity);
        var title = text.ToString();
        if (!title.Contains(titleFilter, StringComparison.OrdinalIgnoreCase)) return null;
        try
        {
            using var process = Process.GetProcessById((int)processId);
            if (!process.ProcessName.Contains(processFilter, StringComparison.OrdinalIgnoreCase)) return null;
        }
        catch (ArgumentException) { return null; }
        catch (InvalidOperationException) { return null; }
        catch (System.ComponentModel.Win32Exception) { return null; }
        if (includeMinimized && IsIconic(window)) return new(window, title, Rectangle.Empty);
        if (!GetClientRect(window, out var client)) return null;
        var origin = new NativePoint();
        if (!ClientToScreen(window, ref origin)) return null;
        var bounds = new Rectangle(origin.X, origin.Y, client.Right - client.Left, client.Bottom - client.Top);
        return bounds.Width >= 100 && bounds.Height >= 100 ? new(window, title, bounds) : null;
    }

    private sealed record GameWindow(IntPtr Handle, string Title, Rectangle Bounds);
    private delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left; public int Top; public int Right; public int Bottom; }
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextLength(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int maximum);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetClientRect(IntPtr window, out NativeRect rectangle);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ClientToScreen(IntPtr window, ref NativePoint point);
}
