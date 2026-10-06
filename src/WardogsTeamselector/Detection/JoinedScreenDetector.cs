using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WardogsTeamselector.Core;

namespace WardogsTeamselector.Detection;

/// <summary>Recognizes the five separated, thin white HUD bars in the supplied gameplay reference.
/// Coordinates scale with the capture viewport, independently of faction dialog calibration.</summary>
public sealed class JoinedScreenDetector : IJoinedScreenDetector
{
    private static readonly int[] Starts = { 1778, 1820, 1861, 1903, 1944 };
    private const double ReferenceWidth = 2048, ReferenceHeight = 1152;

    public static IReadOnlyList<DetectionProbeArea> GetProbeAreas(Size size)
    {
        var areas = new List<DetectionProbeArea>();
        for (int i = 0; i < Starts.Length; i++)
        {
            areas.Add(new($"HUD Balken {i + 1}", Scale(new Rectangle(Starts[i], 1078, 37, 4), size)));
            areas.Add(new($"HUD Form {i + 1}", Scale(new Rectangle(Starts[i] + 3, 1073, 31, 14), size)));
            if (i < 4) areas.Add(new($"HUD Lücke {i + 1}", Scale(new Rectangle(Starts[i] + 37, 1078, Starts[i + 1] - Starts[i] - 37, 4), size)));
        }
        return areas;
    }

    public DetectionResult Detect(CaptureFrame frame, AppSettings settings)
    {
        var bitmap = frame.Bitmap;
        if (bitmap.Width < 800 || bitmap.Height < 450)
            return new(false, 0, Array.Empty<ProbeResult>(), "Spielbereich für dünne HUD-Balken zu klein.");
        var probes = new List<ProbeResult>();
        var barMeans = new List<double>();
        for (int i = 0; i < Starts.Length; i++)
        {
            int matched = 0, shapeMatched = 0;
            double sum = 0, contrastSum = 0;
            for (int j = 0; j < 16; j++)
            {
                double x = Starts[i] + 2 + j * 32d / 15;
                var c = Sample(bitmap, x, 1079.5);
                double light = Luma(c);
                sum += light;
                if (light >= 175 && Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B)) <= 35) matched++;
                double background = Math.Max(Luma(Sample(bitmap, x, 1074)), Luma(Sample(bitmap, x, 1085)));
                double contrast = light - background;
                contrastSum += contrast;
                if (contrast >= 40) shapeMatched++;
            }
            barMeans.Add(sum / 16);
            probes.Add(new($"HUD Balken {i + 1}", matched / 16d, "Heller neutraler Balken ≥175 RGB", $"Helligkeit {sum / 16:F1}; {matched}/16 helle Proben"));
            probes.Add(new($"HUD Form {i + 1}", shapeMatched / 16d, "Dunkler oberhalb/unterhalb, Kontrast ≥40", $"Kontrast {contrastSum / 16:F1}; {shapeMatched}/16 dünne Proben"));
        }
        for (int i = 0; i < 4; i++)
        {
            double x = (Starts[i] + 36 + Starts[i + 1]) / 2d;
            double gap = Luma(Sample(bitmap, x, 1079.5));
            double contrast = Math.Min(barMeans[i], barMeans[i + 1]) - gap;
            probes.Add(new($"HUD Lücke {i + 1}", gap < 155 && contrast >= 45 ? 1 : 0,
                "Vier dunkle Zwischenräume, Kontrast ≥45", $"Helligkeit {gap:F1}; Kontrast {contrast:F1}"));
        }
        double score = probes.Average(p => p.Score);
        // Each bar, its thin shape and every gap are required: one missing bar cannot be
        // compensated by unrelated matching surfaces elsewhere in the HUD.
        bool match = probes.All(p => p.Score >= .875) && score >= .94;
        return new(match, score, probes, match ? "Fünf dünne getrennte HUD-Balken erkannt." : "HUD-Balken, Zwischenräume oder dünne Form stimmen nicht überein.");
    }

    private static Color Sample(Bitmap bitmap, double x, double y) => bitmap.GetPixel(
        Math.Clamp((int)Math.Round((x + .5) * bitmap.Width / ReferenceWidth - .5), 0, bitmap.Width - 1),
        Math.Clamp((int)Math.Round((y + .5) * bitmap.Height / ReferenceHeight - .5), 0, bitmap.Height - 1));
    private static double Luma(Color c) => (c.R + c.G + c.B) / 3d;
    private static Rectangle Scale(Rectangle r, Size size) => Rectangle.FromLTRB(
        (int)Math.Floor(r.Left * size.Width / ReferenceWidth), (int)Math.Floor(r.Top * size.Height / ReferenceHeight),
        (int)Math.Ceiling(r.Right * size.Width / ReferenceWidth), (int)Math.Ceiling(r.Bottom * size.Height / ReferenceHeight));
}
