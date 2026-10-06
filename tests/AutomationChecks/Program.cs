using System.Diagnostics;
using System.Collections.Concurrent;
using WardogsTeamselector.Automation;
using WardogsTeamselector.Core;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
static async Task Until(Func<bool> condition, string message)
{
    var clock = Stopwatch.StartNew();
    while (!condition() && clock.ElapsedMilliseconds < 3000) await Task.Delay(5);
    Check(condition(), message);
}
static AppSettings Settings() => new() { DryRun = false, GeometryCalibrated = true, MinIntervalMs = 50, MaxIntervalMs = 50 };

var screen = new FakeScreen();
var detector = new FakeDetector();
var sink = new FakeSink();
var hud = new FakeJoinedDetector();
using var control = new AutomationController(screen, detector, sink, hud);
foreach (var pair in new[] { (49, 100), (50, 49), (-1, -1), (50, 60001) })
{
    var bad = Settings(); bad.MinIntervalMs = pair.Item1; bad.MaxIntervalMs = pair.Item2;
    bool rejected = false;
    try { control.Start(Team.Blue, bad); } catch (ArgumentException) { rejected = true; }
    Check(rejected, $"Invalid interval accepted: {pair}");
}

control.Start(Team.Red, Settings());
await Task.Delay(140);
Check(sink.Count == 0 && control.Snapshot.State == RunState.Waiting, "Armed run clicked without dialog");
detector.Match = true; screen.Foreground = false;
await Task.Delay(120);
Check(sink.Count == 0, "Clicked without focus");
screen.Foreground = true; screen.Calibrated = false;
await Task.Delay(120);
Check(sink.Count == 0, "Clicked uncalibrated");
screen.Calibrated = true;
await Until(() => sink.Count >= 3, "Stable dialog did not begin clicking");
Check(detector.Calls >= 3, "Stable multi-frame checks missing");
Check(sink.MinimumSpacing >= 50, $"50ms floor violated: {sink.MinimumSpacing}");
detector.Match = false;
await Until(() => control.Snapshot.State == RunState.ConfirmingJoin, "Dialog loss did not pause");
var count = sink.Count;
await Task.Delay(140);
Check(sink.Count == count && control.Snapshot.AbsenceRemainingMs > 0, "Absent dialog clicked or prematurely stopped");
foreach (var match in new[] { true, true, false }) detector.Samples.Enqueue(match);
await Until(() => detector.Samples.IsEmpty, "Transient frames were not sampled");
await Task.Delay(40);
Check(sink.Count == count && control.Snapshot.State == RunState.ConfirmingJoin, "Two-frame return or negative blip clicked");
detector.Match = true;
await Until(() => sink.Count > count, "Returning dialog did not resume");
Check(control.Snapshot.Team == Team.Red && control.Snapshot.ClickCount > 3, "Return lost team or count");
control.Stop();
var brief = Settings(); brief.DialogAbsenceTimeoutMs = 200;
control.Start(Team.Red, brief);
brief.DialogAbsenceTimeoutMs = 60000;
await Until(() => control.Snapshot.State == RunState.Clicking, "Timeout run did not start");
detector.Match = false;
await Until(() => control.Snapshot.State == RunState.Stopped, "Continuous absence did not stop");
Check(control.Snapshot.Reason.Contains("nicht zurückgekehrt"), "Absence timeout not explained");
count = sink.Count; detector.Match = true;
await Task.Delay(140);
Check(sink.Count == count, "Stopped run auto-rearmed");

control.Start(Team.Blue, Settings());
await Until(() => sink.Count > count, "Restart failed");
screen.Foreground = false;
await Until(() => control.Snapshot.State == RunState.Stopped, "Focus loss did not stop");
screen.Foreground = true;

control.Start(Team.Blue, Settings());
await Until(() => control.Snapshot.State == RunState.Clicking, "Geometry test did not begin");
screen.Width = 1001;
await Until(() => control.Snapshot.State == RunState.Stopped, "Geometry change did not stop");
screen.Width = 1000;

var settings = Settings();
control.Start(Team.Green, settings);
settings.MinIntervalMs = 1; settings.Regions.Clear(); settings.Hotkeys.Clear();
await Until(() => control.Snapshot.State == RunState.Clicking, "Settings were not deep-snapshotted");
control.Stop(); count = sink.Count;
await Task.Delay(120);
Check(sink.Count == count, "A click occurred after Stop returned");

Parallel.For(0, 30, i => control.Start((Team)(i % 3), Settings()));
control.Start(Team.Red, Settings());
await Until(() => control.Snapshot.State == RunState.Clicking, "Concurrent replacement never activated");
Check(control.Snapshot.Team == Team.Red, "Stale session published after replacement");
Check(sink.MinimumSpacing >= 50, "Concurrent start bypassed 50ms floor");
control.Stop();

var dry = Settings(); dry.DryRun = true; screen.Calibrated = false; count = sink.Count;
control.Start(Team.Blue, dry);
await Until(() => control.Snapshot.ClickCount >= 2, "Uncalibrated dry-run did not simulate");
Check(sink.Count == count && control.Snapshot.Reason.Contains("simuliert"), "Dry run sent inputs");
control.Stop(); screen.Calibrated = true;

screen.Fail = true;
control.Start(Team.Blue, Settings());
await Until(() => control.Snapshot.State == RunState.Stopped, "Capture failure did not stop");
Check(control.Snapshot.Reason.Contains("fehlgeschlagen"), "Capture error not explained");
screen.Fail = false;

// HUD must be continuously visible, without the selection dialog, for at least 500ms.
detector.Match = false; hud.Match = true;
control.Start(Team.Green, Settings());
await Task.Delay(300);
Check(control.Snapshot.State == RunState.Waiting, "HUD confirmed too early");
hud.Match = false;
await Task.Delay(80);
hud.Match = true;
await Task.Delay(300);
Check(control.Snapshot.State == RunState.Waiting, "HUD flicker did not reset confirmation");
await Until(() => control.Snapshot.State == RunState.Stopped, "Stable HUD did not confirm armed run");
Check(control.Snapshot.Reason.Contains("HUD"), "HUD confirmation not explained");
count = sink.Count; detector.Match = true;
await Task.Delay(140);
Check(sink.Count == count, "Confirmed HUD run auto-rearmed");
control.Start(Team.Blue, Settings());
await Until(() => control.Snapshot.State == RunState.Clicking, "HUD blocked visible dialog");
await Task.Delay(600);
Check(control.Snapshot.State == RunState.Clicking, "HUD confirmed while selection dialog was present");
detector.Match = false;
await Until(() => control.Snapshot.State == RunState.ConfirmingJoin, "HUD success run did not pause");
count = sink.Count;
await Until(() => control.Snapshot.State == RunState.Stopped, "HUD did not confirm after clicking");
Check(sink.Count == count && control.Snapshot.Reason.Contains("HUD"), "HUD success delivered clicks or wrong stop reason");
detector.Match = true; hud.Match = false;
control.Start(Team.Blue, Settings());
await Until(() => control.Snapshot.State == RunState.Clicking, "Confirming focus test did not start");
detector.Match = false;
await Until(() => control.Snapshot.State == RunState.ConfirmingJoin, "Confirming focus test did not pause");
screen.Foreground = false;
await Until(() => control.Snapshot.State == RunState.Stopped, "Focus loss while confirming did not stop");
Check(control.Snapshot.Reason.Contains("fokus"), "Focus loss while confirming not explained");
screen.Foreground = true; hud.Match = true;
control.Stop();
detector.Match = false;
var hudOff = Settings(); hudOff.DetectJoinedHud = false;
control.Start(Team.Blue, hudOff); hudOff.DetectJoinedHud = true; hudOff.DialogAbsenceTimeoutMs = 1;
await Task.Delay(650);
Check(control.Snapshot.State == RunState.Waiting && control.Snapshot.JoinedDetection is null, "HUD setting was not copied");
control.Stop("Manual test stop");
await Task.Delay(650);
Check(control.Snapshot.Reason == "Manual test stop", "HUD detection ran after Stop");
control.Start(Team.Blue, Settings()); screen.Foreground = false;
await Task.Delay(650);
Check(control.Snapshot.State == RunState.Waiting, "HUD confirmed without focus");
screen.Foreground = true; hud.Match = false; detector.Match = true;
control.Stop();

// Stop must wait for an in-flight input rather than allowing it to complete afterwards.
using var blockingSink = new BlockingSink();
using var second = new AutomationController(screen, detector, blockingSink);
second.Start(Team.Blue, Settings());
Check(blockingSink.Entered.Wait(3000), "Blocking input not entered");
var stopTask = Task.Run(() => second.Stop());
await Task.Delay(80);
Check(!stopTask.IsCompleted, "Stop returned during input delivery");
blockingSink.Release.Set();
await stopTask;
var delivered = blockingSink.Count;
await Task.Delay(120);
Check(blockingSink.Count == delivered, "Input delivered after synchronized Stop");
Console.WriteLine("PASS: timing validation, armed waiting, stable detection, focus/calibration, transient dialog retry, absence timeout, continuous HUD confirmation/flicker/focus/disable, geometry, deep settings snapshot, cancellation, concurrent starts, dry-run, capture failure, in-flight Stop synchronization.");

sealed class FakeScreen : IScreenService
{
    public volatile bool Foreground = true, Calibrated = true, Fail;
    public volatile int Width = 1000;
    public IReadOnlyList<MonitorInfo> GetMonitors() => [];
    public TargetGeometry? ResolveTarget(AppSettings settings) => new(new(0, 0, Width, 600), new IntPtr(42), Foreground, "Fake", Calibrated);
    public CaptureFrame Capture(TargetGeometry target) => Fail ? throw new InvalidOperationException("Fake capture failure") : new(new Bitmap(1, 1), target);
}
sealed class FakeDetector : IDialogDetector
{
    public volatile bool Match;
    public int Calls;
    public readonly ConcurrentQueue<bool> Samples = new();
    public DetectionResult Detect(CaptureFrame frame, AppSettings settings)
    {
        Interlocked.Increment(ref Calls);
        var match = Samples.TryDequeue(out var sample) ? sample : Match;
        return new(match, match ? 1 : 0, [], "Fake");
    }
}
sealed class FakeJoinedDetector : IJoinedScreenDetector
{
    public volatile bool Match;
    public DetectionResult Detect(CaptureFrame frame, AppSettings settings) => new(Match, Match ? 1 : 0, [], "Fake HUD");
}
sealed class FakeSink : IClickSink
{
    private readonly object gate = new();
    private readonly List<long> times = [];
    public int Count { get { lock (gate) return times.Count; } }
    public double MinimumSpacing
    {
        get { lock (gate) return times.Count < 2 ? double.PositiveInfinity : times.Zip(times.Skip(1), (a, b) => Stopwatch.GetElapsedTime(a, b).TotalMilliseconds).Min(); }
    }
    public void Click(Point position) { lock (gate) times.Add(Stopwatch.GetTimestamp()); }
}
sealed class BlockingSink : IClickSink, IDisposable
{
    public readonly ManualResetEventSlim Entered = new(), Release = new();
    public int Count;
    public void Click(Point position) { Entered.Set(); Release.Wait(); Interlocked.Increment(ref Count); }
    public void Dispose() { Entered.Dispose(); Release.Dispose(); }
}
