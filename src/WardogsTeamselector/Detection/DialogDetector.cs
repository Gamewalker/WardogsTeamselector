using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WardogsTeamselector.Core;

namespace WardogsTeamselector.Detection;

/// <summary>Pixel rectangle in the captured bitmap, not in desktop coordinates.</summary>
public sealed record DetectionProbeArea(string Name, Rectangle Bounds);

/// <summary>
/// Recognizes the fixed frame and card layout, without examining text, counters or team emblems.
/// All reference coordinates are transformed around the viewport center, then offset in viewport units.
/// </summary>
public sealed class DialogDetector : IDialogDetector
{
    private const double ReferenceWidth = 3838, ReferenceHeight = 2158;
    private sealed record Area(string Name, RectangleF Rect, double Expected);
    private static readonly Area[] Patches = {
        new("Dialog oben links", new(1470, 926, 70, 9), 28),
        new("Dialog oben rechts", new(2280, 926, 70, 9), 28),
        new("Dialog unten links", new(1480, 1307, 75, 8), 28),
        new("Dialog unten rechts", new(2280, 1307, 75, 8), 28),
        new("Kartenabstand links", new(1766, 970, 8, 280), 28),
        new("Kartenabstand rechts", new(2066, 970, 8, 280), 28),
        new("Karte Blau", new(1502, 967, 32, 15), 12),
        new("Karte Rot", new(1802, 967, 32, 15), 3),
        new("Karte Grün", new(2102, 967, 32, 15), 3)
    };
    private sealed record Edge(string Name, float X1, float Y1, float X2, float Y2, bool Horizontal, double MinimumContrast);
    private static readonly Edge[] Edges = {
        new("Oberer Dialograhmen", 1480, 837, 2360, 837, true, 25),
        new("Unterer Dialograhmen", 1480, 1323, 2360, 1323, true, 12),
        new("Linker Dialograhmen", 1459, 950, 1459, 1290, false, 16),
        new("Rechter Dialograhmen", 2380, 950, 2380, 1290, false, 16)
    };

    /// <summary>Use these bitmap-relative areas for the debug preview overlay.</summary>
    public static IReadOnlyList<DetectionProbeArea> GetProbeAreas(Size bitmapSize, AppSettings settings)
    {
        var result = Patches.Select(a => new DetectionProbeArea(a.Name, Transform(a.Rect, bitmapSize, settings))).ToList();
        foreach (var e in Edges)
        {
            var r = e.Horizontal ? new RectangleF(e.X1, e.Y1 - 3, e.X2 - e.X1, 6)
                : new RectangleF(e.X1 - 3, e.Y1, 6, e.Y2 - e.Y1);
            result.Add(new(e.Name, Transform(r, bitmapSize, settings)));
        }
        return result;
    }

    public DetectionResult Detect(CaptureFrame frame, AppSettings settings)
    {
        var bitmap = frame.Bitmap;
        if (!double.IsFinite(settings.DetectionScale) || settings.DetectionScale <= 0 ||
            !double.IsFinite(settings.DetectionOffsetX) || !double.IsFinite(settings.DetectionOffsetY))
            return new(false, 0, Array.Empty<ProbeResult>(), "Ungültige Erkennungskalibrierung.");
        var probes = new List<ProbeResult>();
        foreach (var a in Patches)
        {
            var rect = Transform(a.Rect, bitmap.Size, settings);
            if (!Contains(bitmap, rect)) return new(false, 0, probes, "Messflächen liegen außerhalb des Spielbereichs.");
            var colors = SamplePatch(bitmap, rect);
            // A cursor can replace a few samples with bright pixels. Channel medians
            // retain the stable surface color instead of letting those outliers dominate.
            double r = Median(colors.Select(c => (double)c.R)), g = Median(colors.Select(c => (double)c.G)), b = Median(colors.Select(c => (double)c.B));
            double mean = (r + g + b) / 3;
            double variation = Median(colors.Select(c => Math.Abs(Luminance(c) - mean)));
            double neutral = Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));
            bool card = a.Name.StartsWith("Karte ", StringComparison.Ordinal);
            double deviation = card ? Math.Max(0, mean - 64) : Math.Abs(mean - a.Expected);
            double score = Math.Clamp(1 - deviation / 24 - neutral / 40 - variation / 30, 0, 1);
            probes.Add(new(a.Name, score, card ? "Dunkle neutrale Fläche (RGB 0–64), Hover erlaubt" : $"RGB ≈ {a.Expected:F0}, neutral und gleichmäßig",
                $"RGB {r:F0}/{g:F0}/{b:F0}; Streuung {variation:F1}"));
        }
        foreach (var e in Edges)
        {
            int matched = 0;
            double totalContrast = 0;
            for (int i = 0; i < 20; i++)
            {
                double t = (i + .5) / 20;
                float x = (float)(e.X1 + (e.X2 - e.X1) * t), y = (float)(e.Y1 + (e.Y2 - e.Y1) * t);
                var p = TransformPoint(x, y, bitmap.Size, settings);
                var inside = TransformPoint(x + (e.Horizontal ? 0 : e.X1 < 1900 ? 9 : -9),
                    y + (e.Horizontal ? e.Y1 < 1000 ? 9 : -9 : 0), bitmap.Size, settings);
                if (!Contains(bitmap, p) || !Contains(bitmap, inside)) return new(false, 0, probes, "Rahmenmessung liegt außerhalb des Spielbereichs.");
                double peak = 0;
                // Thin antialiased frame survives downsampling; find its local peak along the normal.
                for (int delta = -2; delta <= 2; delta++)
                {
                    var q = new Point(p.X + (e.Horizontal ? 0 : delta), p.Y + (e.Horizontal ? delta : 0));
                    if (!Contains(bitmap, q)) continue;
                    var color = bitmap.GetPixel(q.X, q.Y);
                    if (Math.Max(color.R, Math.Max(color.G, color.B)) - Math.Min(color.R, Math.Min(color.G, color.B)) <= 25)
                        peak = Math.Max(peak, Luminance(color));
                }
                double contrast = peak - Luminance(bitmap.GetPixel(inside.X, inside.Y));
                totalContrast += contrast;
                if (contrast >= e.MinimumContrast) matched++;
            }
            probes.Add(new(e.Name, matched / 20d, $"Kontrast ≥ {e.MinimumContrast:F0} an mindestens 80 % der Kante",
                $"{matched}/20 passend; mittlerer Kontrast {totalContrast / 20:F1}"));
        }
        // One locally occluded surface patch may be ignored, never a whole frame edge.
        // Remaining surfaces and all four edges must independently support the dialog.
        var retainedPatches = probes.Take(Patches.Length).OrderByDescending(p => p.Score).Take(Patches.Length - 1).ToArray();
        var frameEdges = probes.Skip(Patches.Length).ToArray();
        double total = retainedPatches.Concat(frameEdges).Average(p => p.Score);
        bool structure = retainedPatches.All(p => p.Score >= .55) && frameEdges.All(p => p.Score >= .8);
        int ignored = Enumerable.Range(0, Patches.Length).OrderBy(i => probes[i].Score).First();
        probes[ignored] = probes[ignored] with { Actual = probes[ignored].Actual + "; schwächste Flächenprobe nicht gewichtet" };
        bool match = structure && total >= settings.DetectionThreshold;
        return new(match, total, probes, match ? "Teamauswahldialog erkannt (Rahmen und Kartenlayout)." :
            structure ? "Gesamtscore unter Erkennungsschwelle." : "Rahmen oder Kartenlayout fehlt/ist verdeckt.");
    }

    private static Point TransformPoint(double x, double y, Size size, AppSettings settings) => new(
        (int)Math.Round(((x / ReferenceWidth - .5) * settings.DetectionScale + .5 + settings.DetectionOffsetX) * size.Width),
        (int)Math.Round(((y / ReferenceHeight - .5) * settings.DetectionScale + .5 + settings.DetectionOffsetY) * size.Height));
    private static Rectangle Transform(RectangleF rect, Size size, AppSettings settings)
    {
        var start = TransformPoint(rect.X, rect.Y, size, settings);
        var end = TransformPoint(rect.Right, rect.Bottom, size, settings);
        return new(start.X, start.Y, Math.Max(1, end.X - start.X), Math.Max(1, end.Y - start.Y));
    }
    private static bool Contains(Bitmap bitmap, Point p) => p.X >= 0 && p.Y >= 0 && p.X < bitmap.Width && p.Y < bitmap.Height;
    private static bool Contains(Bitmap bitmap, Rectangle r) => r.X >= 0 && r.Y >= 0 && r.Right <= bitmap.Width && r.Bottom <= bitmap.Height;
    private static double Luminance(Color color) => (color.R + color.G + color.B) / 3d;
    private static double Median(IEnumerable<double> values) { var ordered = values.OrderBy(v => v).ToArray(); return ordered[ordered.Length / 2]; }
    private static List<Color> SamplePatch(Bitmap bitmap, Rectangle rectangle)
    {
        var colors = new List<Color>(25);
        for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++)
            colors.Add(bitmap.GetPixel(rectangle.X + Math.Min(rectangle.Width - 1, (int)((x + .5) / 5 * rectangle.Width)),
                rectangle.Y + Math.Min(rectangle.Height - 1, (int)((y + .5) / 5 * rectangle.Height))));
        return colors;
    }
}
