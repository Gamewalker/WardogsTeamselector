using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using WardogsTeamselector.Core;

namespace WardogsTeamselector.Automation;

/// <summary>Create, Apply and Dispose on the owning WPF window's UI thread. The stop key is observed and forwarded unchanged.</summary>
public sealed class HotkeyService : IDisposable
{
    private readonly Window window;
    private readonly IntPtr handle;
    private readonly HwndSource source;
    private readonly HookProc hookProc;
    private readonly Dictionary<int, Team> registered = new();
    private IntPtr hook;
    private bool stopDown;
    private int stopKey = 0x1B;
    private bool disposed;
    public event Action<Team>? TeamPressed;
    public event Action? StopPressed;

    public HotkeyService(Window window)
    {
        this.window = window;
        window.Dispatcher.VerifyAccess();
        handle = new WindowInteropHelper(window).EnsureHandle();
        source = HwndSource.FromHwnd(handle) ?? throw new InvalidOperationException("Fensterhandle konnte nicht erstellt werden.");
        source.AddHook(WindowProc);
        hookProc = KeyboardProc;
        hook = SetWindowsHookEx(13, hookProc, GetModuleHandle(null), 0);
        if (hook == IntPtr.Zero)
        {
            source.RemoveHook(WindowProc);
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Globale Stopptaste konnte nicht eingerichtet werden.");
        }
    }

    public void Apply(AppSettings settings)
    {
        window.Dispatcher.VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        settings.Validate();
        stopKey = settings.StopHotkey; stopDown = false;
        UnregisterAll();
        foreach (var team in Enum.GetValues<Team>())
        {
            var id = 0x5740 + (int)team;
            var key = settings.Hotkeys[team];
            if (!RegisterHotKey(handle, id, 0x4000, (uint)key))
            {
                var error = Marshal.GetLastWin32Error();
                UnregisterAll();
                throw new Win32Exception(error, $"{HotkeyChoice.Display(key)} für {team} konnte nicht registriert werden (bereits belegt?). Teamhotkeys sind deaktiviert; andere Tasten wählen und erneut anwenden. Stopp: {HotkeyChoice.Display(stopKey)} bleibt aktiv.");
            }
            registered.Add(id, team);
        }
    }

    private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x0312 && registered.TryGetValue(wParam.ToInt32(), out var team))
        {
            handled = true;
            try { TeamPressed?.Invoke(team); } catch { }
        }
        return IntPtr.Zero;
    }

    private IntPtr KeyboardProc(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && Marshal.ReadInt32(lParam) == stopKey)
        {
            var msg = wParam.ToInt32();
            if (msg is 0x0101 or 0x0105) stopDown = false;
            else if (msg is 0x0100 or 0x0104 && !stopDown)
            {
                stopDown = true;
                try { StopPressed?.Invoke(); } catch { }
            }
        }
        return CallNextHookEx(hook, code, wParam, lParam);
    }

    private void UnregisterAll()
    {
        foreach (var id in registered.Keys) UnregisterHotKey(handle, id);
        registered.Clear();
    }

    public void Dispose()
    {
        window.Dispatcher.VerifyAccess();
        if (disposed) return;
        disposed = true;
        UnregisterAll();
        if (hook != IntPtr.Zero) { UnhookWindowsHookEx(hook); hook = IntPtr.Zero; }
        source.RemoveHook(WindowProc);
        GC.KeepAlive(hookProc);
    }

    private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int id, HookProc callback, IntPtr module, uint thread);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr GetModuleHandle(string? name);
}
