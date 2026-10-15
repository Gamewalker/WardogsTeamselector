using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using WardogsTeamselector.Core;
using WardogsTeamselector.Detection;

internal static class Program
{
    private static int checks;
    private static void Main(string[] args)
    {
        RunBonusChecks(args.Length > 0 ? args[0] : "src/WardogsTeamselector/Assets/reference.png");
        using var reference = new Bitmap("tests/DetectionChecks/Fixtures/legacy.png");
        Check("Original PNG", reference, true);
        CheckOverlay(reference, false);
        using (var hover = new Bitmap(reference))
        {
            using (var g = Graphics.FromImage(hover)) { using var brush = new SolidBrush(Color.FromArgb(36, 36, 36)); g.FillRectangle(brush, 1486, 947, 268, 350); }
            Check("Hover auf blauer Teamkarte", hover, true, new AppSettings { DetectionThreshold = .9 });
        }
        using (var cursor = new Bitmap(reference))
        {
            using (var g = Graphics.FromImage(cursor)) g.FillRectangle(Brushes.White, 1470, 925, 32, 24);
            Check("Kleine lokale Cursorüberdeckung", cursor, true, new AppSettings { DetectionThreshold = .9 });
        }
        foreach (int x in new[] { 1786, 2086 })
        {
            using var hover = new Bitmap(reference);
            using (var g = Graphics.FromImage(hover)) { using var brush = new SolidBrush(Color.FromArgb(36, 36, 36)); g.FillRectangle(brush, x, 947, 268, 350); }
            Check($"Hover auf Teamkarte X={x}", hover, true);
        }
        using (var covered = new Bitmap(reference))
        {
            using (var g = Graphics.FromImage(covered)) { g.FillRectangle(Brushes.White, 1470, 926, 70, 9); g.FillRectangle(Brushes.White, 2280, 926, 70, 9); }
            Check("Zwei vollständig verdeckte Messflächen", covered, false);
        }
        using (var shiftedColors = new Bitmap(reference))
        {
            using (var g = Graphics.FromImage(shiftedColors)) { using var brush = new SolidBrush(Color.FromArgb(35, 35, 35)); foreach (var area in DialogDetector.GetProbeAreas(reference.Size, new AppSettings(), false).Take(6)) g.FillRectangle(brush, area.Bounds); }
            Check("Einstellbare Schwelle 90 Prozent", shiftedColors, false, new AppSettings { DetectionThreshold = .9 });
            Check("Einstellbare Schwelle 85 Prozent", shiftedColors, true, new AppSettings { DetectionThreshold = .85 });
        }
        foreach (var size in new[] { new Size(1920, 1080), new Size(2560, 1440), new Size(3840, 2160) })
        {
            using var scaled = Resize(reference, size);
            Check($"Resize {size.Width}x{size.Height}", scaled, true);
        }
        using (var changed = new Bitmap(reference))
        {
            using var g = Graphics.FromImage(changed);
            // Replace the entire heading and emblem/counter fields without disturbing structural probes.
            using var heading = new SolidBrush(Color.FromArgb(28, 28, 28));
            g.FillRectangle(heading, 1475, 850, 880, 60);
            g.DrawString("SPRACHTEST / SELECT TEAM / ÉQUIPE", SystemFonts.DefaultFont, Brushes.White, 1485, 865);
            g.FillRectangle(Brushes.DarkBlue, 1540, 990, 170, 165);
            g.FillRectangle(Brushes.Red, 1840, 990, 170, 165);
            g.FillRectangle(Brushes.Lime, 2140, 990, 170, 165);
            g.FillRectangle(Brushes.Black, 1570, 1205, 110, 65);
            g.FillRectangle(Brushes.Black, 1870, 1205, 110, 65);
            g.FillRectangle(Brushes.Black, 2170, 1205, 110, 65);
            Check("Andere Überschrift, aktive Teamfarben, andere Zahlen", changed, true);
        }
        foreach (var color in new[] { Color.Black, Color.FromArgb(28, 28, 28), Color.FromArgb(100, 100, 100) })
        {
            using var blank = new Bitmap(reference.Width, reference.Height);
            using (var g = Graphics.FromImage(blank)) g.Clear(color);
            Check($"Leere Fläche {color.R}", blank, false);
        }
        using (var removed = new Bitmap(reference))
        {
            using (var g = Graphics.FromImage(removed)) g.FillRectangle(Brushes.Black, 1450, 825, 950, 520);
            Check("Dialog entfernt", removed, false);
        }
        using (var covered = new Bitmap(reference))
        {
            using (var g = Graphics.FromImage(covered)) g.FillRectangle(Brushes.Black, 2340, 820, 80, 520);
            Check("Rechter Rand verdeckt", covered, false);
        }
        using (var calibrated = new Bitmap(reference.Width, reference.Height))
        {
            var settings = new AppSettings { DetectionScale = .8, DetectionOffsetX = .05, DetectionOffsetY = -.04 };
            using (var g = Graphics.FromImage(calibrated))
            {
                g.Clear(Color.Black);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(reference, new Rectangle((int)Math.Round((.1 + .05) * reference.Width),
                    (int)Math.Round((.1 - .04) * reference.Height), (int)Math.Round(.8 * reference.Width), (int)Math.Round(.8 * reference.Height)));
            }
            Check("Kalibrierung Scale/Offset", calibrated, true, settings);
            Check("Falsche Kalibrierung", calibrated, false);
        }
        Console.WriteLine($"PASS: {checks} Detection-Checks, keine Bildschirmeingaben.");
    }
    private static void RunBonusChecks(string path)
    {
        using var original = new Bitmap(path);
        Check("Neue Referenz mit Bonuszeile", original, true);
        CheckOverlay(original, true);
        using var reference = Resize(original, new Size(2048, 1152));
        foreach (var size in new[] { new Size(1280, 720), new Size(1920, 1080), new Size(2560, 1440), new Size(3840, 2160) })
        {
            using var scaled = Resize(reference, size);
            Check($"Bonuslayout {size.Width}x{size.Height}", scaled, true);
        }
        foreach (int x in new[] { 792, 952, 1112 })
        {
            using var hover = new Bitmap(reference);
            using (var g = Graphics.FromImage(hover))
            {
                using var brush = new SolidBrush(Color.FromArgb(36, 36, 36));
                g.FillRectangle(brush, x, 497, 144, 187);
            }
            Check($"Bonuslayout Hover X={x}", hover, true);
        }
        using (var changed = new Bitmap(reference))
        {
            using (var g = Graphics.FromImage(changed))
            {
                using var background = new SolidBrush(Color.FromArgb(28, 28, 28));
                g.FillRectangle(background, 790, 445, 465, 30);
                g.DrawString("ANDERE SPRACHE", SystemFonts.DefaultFont, Brushes.White, 795, 450);
                g.FillRectangle(background, 790, 692, 465, 20);
                foreach (int x in new[] { 820, 980, 1140 })
                {
                    g.FillRectangle(Brushes.Magenta, x, 520, 90, 90);
                    g.FillRectangle(Brushes.White, x + 25, 645, 35, 27);
                }
            }
            Check("Bonuslayout andere Schrift, Embleme, Zahlen und Boni", changed, true);
        }
        var areas = DialogDetector.GetProbeAreas(reference.Size, new AppSettings());
        using (var cursor = new Bitmap(reference))
        {
            using (var g = Graphics.FromImage(cursor)) g.FillRectangle(Brushes.White, areas[0].Bounds.X, areas[0].Bounds.Y, 10, 10);
            Check("Bonuslayout Cursor", cursor, true);
        }
        using (var covered = new Bitmap(reference))
        {
            using (var g = Graphics.FromImage(covered)) foreach (var area in areas.Take(2)) g.FillRectangle(Brushes.White, area.Bounds);
            Check("Bonuslayout zwei Messflächen verdeckt", covered, false);
        }
        // Each complete edge must remain mandatory, even with legacy fallback.
        foreach (var edge in new[] { new Rectangle(775, 434, 500, 8), new Rectangle(775, 712, 500, 8),
            new Rectangle(775, 430, 8, 290), new Rectangle(1266, 430, 8, 290) })
        {
            using var covered = new Bitmap(reference);
            using (var g = Graphics.FromImage(covered)) g.FillRectangle(Brushes.Black, edge);
            Check($"Bonuslayout Rahmen verdeckt {edge}", covered, false);
        }
        using (var removed = new Bitmap(reference))
        {
            using (var g = Graphics.FromImage(removed)) g.FillRectangle(Brushes.Black, 775, 430, 500, 290);
            Check("Bonuslayout Dialog entfernt", removed, false);
        }
        using (var calibrated = new Bitmap(reference.Width, reference.Height))
        {
            using (var g = Graphics.FromImage(calibrated))
            {
                g.Clear(Color.Black);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(reference, new Rectangle((int)Math.Round(.15 * reference.Width),
                    (int)Math.Round(.06 * reference.Height), (int)Math.Round(.8 * reference.Width), (int)Math.Round(.8 * reference.Height)));
            }
            Check("Bonuslayout kalibriert", calibrated, true, new AppSettings { DetectionScale = .8, DetectionOffsetX = .05, DetectionOffsetY = -.04 });
            Check("Bonuslayout falsche Kalibrierung", calibrated, false);
        }
        foreach (var region in AppSettings.DefaultRegions())
        {
            var center = region.Center(new Rectangle(Point.Empty, reference.Size));
            var card = new Rectangle(791 + (int)region.Team * 160, 495, 146, 190);
            if (!card.Contains(center)) throw new Exception($"Standard-Klickpunkt außerhalb der Karte: {region.Team}");
            checks++;
        }
    }
    private static void CheckOverlay(Bitmap bitmap, bool withBonuses)
    {
        using var frame = new CaptureFrame(new Bitmap(bitmap), new TargetGeometry(new Rectangle(Point.Empty, bitmap.Size), IntPtr.Zero, true, "Overlay", true));
        var result = new DialogDetector().Detect(frame, new AppSettings());
        var expected = DialogDetector.GetProbeAreas(bitmap.Size, new AppSettings(), withBonuses);
        if (!result.Probes.Select(p => p.Bounds).SequenceEqual(expected.Select(p => (Rectangle?)p.Bounds)))
            throw new Exception("Overlay-Messflächen passen nicht zum erkannten Layout.");
        checks++;
    }
    private static Bitmap Resize(Bitmap source, Size size)
    {
        var result = new Bitmap(size.Width, size.Height);
        using var g = Graphics.FromImage(result);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(source, new Rectangle(Point.Empty, size));
        return result;
    }
    private static void Check(string name, Bitmap bitmap, bool expected, AppSettings? settings = null)
    {
        using var frame = new CaptureFrame(new Bitmap(bitmap), new TargetGeometry(new Rectangle(Point.Empty, bitmap.Size), IntPtr.Zero, true, "Test PNG", true));
        var result = new DialogDetector().Detect(frame, settings ?? new AppSettings());
        Console.WriteLine($"{name}: {result.IsMatch}, Score {result.Score:F3}");
        if (result.IsMatch != expected)
        {
            foreach (var probe in result.Probes) Console.WriteLine($"  {probe.Name}: {probe.Score:F2} {probe.Actual}");
            throw new Exception($"{name}: expected {expected}; {result.Reason}");
        }
        checks++;
    }
}
