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
var count = sink.Count;
// Continue for longer than the old default ten-second absence timeout.
await Task.Delay(10500);
Check(sink.Count > count + 20 && control.Snapshot.State == RunState.Clicking, "Dialog absence stopped clicking without HUD");
Check(control.Snapshot.Team == Team.Red, "Dialog absence changed team");
detector.Match = true;
count = sink.Count;
await Until(() => sink.Count >= count + 3, "Returned dialog after failed join did not continue clicking");
Check(control.Snapshot.Team == Team.Red && control.Snapshot.State == RunState.Clicking, "Failed join replaced or stopped the active team");
foreach (var match in new[] { true, true, false }) detector.Samples.Enqueue(match);
await Until(() => detector.Samples.IsEmpty, "Transient frames were not sampled");
count = sink.Count;
await Until(() => sink.Count > count, "Dialog flicker paused the active run");
control.Stop(); count = sink.Count;
detector.Match = true;
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

// HUD must be continuously visible for 500ms; flicker cannot finish a run.
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

hud.Match = false;
control.Start(Team.Blue, Settings());
await Until(() => control.Snapshot.State == RunState.Clicking, "HUD success run did not start");
detector.Match = false; hud.Match = true;
count = sink.Count;
await Task.Delay(250);
Check(control.Snapshot.State == RunState.Clicking && sink.Count > count, "Clicks paused before stable HUD confirmation");
hud.Match = false;
await Task.Delay(80);
hud.Match = true;
await Task.Delay(300);
Check(control.Snapshot.State == RunState.Clicking, "Active HUD flicker did not reset confirmation");
await Until(() => control.Snapshot.State == RunState.Stopped, "HUD did not confirm after clicking");
Check(control.Snapshot.Reason.Contains("HUD"), "Wrong HUD stop reason");
count = sink.Count;
await Task.Delay(140);
Check(sink.Count == count, "Click delivered after HUD stop");

// A stale positive dialog result must not suppress HUD success.
detector.Match = true; hud.Match = false;
control.Start(Team.Blue, Settings());
await Until(() => control.Snapshot.State == RunState.Clicking, "Overlapping detection test did not start");
hud.Match = true;
await Until(() => control.Snapshot.State == RunState.Stopped, "Dialog detection suppressed mandatory HUD stop");

hud.Match = false;
control.Start(Team.Blue, Settings());
await Until(() => control.Snapshot.State == RunState.Clicking, "Focus test did not start");
detector.Match = false;
screen.Foreground = false;
await Until(() => control.Snapshot.State == RunState.Stopped, "Focus loss without dialog did not stop");
Check(control.Snapshot.Reason.Contains("fokus"), "Focus loss not explained");
screen.Foreground = true; hud.Match = true;
control.Stop("Manual test stop");
await Task.Delay(650);
Check(control.Snapshot.Reason == "Manual test stop", "HUD detection ran after Stop");
detector.Match = false; screen.Foreground = false;
control.Start(Team.Blue, Settings());
await Task.Delay(650);
Check(control.Snapshot.State == RunState.Waiting, "HUD confirmed without focus");
screen.Foreground = true; hud.Match = false; detector.Match = true;
control.Stop();

// Stop must wait for an in-flight input rather than allowing it to complete afterwards.
using var blockingSink = new BlockingSink();
using var second = new AutomationController(screen, detector, blockingSink, hud);
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

// Group Auto uses this worker to observe the next selection after HUD success.
control.Stop("Observe only", AutomationStopCause.GroupUpdate); detector.Match = true; hud.Match = false;
var beforeObservation = detector.Calls; count = sink.Count;
control.Observe(Settings());
await Until(() => detector.Calls > beforeObservation && control.Snapshot.Detection?.IsMatch == true, "Idle observation never detected the next dialog");
await Task.Delay(300);
Check(sink.Count == count && control.Snapshot.State == RunState.Stopped, "Observation sent input or activated a run");
control.Start(Team.Green, Settings());
await Until(() => control.Snapshot.State == RunState.Clicking, "Group-style restart did not validate the dialog");
hud.Match = true;
await Until(() => control.Snapshot.State == RunState.Stopped && control.Snapshot.StopCause == AutomationStopCause.Joined, "HUD success lacks typed stop cause");
count = sink.Count;
await Task.Delay(300); Check(sink.Count == count, "Idle HUD observation restarted clicking");
control.Observe(null); control.Stop("ESC");
Check(control.Snapshot.StopCause == AutomationStopCause.Manual, "Explicit stop must remain distinguishable from HUD success");
screen.Fail = true; control.Observe(Settings());
await Until(() => control.Snapshot.StopCause == AutomationStopCause.Safety, "Observer failure did not produce a safety stop");
screen.Fail = false; control.Observe(null);

// The input worker must enforce lease expiry without relying on UI timers.
hud.Match = false; detector.Match = true;
control.Start(Team.Blue, Settings(), Stopwatch.GetTimestamp() + Stopwatch.Frequency / 3);
await Until(() => control.Snapshot.State == RunState.Clicking, "Leased group run did not start");
await Until(() => control.Snapshot.State == RunState.Stopped && control.Snapshot.StopCause == AutomationStopCause.GroupUpdate, "Expired lease did not stop on the input worker");
count = sink.Count; await Task.Delay(150); Check(sink.Count == count, "Expired lease continued input");
control.Start(Team.Blue, Settings(), Stopwatch.GetTimestamp() + Stopwatch.Frequency / 3);
await Until(() => control.Snapshot.State == RunState.Clicking, "Lease renewal run did not start");
control.RenewOnlineLease(Stopwatch.GetTimestamp() + Stopwatch.Frequency);
await Task.Delay(400); Check(control.Snapshot.State == RunState.Clicking, "Fresh authorized renewal did not extend the lease");
control.Stop();
Console.WriteLine("PASS: timing validation, armed waiting, stable detection, focus/calibration, continuous clicking beyond former absence timeout, mandatory HUD confirmation/flicker/focus/overlap, geometry, deep settings snapshot, cancellation, concurrent starts, dry-run, capture failure, in-flight Stop synchronization.");

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
