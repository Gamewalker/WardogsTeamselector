using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WardogsTeamselector.Core;

public enum Team { Blue, Red, Green }
public sealed record HotkeyChoice(int Code, string Name)
{
    public override string ToString() => Name;
    public static IReadOnlyList<HotkeyChoice> All { get; } = Enumerable.Range(0x70, 24)
        .Select(code => new HotkeyChoice(code, "F" + (code - 0x6F)))
        .Concat(new[] { new HotkeyChoice(0x1B, "ESC"), new(0x2D, "EINFG"), new(0x2E, "ENTF"),
            new(0x24, "POS1"), new(0x23, "ENDE"), new(0x21, "BILD AUF"), new(0x22, "BILD AB"),
            new(0x13, "PAUSE"), new(0x91, "ROLLEN"), new(0x90, "NUM"), new(0x25, "←"),
            new(0x26, "↑"), new(0x27, "→"), new(0x28, "↓") }).ToArray();
    public static bool IsSupported(int code) => All.Any(key => key.Code == code);
    public static string Display(int code) => All.First(key => key.Code == code).Name;
}
public enum RunState { Stopped, Waiting, Clicking }
public enum AutomationStopCause { None, Manual, Safety, Joined, GroupUpdate }
public sealed record MonitorInfo(string Id, string Name, Rectangle Bounds, bool Primary);
public sealed record TargetGeometry(Rectangle Bounds, IntPtr WindowHandle, bool IsForeground, string Description, bool IsCalibrated);
public sealed record TeamRegion(Team Team, double X, double Y, double Width, double Height)
{
    public Point Center(Rectangle bounds) => new(bounds.Left + (int)Math.Round((X + Width / 2) * bounds.Width), bounds.Top + (int)Math.Round((Y + Height / 2) * bounds.Height));
}
public sealed record ProbeResult(string Name, double Score, string Expected, string Actual);
public sealed record DetectionResult(bool IsMatch, double Score, IReadOnlyList<ProbeResult> Probes, string Reason);
public sealed class CaptureFrame : IDisposable
{
    public Bitmap Bitmap { get; }
    public TargetGeometry Geometry { get; }
    public DateTimeOffset CapturedAt { get; } = DateTimeOffset.Now;
    public CaptureFrame(Bitmap bitmap, TargetGeometry geometry) { Bitmap = bitmap; Geometry = geometry; }
    public void Dispose() => Bitmap.Dispose();
}
public sealed class AppSettings
{
    public int MinIntervalMs { get; set; } = 50;
    public int MaxIntervalMs { get; set; } = 70;
    public double DetectionThreshold { get; set; } = 0.90;
    public bool LivePreviewEnabled { get; set; } = true;
    public bool FocusGameOnTeamActivation { get; set; } = true;
    public string? MonitorId { get; set; }
    public string WindowTitleContains { get; set; } = "wardogs";
    public string ProcessNameContains { get; set; } = "wardogs";
    public bool DryRun { get; set; } = false;
    public bool GeometryCalibrated { get; set; }
    public Rectangle? ManualBounds { get; set; }
    public double DetectionOffsetX { get; set; }
    public double DetectionOffsetY { get; set; }
    public double DetectionScale { get; set; } = 1;
    public Dictionary<Team, int> Hotkeys { get; set; } = new() { [Team.Blue] = 0x75, [Team.Red] = 0x76, [Team.Green] = 0x77 };
    public int StopHotkey { get; set; } = 0x23;
    public List<TeamRegion> Regions { get; set; } = DefaultRegions();
    public static List<TeamRegion> DefaultRegions() => new() {
        new(Team.Blue, 1483d/3838, 944d/2158, 273d/3838, 358d/2158),
        new(Team.Red, 1783d/3838, 944d/2158, 273d/3838, 358d/2158),
        new(Team.Green, 2083d/3838, 944d/2158, 273d/3838, 358d/2158) };
    public void Validate()
    {
        if (MinIntervalMs < 50 || MaxIntervalMs < MinIntervalMs || MaxIntervalMs > 60000) throw new ArgumentException("Es gilt 50 ≤ A ≤ B ≤ 60000 ms.");
        if (!double.IsFinite(DetectionThreshold) || DetectionThreshold < 0.5 || DetectionThreshold > 1) throw new ArgumentException("Erkennungsschwelle: 0,5 bis 1.");
        if (!double.IsFinite(DetectionScale) || DetectionScale < 0.25 || DetectionScale > 4 || !double.IsFinite(DetectionOffsetX) || !double.IsFinite(DetectionOffsetY)) throw new ArgumentException("Ungültige Erkennungskalibrierung.");
        if (string.IsNullOrWhiteSpace(WindowTitleContains)) throw new ArgumentException("Spielfenster-Filter darf nicht leer sein.");
        if (string.IsNullOrWhiteSpace(ProcessNameContains)) throw new ArgumentException("Spielprozess-Filter darf nicht leer sein.");
        if (Hotkeys.Count != 3 || Enum.GetValues<Team>().Any(t => !Hotkeys.ContainsKey(t)) || Hotkeys.Values.Append(StopHotkey).Distinct().Count() != 4 || Hotkeys.Values.Append(StopHotkey).Any(k => !HotkeyChoice.IsSupported(k))) throw new ArgumentException("Vier unterschiedliche Tasten für Teams und Stopp wählen: F1–F24, ESC oder Sondertasten.");
        if (Regions.Count != 3 || Regions.Select(r => r.Team).Distinct().Count() != 3 || Regions.Any(r => !Enum.IsDefined(r.Team) || !double.IsFinite(r.X) || !double.IsFinite(r.Y) || !double.IsFinite(r.Width) || !double.IsFinite(r.Height) || r.X < 0 || r.Y < 0 || r.Width <= 0 || r.Height <= 0 || r.X + r.Width > 1 || r.Y + r.Height > 1)) throw new ArgumentException("Teamflächen müssen vollständig im Spielbereich liegen.");
        if (ManualBounds is Rectangle b && (b.Width < 100 || b.Height < 100)) throw new ArgumentException("Manueller Spielbereich ist zu klein.");
    }
}
public interface IScreenService
{
    IReadOnlyList<MonitorInfo> GetMonitors();
    TargetGeometry? ResolveTarget(AppSettings settings);
    // Recheck the captured window immediately before input without rediscovering
    // all desktop windows. Other screen implementations retain the full check.
    TargetGeometry? RevalidateTarget(TargetGeometry target, AppSettings settings) => ResolveTarget(settings);
    CaptureFrame Capture(TargetGeometry target);
}
public interface IDialogDetector { DetectionResult Detect(CaptureFrame frame, AppSettings settings); }
public interface IJoinedScreenDetector { DetectionResult Detect(CaptureFrame frame, AppSettings settings); }
public interface IClickSink { void Click(Point position); }
public sealed record AutomationSnapshot(RunState State, Team? Team, long ClickCount, int IntervalMs, string Reason, DetectionResult? Detection, TargetGeometry? Geometry, DetectionResult? JoinedDetection = null, AutomationStopCause StopCause = AutomationStopCause.None);
