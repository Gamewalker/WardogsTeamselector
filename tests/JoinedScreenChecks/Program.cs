using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using WardogsTeamselector.Core;
using WardogsTeamselector.Detection;

internal static class Program
{
    private static int checks;
    private static void Main()
    {
        using var reference = new Bitmap("src/WardogsTeamselector/Assets/joined.png");
        Check("Gameplay reference", reference, true);
        foreach (var size in new[] { new Size(1920, 1080), new Size(2560, 1440), new Size(3840, 2160), new Size(1280, 720) })
        {
            using var scaled = new Bitmap(size.Width, size.Height);
            using (var g = Graphics.FromImage(scaled)) { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(reference, new Rectangle(Point.Empty, size)); }
            Check($"Scaled {size}", scaled, true);
        }
        Check("Independent dialog calibration", reference, true, new AppSettings { DetectionOffsetX = .2, DetectionOffsetY = .1, DetectionScale = 1.5 });
        using var dialog = new Bitmap("src/WardogsTeamselector/Assets/reference.png");
        Check("Faction dialog", dialog, false);
        foreach (var color in new[] { Color.Black, Color.White })
        {
            using var solid = new Bitmap(reference.Width, reference.Height);
            using (var g = Graphics.FromImage(solid)) g.Clear(color);
            Check($"Solid {color}", solid, false);
        }
        using (var absent = new Bitmap(reference))
        {
            using (var g = Graphics.FromImage(absent)) g.FillRectangle(Brushes.Black, 1775, 1070, 212, 20);
            Check("Bars absent", absent, false);
        }
        for (int i = 0; i < 5; i++)
        {
            using var missing = new Bitmap(reference);
            using (var g = Graphics.FromImage(missing)) g.FillRectangle(Brushes.Black, new[] { 1778, 1820, 1861, 1903, 1944 }[i], 1078, 37, 4);
            Check($"Missing bar {i + 1}", missing, false);
        }
        using (var wrong = new Bitmap(reference))
        {
            using (var g = Graphics.FromImage(wrong))
            {
                g.FillRectangle(Brushes.Black, 1775, 1070, 212, 20);
                for (int i = 0; i < 5; i++) g.FillRectangle(Brushes.White, 1778 + i * 40, 1078, 37, 4);
            }
            Check("Wrong spacing", wrong, false);
        }
        using (var thick = new Bitmap(reference))
        {
            using (var g = Graphics.FromImage(thick)) foreach (int x in new[] { 1778, 1820, 1861, 1903, 1944 }) g.FillRectangle(Brushes.White, x, 1065, 37, 25);
            Check("Thick white textures", thick, false);
        }
        Console.WriteLine($"Passed {checks} HUD image checks.");
    }
    private static void Check(string name, Bitmap bitmap, bool expected, AppSettings? settings = null)
    {
        using var frame = new CaptureFrame(new Bitmap(bitmap), new TargetGeometry(new Rectangle(Point.Empty, bitmap.Size), IntPtr.Zero, true, "test", true));
        var result = new JoinedScreenDetector().Detect(frame, settings ?? new AppSettings());
        if (result.IsMatch != expected)
        {
            foreach (var p in result.Probes) Console.WriteLine($"{p.Name}: {p.Score:F3}; {p.Actual}");
            throw new Exception($"{name}: expected {expected}, got {result.IsMatch} ({result.Score:F3})");
        }
        checks++;
        Console.WriteLine($"PASS {name}: {result.Score:F3}");
    }
}
