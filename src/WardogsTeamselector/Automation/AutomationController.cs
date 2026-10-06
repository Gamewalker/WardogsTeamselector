using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WardogsTeamselector.Core;

namespace WardogsTeamselector.Automation;

/// <summary>One sampling worker. Updated runs on that worker or caller; UI subscribers must dispatch asynchronously.</summary>
public sealed class AutomationController : IDisposable
{
    private readonly object gate = new();
    private readonly IScreenService screen;
    private readonly IDialogDetector detector;
    private readonly IClickSink sink;
    private readonly IJoinedScreenDetector? joinedDetector;
    private readonly CancellationTokenSource lifetime = new();
    private readonly Task worker;
    private Session? active;
    private long lastClickTimestamp;
    private bool disposed;
    private AutomationSnapshot snapshot = new(RunState.Stopped, null, 0, 0, "Bereit", null, null);
    public event Action<AutomationSnapshot>? Updated;
    public AutomationSnapshot Snapshot { get { lock (gate) return snapshot; } }

    public AutomationController(IScreenService screen, IDialogDetector detector, IClickSink sink, IJoinedScreenDetector? joinedDetector = null)
    {
        this.screen = screen; this.detector = detector; this.sink = sink; this.joinedDetector = joinedDetector;
        worker = Task.Run(RunAsync);
    }

    public void Start(Team team, AppSettings settings)
    {
        var copy = Copy(settings);
        copy.Validate();
        if (!Enum.IsDefined(team)) throw new ArgumentException("Ungültiges Team.");
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            active = new Session(team, copy);
            Publish(new(RunState.Waiting, team, 0, 0, "Warte auf stabilen Dialog und Spielfokus", null, null));
        }
    }

    /// <summary>Synchronizes with input delivery: after return no input from the stopped run can be sent.</summary>
    public void Stop(string reason = "ESC")
    {
        lock (gate) StopLocked(reason);
    }

    private void StopLocked(string reason)
    {
        active = null;
        Publish(snapshot with { State = RunState.Stopped, Reason = reason, AbsenceRemainingMs = 0 });
    }

    private async Task RunAsync()
    {
        try
        {
            while (!lifetime.IsCancellationRequested)
            {
                Session? session;
                lock (gate) session = active;
                if (session is not null) Sample(session);
                await Task.Delay(20, lifetime.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
    }

    private void Sample(Session session)
    {
        try
        {
            var target = screen.ResolveTarget(session.Settings);
            if (target is null)
            {
                lock (gate)
                {
                    if (active != session) return;
                    session.Stable = 0;
                    session.JoinedSince = null; session.JoinedStable = 0;
                    if (session.Started) StopLocked("Spielfenster nicht mehr gefunden");
                    else Publish(snapshot with { Reason = "Warte auf Spielfenster", Geometry = null, Detection = null, JoinedDetection = null });
                }
                return;
            }
            using var frame = screen.Capture(target);
            var detection = detector.Detect(frame, session.Settings);
            var joined = session.Settings.DetectJoinedHud ? joinedDetector?.Detect(frame, session.Settings) : null;
            lock (gate)
            {
                if (active != session) return;
                if (session.Started && !SameGeometry(session.Geometry!, target)) { StopLocked("Spielbereich geändert"); return; }
                var now = Stopwatch.GetTimestamp();
                if (session.Geometry is not null && !SameGeometry(session.Geometry, target))
                {
                    session.Stable = 0; session.JoinedSince = null; session.JoinedStable = 0;
                }
                session.Geometry = target;
                snapshot = snapshot with { Detection = detection, JoinedDetection = joined, Geometry = target };
                if (!target.IsForeground || (!target.IsCalibrated && !session.Settings.DryRun))
                {
                    session.Stable = 0;
                    session.JoinedSince = null; session.JoinedStable = 0;
                    if (session.Started)
                    {
                        StopLocked(!target.IsForeground ? "Spielfokus verloren" : "Geometrie nicht kalibriert");
                    }
                    else Publish(snapshot with { Reason = !target.IsForeground ? "Warte auf Spielfokus" : "Klickfreigabe benötigt kalibrierten Spielbereich" });
                    return;
                }
                if (!detection.IsMatch)
                {
                    session.Stable = 0;
                    // Continuous HUD recognition also ends an armed run before its first click.
                    if (joined?.IsMatch == true)
                    {
                        session.JoinedSince ??= now;
                        session.JoinedStable++;
                        if (session.JoinedStable >= 3 && Stopwatch.GetElapsedTime(session.JoinedSince.Value, now).TotalMilliseconds >= 500)
                        {
                            var hudFresh = screen.ResolveTarget(session.Settings);
                            if (hudFresh is null || !hudFresh.IsForeground || !SameGeometry(target, hudFresh) || (DateTimeOffset.Now - frame.CapturedAt).TotalMilliseconds > 100)
                            {
                                session.JoinedSince = null; session.JoinedStable = 0;
                                if (session.Started) StopLocked("Fokus oder Spielbereich vor Beitrittsbestätigung geändert");
                                return;
                            }
                            StopLocked("HUD: Beitritt erkannt");
                            return;
                        }
                    }
                    else { session.JoinedSince = null; session.JoinedStable = 0; }
                    if (session.Started)
                    {
                        session.AbsentSince ??= now;
                        var remaining = Math.Max(0, session.Settings.DialogAbsenceTimeoutMs - (int)Stopwatch.GetElapsedTime(session.AbsentSince.Value, now).TotalMilliseconds);
                        if (remaining == 0) { StopLocked("Bestätigungsfrist abgelaufen – Auswahldialog nicht zurückgekehrt"); return; }
                        Publish(snapshot with { State = RunState.ConfirmingJoin, AbsenceRemainingMs = remaining, Reason = "Dialog fehlt: warte auf Rückkehr oder Beitrittsbestätigung" });
                    }
                    else Publish(snapshot with { Reason = joined?.IsMatch == true ? "HUD-Beitritt stabilisieren" : "Warte auf Auswahldialog" });
                    return;
                }
                session.AbsentSince = null;
                session.JoinedSince = null; session.JoinedStable = 0;
                snapshot = snapshot with { State = session.Stable < 2 ? RunState.Waiting : snapshot.State, AbsenceRemainingMs = 0 };
                session.Stable++;
                if (session.Stable < 3) { Publish(snapshot with { Detection = detection, Geometry = target, Reason = $"Dialog stabilisieren ({session.Stable}/3)" }); return; }
                var elapsed = lastClickTimestamp == 0 ? double.PositiveInfinity : Stopwatch.GetElapsedTime(lastClickTimestamp, now).TotalMilliseconds;
                if (elapsed < Math.Max(50, session.Interval))
                {
                    Publish(snapshot with { Detection = detection, Geometry = target });
                    return;
                }
                // Revalidate foreground immediately before SendInput; no click based on a stale capture.
                var fresh = screen.ResolveTarget(session.Settings);
                if (fresh is null || !fresh.IsForeground || !SameGeometry(target, fresh) || (!fresh.IsCalibrated && !session.Settings.DryRun))
                {
                    if (session.Started) StopLocked("Fokus oder Spielbereich vor Klick geändert");
                    else session.Stable = 0;
                    return;
                }
                if ((DateTimeOffset.Now - frame.CapturedAt).TotalMilliseconds > 100) { session.Stable = 0; if (session.Started) StopLocked("Bildschirmaufnahme zu alt"); return; }
                var point = session.Settings.Regions.Single(r => r.Team == session.Team).Center(target.Bounds);
                if (!session.Settings.DryRun) sink.Click(point);
                lastClickTimestamp = Stopwatch.GetTimestamp();
                session.Started = true;
                session.Count++;
                session.Interval = Random.Shared.Next(session.Settings.MinIntervalMs, session.Settings.MaxIntervalMs + 1);
                Publish(new(RunState.Clicking, session.Team, session.Count, session.Interval, session.Settings.DryRun ? "Testmodus: Klick simuliert" : "Klick gesendet", detection, target, joined));
            }
        }
        catch (Exception ex)
        {
            lock (gate) if (active == session) StopLocked($"Aufnahme/Steuerung fehlgeschlagen: {ex.Message}");
        }
    }

    private static bool SameGeometry(TargetGeometry a, TargetGeometry b) => a.Bounds == b.Bounds && a.WindowHandle == b.WindowHandle && a.IsCalibrated == b.IsCalibrated;
    private void Publish(AutomationSnapshot value)
    {
        snapshot = value;
        // A debug/UI subscriber cannot terminate the input worker.
        foreach (var callback in Updated?.GetInvocationList() ?? Array.Empty<Delegate>())
            try { ((Action<AutomationSnapshot>)callback)(value); } catch { }
    }

    private static AppSettings Copy(AppSettings value) => new()
    {
        MinIntervalMs = value.MinIntervalMs, MaxIntervalMs = value.MaxIntervalMs,
        DetectionThreshold = value.DetectionThreshold, MonitorId = value.MonitorId,
        DialogAbsenceTimeoutMs = value.DialogAbsenceTimeoutMs, DetectJoinedHud = value.DetectJoinedHud,
        LivePreviewEnabled = value.LivePreviewEnabled,
        WindowTitleContains = value.WindowTitleContains, ProcessNameContains = value.ProcessNameContains, DryRun = value.DryRun,
        GeometryCalibrated = value.GeometryCalibrated, ManualBounds = value.ManualBounds,
        DetectionOffsetX = value.DetectionOffsetX, DetectionOffsetY = value.DetectionOffsetY,
        DetectionScale = value.DetectionScale, Hotkeys = new(value.Hotkeys), Regions = new(value.Regions)
    };

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            StopLocked("Anwendung beendet");
            lifetime.Cancel();
        }
        if (Task.CurrentId != worker.Id) worker.GetAwaiter().GetResult();
        lifetime.Dispose();
    }

    private sealed class Session(Team team, AppSettings settings)
    {
        public Team Team { get; } = team;
        public AppSettings Settings { get; } = settings;
        public int Stable;
        public int JoinedStable;
        public long? JoinedSince;
        public long? AbsentSince;
        public int Interval;
        public bool Started;
        public long Count;
        public TargetGeometry? Geometry;
    }
}
