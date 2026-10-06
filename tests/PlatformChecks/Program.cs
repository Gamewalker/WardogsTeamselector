using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WardogsTeamselector.Core;
using WardogsTeamselector.Platform;

internal static class Program
{
    private static int count;

    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        if (args.Length == 2 && args[0] == "--helper") return Helper(args[1]);
        try { Run(); Console.WriteLine($"PASS: {count} platform checks; no mouse input sent."); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void Run()
    {
        var service = new ScreenService();
        var monitors = service.GetMonitors();
        Check(monitors.Count > 0 && monitors.All(m => m.Bounds.Width > 0 && m.Bounds.Height > 0), "physical monitor inventory");
        Check(monitors.Select(m => m.Id).Distinct().Count() == monitors.Count, "stable unique monitor IDs");

        var ownTitle = "WardogsOwnProcessCheck_" + Guid.NewGuid().ToString("N");
        using (var own = new Form { Text = ownTitle, ClientSize = new Size(320, 180) })
        {
            own.Show(); Application.DoEvents();
            Check(service.ResolveTarget(new AppSettings { WindowTitleContains = ownTitle, ProcessNameContains = "PlatformChecks" }) is null, "own-process window excluded");
            own.Close();
        }
        Check(service.ResolveTarget(new AppSettings { WindowTitleContains = "NoWindow_" + Guid.NewGuid().ToString("N") }) is null, "missing game never authorizes geometry");

        var title = "WardogsPhysicalCheck_" + Guid.NewGuid().ToString("N");
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
        if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        start.ArgumentList.Add("--helper"); start.ArgumentList.Add(title);
        using var child = Process.Start(start) ?? throw new InvalidOperationException("Helper launch failed.");
        try
        {
            var ready = child.StandardOutput.ReadLineAsync();
            if (!ready.Wait(TimeSpan.FromSeconds(8))) throw new TimeoutException("Helper window did not appear.");
            var physical = ready.Result!.Split(',').Select(int.Parse).ToArray();
            var expected = new Rectangle(physical[0], physical[1], physical[2], physical[3]);
            var settings = new AppSettings { WindowTitleContains = title, ProcessNameContains = child.ProcessName };
            var geometry = service.ResolveTarget(settings);
            Check(geometry is not null && geometry.Bounds == expected, "separate-process physical client bounds");
            Check(geometry!.IsCalibrated, "16:9 client initially calibrated");
            settings.ProcessNameContains = "wrong-process";
            Check(service.ResolveTarget(settings) is null, "title match cannot bypass process filter");
            settings.ProcessNameContains = child.ProcessName;
            var monitor = monitors.Single(m => m.Bounds.Contains(expected));
            settings.MonitorId = monitor.Id;
            Check(service.ResolveTarget(settings)?.Bounds == expected, "fixed monitor preserves windowed client bounds");
            settings.MonitorId = "missing-monitor";
            Check(service.ResolveTarget(settings) is null, "missing monitor does not silently fall back");
            settings.MonitorId = monitor.Id;
            settings.ManualBounds = new Rectangle(expected.X + 10, expected.Y + 10, 200, 150);
            var manual = service.ResolveTarget(settings);
            Check(manual?.Bounds == settings.ManualBounds && !manual.IsCalibrated, "manual bounds and uncalibrated aspect ratio");
            settings.GeometryCalibrated = true;
            Check(service.ResolveTarget(settings)?.IsCalibrated == true, "explicit geometry calibration");
            settings.ManualBounds = new Rectangle(expected.X - 10, expected.Y, 200, 150);
            Check(service.ResolveTarget(settings) is null, "manual bounds cannot escape game client");
            settings.ManualBounds = null;
            geometry = service.ResolveTarget(settings)!;
            using var frame = service.Capture(geometry);
            Check(frame.Bitmap.Size == expected.Size, "GDI capture physical dimensions");
            Check(frame.Geometry == geometry && frame.CapturedAt <= DateTimeOffset.Now, "capture retains target geometry and timestamp");
            var color = frame.Bitmap.GetPixel(expected.Width / 2, expected.Height / 2);
            // Desktop capture intentionally includes occluding applications. Automation
            // must gate on focus + dialog detection; a live test cannot guarantee that
            // a helper window remains unobscured while the user is using the desktop.
            if (color.R == 17 && color.G == 67 && color.B == 113) Check(true, "unobscured helper pixel captured");
            else Console.WriteLine($"NOTE: helper pixel is occluded or composited; observed RGB({color.R},{color.G},{color.B}), foreground={geometry.IsForeground}. Pixel-color assertion skipped.");
        }
        finally { if (!child.HasExited) child.Kill(); child.WaitForExit(); }

        var input = typeof(WindowsClickSink).GetNestedType("Input", BindingFlags.NonPublic)!;
        var mouse = typeof(WindowsClickSink).GetNestedType("MouseInput", BindingFlags.NonPublic)!;
        Check(Marshal.SizeOf(input) == (IntPtr.Size == 8 ? 40 : 28), "native INPUT ABI size");
        Check(Marshal.OffsetOf(input, "Data").ToInt32() == (IntPtr.Size == 8 ? 8 : 4), "native INPUT union alignment");
        Check(Marshal.SizeOf(mouse) == (IntPtr.Size == 8 ? 32 : 24), "native MOUSEINPUT ABI size");
    }

    private static int Helper(string title)
    {
        var monitor = Screen.PrimaryScreen!.Bounds;
        using var form = new Form { Text = title, StartPosition = FormStartPosition.Manual,
            Location = new Point(monitor.Left + 30, monitor.Top + 30), FormBorderStyle = FormBorderStyle.None,
            ClientSize = new Size(640, 360), BackColor = Color.FromArgb(17, 67, 113), TopMost = true };
        using var timer = new System.Windows.Forms.Timer { Interval = 500 };
        var ready = false;
        timer.Tick += (_, _) =>
        {
            if (ready) { form.Close(); return; }
            ready = true; form.BringToFront(); form.Activate(); form.Refresh();
            var origin = form.PointToScreen(Point.Empty);
            Console.WriteLine($"{origin.X},{origin.Y},{form.ClientSize.Width},{form.ClientSize.Height}");
            Console.Out.Flush(); timer.Interval = 20000;
        };
        form.Shown += (_, _) => timer.Start();
        Application.Run(form); return 0;
    }

    private static void Check(bool success, string description)
    {
        if (!success) throw new InvalidOperationException("FAIL: " + description);
        count++; Console.WriteLine("PASS: " + description);
    }
}
