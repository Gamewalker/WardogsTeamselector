using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using WardogsTeamselector.Automation;
using WardogsTeamselector.Core;
using WardogsTeamselector.Detection;
using WardogsTeamselector.Platform;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Rectangle = System.Drawing.Rectangle;

namespace WardogsTeamselector;
public sealed partial class MainWindow : Window
{
    private readonly ScreenService screen = new();
    private readonly DialogDetector detector = new();
    private readonly JoinedScreenDetector joinedDetector = new();
    private readonly AutomationController automation;
    private HotkeyService? hotkeys;
    private AppSettings settings = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private bool previewBusy, closing, referenceMode, dirty, loadingFields, hasSavedProfile, regionDirty;
    private readonly bool smokeMode;
    private Team editingTeam = Team.Blue;
    private readonly Dictionary<string, int> fieldPages = new();
    private volatile bool liveUpdatesEnabled = true;
    private readonly object uiUpdateGate = new();
    private AutomationSnapshot? pendingUiSnapshot;
    private bool uiUpdateQueued;
    private long lastUiUpdate;
    private RunState? lastUiState;
    private Team? lastUiTeam;
    private string? lastUiReason;
    private readonly Dictionary<string, TextBox> fields = new();
    private readonly Dictionary<Team, ComboBox> keyBoxes = new();
    private readonly Dictionary<Team, Button> teamButtons = new();
    private readonly ComboBox monitor = new(), selectedTeam = new();
    private readonly CheckBox dryRun = new() { Content = "Testmodus – keine Mauseingaben", IsChecked = true }, calibrated = new() { Content = "Geometrie für dieses Profil geprüft" };
    private readonly CheckBox liveUpdates = new() { Content = "Live-Bild und Detaildiagnose aktualisieren", IsChecked = true };
    private readonly CheckBox focusGame = new() { Content = "Spiel nach Teamaktivierung in den Vordergrund holen", IsChecked = true };
    private readonly TextBlock status = new(), geometryText = new(), detectionText = new(), errorText = new(), counters = new();
    private readonly TextBlock joinedText = new() { Foreground = Muted, Margin = new Thickness(0, 4, 0, 0) };
    private readonly System.Windows.Controls.Image preview = new() { Stretch = Stretch.Uniform };
    private readonly Canvas overlay = new() { Background = Brushes.Transparent };
    private readonly Grid previewHost = new();
    private readonly DataGrid probes = new() { IsReadOnly = true, AutoGenerateColumns = true, HeadersVisibility = DataGridHeadersVisibility.Column, MinHeight = 160 };
    private readonly TextBox log = new() { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MinHeight = 100 };
    private readonly List<string> logLines = new();
    private BitmapSource? lastImage;
    private TargetGeometry? lastGeometry;
    private Point? dragStart;
    private Point? dragEnd;
    private readonly System.Windows.Shapes.Rectangle dragOutline = new()
    {
        Stroke = Brushes.White, StrokeThickness = 2, StrokeDashArray = new DoubleCollection { 4, 2 },
        Fill = new SolidColorBrush(Color.FromArgb(24, 255, 255, 255)), IsHitTestVisible = false
    };
    private string? startupSettingsError;
    private static readonly Brush Muted = new SolidColorBrush(Color.FromRgb(159, 177, 185));

    public MainWindow(bool registerGlobalHotkeys = true)
    {
        smokeMode = !registerGlobalHotkeys;
        Title = $"WardogsTeamselector · {BuildDescription}"; Width = 1180; Height = 820; MinWidth = 920; MinHeight = 660;
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/app.ico"));
        Background = BrushFrom(28, 30, 34); Foreground = Brushes.WhiteSmoke; FontFamily = new System.Windows.Media.FontFamily("Segoe UI"); FontSize = 14;
        Resources.MergedDictionaries.Add(CreateTheme());
        UseLayoutRounding = true;
        automation = new(screen, detector, new WindowsClickSink(), joinedDetector);
        automation.Updated += OnAutomation;
        try { settings = SettingsStore.Load(); hasSavedProfile = File.Exists(SettingsStore.FilePath); } catch (Exception ex) { startupSettingsError = "Gespeichertes Profil ungültig: " + ex.Message + " Unter Einrichtung und Konfiguration prüfen, dann speichern."; }
        Build(); LoadFields();
        Loaded += (_, _) => InitializeUpdates();
        Closed += (_, _) => FinishUpdates();
        pages.SelectedIndex = hasSavedProfile && startupSettingsError == null ? 1 : 0;
        UpdatePreviewLocation();
        SourceInitialized += (_, _) => { if (!registerGlobalHotkeys) return; hotkeys = new(this); hotkeys.TeamPressed += StartTeam; hotkeys.EscapePressed += () => automation.Stop("ESC – abgebrochen"); RegisterKeys(); };
        Loaded += async (_, _) => { if (startupSettingsError != null) ShowError(startupSettingsError); await RefreshPreview(); UpdatePreviewTimer(); };
        timer.Tick += async (_, _) => await RefreshPreview();
        Closing += (_, e) =>
        {
            if (dirty && !smokeMode)
            {
                var choice = MessageBox.Show(this, "Änderungen vor dem Schließen speichern?", "Ungespeicherte Einstellungen", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (choice == MessageBoxResult.Cancel || (choice == MessageBoxResult.Yes && !SaveSettings())) { e.Cancel = true; return; }
            }
            closing = true; timer.Stop(); automation.Dispose(); hotkeys?.Dispose();
        };
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) automation.Stop("ESC – abgebrochen"); };
    }

    private void LoadFields()
    {
        loadingFields = true;
        try
        {
        fields["min"].Text = settings.MinIntervalMs.ToString(); fields["max"].Text = settings.MaxIntervalMs.ToString(); fields["title"].Text = settings.WindowTitleContains;
        fields["process"].Text = settings.ProcessNameContains;
        liveUpdates.IsChecked = settings.LivePreviewEnabled;
        focusGame.IsChecked = settings.FocusGameOnTeamActivation;
        fields["threshold"].Text = Format(settings.DetectionThreshold * 100); fields["offsetx"].Text = Format(settings.DetectionOffsetX * 100); fields["offsety"].Text = Format(settings.DetectionOffsetY * 100); fields["scale"].Text = Format(settings.DetectionScale);
        fields["bounds"].Text = settings.ManualBounds is Rectangle r ? $"{r.X},{r.Y},{r.Width},{r.Height}" : ""; dryRun.IsChecked = settings.DryRun; calibrated.IsChecked = settings.GeometryCalibrated;
        foreach (var t in Enum.GetValues<Team>()) keyBoxes[t].SelectedIndex = settings.Hotkeys[t] - 0x70;
        RefreshMonitors(false); LoadRegion();
        }
        finally { loadingFields = false; }
        SetLiveUpdates(settings.LivePreviewEnabled);
        UpdateProfileState();
    }
    private AppSettings ReadFields()
    {
        int minimum = Integer("min", "Minimum: eine ganze Zahl von 50 bis 60000 ms eingeben.");
        int maximum = Integer("max", "Maximum: eine ganze Zahl von 50 bis 60000 ms eingeben.");
        if (minimum < 50 || minimum > 60000) throw FieldError("min", "Das minimale Klickintervall muss zwischen 50 und 60000 ms liegen.");
        if (maximum < minimum || maximum > 60000) throw FieldError("max", "Das Maximum muss mindestens so groß wie das Minimum sein und darf 60000 ms nicht überschreiten.");
        if (string.IsNullOrWhiteSpace(fields["title"].Text)) throw FieldError("title", "Einen Teil des Spielfenstertitels eingeben, z. B. wardogs.");
        if (string.IsNullOrWhiteSpace(fields["process"].Text)) throw FieldError("process", "Einen Teil des Prozessnamens ohne .exe eingeben, z. B. wardogs.");
        double threshold = Number("threshold"), scale = Number("scale");
        if (threshold < 50 || threshold > 100) throw FieldError("threshold", "Die Erkennungsschwelle muss zwischen 50 und 100 % liegen.");
        if (scale < .25 || scale > 4) throw FieldError("scale", "Die Dialogskalierung muss zwischen 0,25 und 4 liegen (Standard: 1).");
        var regions = settings.Regions.ToList();
        if (regionDirty) { var region = ReadRegion(editingTeam); regions = regions.Select(r => r.Team == editingTeam ? region : r).ToList(); }
        var keys = keyBoxes.ToDictionary(k => k.Key, k => 0x70 + k.Value.SelectedIndex);
        var conflict = keys.GroupBy(pair => pair.Value).FirstOrDefault(group => group.Count() > 1);
        if (conflict != null)
        {
            var team = conflict.Last().Key;
            var names = string.Join(" und ", conflict.Select(pair => TeamName(pair.Key)));
            var message = $"{names} verwenden F{conflict.Key - 0x6F}. Für {TeamName(team)} eine andere F-Taste wählen.";
            FocusInput(keyBoxes[team], 2, message);
            throw new ArgumentException(message);
        }
        var s = new AppSettings { MinIntervalMs = minimum, MaxIntervalMs = maximum, WindowTitleContains = fields["title"].Text.Trim(), ProcessNameContains = fields["process"].Text.Trim(), DetectionThreshold = threshold / 100, DetectionOffsetX = Number("offsetx") / 100, DetectionOffsetY = Number("offsety") / 100, DetectionScale = scale, DryRun = dryRun.IsChecked == true, GeometryCalibrated = calibrated.IsChecked == true, MonitorId = (monitor.SelectedItem as MonitorChoice)?.Id, Regions = regions, Hotkeys = keys };
        if (!string.IsNullOrWhiteSpace(fields["bounds"].Text))
        {
            var parts = fields["bounds"].Text.Split(',');
            var values = new int[4];
            if (parts.Length != 4 || parts.Where((part, index) => !int.TryParse(part.Trim(), out values[index])).Any()) throw FieldError("bounds", "Den Spielbereich als vier ganze Zahlen eingeben: X,Y,Breite,Höhe. Leer lassen für Automatik.");
            if (values[2] < 100 || values[3] < 100) throw FieldError("bounds", "Breite und Höhe des manuellen Spielbereichs müssen mindestens 100 Pixel betragen.");
            s.ManualBounds = new(values[0], values[1], values[2], values[3]);
        }
        s.LivePreviewEnabled = liveUpdatesEnabled;
        s.FocusGameOnTeamActivation = focusGame.IsChecked == true;
        s.Validate(); return s;
    }
    private void RefreshMonitors() => RefreshMonitors(true);
    private void RefreshMonitors(bool preserveSelection)
    {
        string? id = preserveSelection && monitor.SelectedItem is MonitorChoice choice ? choice.Id : settings.MonitorId;
        bool wasLoading = loadingFields;
        loadingFields = true;
        try
        {
        var items = new List<MonitorChoice> { new(null, "Automatisch · Spielfenster") }; items.AddRange(screen.GetMonitors().Select(m => new MonitorChoice(m.Id, $"{m.Name} · {m.Bounds.Width}×{m.Bounds.Height} · ({m.Bounds.X},{m.Bounds.Y})")));
        if (id != null && !items.Any(m => m.Id == id)) items.Add(new(id, "Nicht verbunden · " + id)); monitor.ItemsSource = items; monitor.DisplayMemberPath = "Name"; monitor.SelectedItem = items.FirstOrDefault(m => m.Id == id) ?? items[0];
        }
        finally { loadingFields = wasLoading; }
    }
    private bool SaveSettings()
    {
        try
        {
            automation.Stop("Einstellungen gespeichert");
            var next = ReadFields(); ApplyHotkeys(next); SettingsStore.Save(next);
            settings = next; startupSettingsError = null; dirty = false; hasSavedProfile = true;
            LoadRegion(); ClearError(); UpdateProfileState(); DrawOverlay();
            AddLog("Einstellungen gespeichert · " + SettingsStore.FilePath);
            return true;
        }
        catch (Exception ex) { ShowError(ex.Message); return false; }
    }
    private void ApplyHotkeys(AppSettings next)
    {
        try { hotkeys?.Apply(next); foreach (var team in Enum.GetValues<Team>()) { teamKeyLabels[team].Text = "F" + (next.Hotkeys[team] - 0x6F); teamKeyLabels[team].FontSize = 26; teamButtons[team].ToolTip = "Einmal drücken aktiviert das Team. ESC bricht ab."; } }
        catch { foreach (var team in Enum.GetValues<Team>()) { teamKeyLabels[team].Text = "Hotkey inaktiv"; teamKeyLabels[team].FontSize = 16; teamButtons[team].ToolTip = "Hotkey-Konflikt: Unter Konfiguration andere Tasten wählen und speichern. Die Teamtasten bleiben verfügbar."; } throw; }
        finally { UpdateRunDisplay(automation.Snapshot); }
    }
    private void RegisterKeys() { try { ApplyHotkeys(settings); } catch (Exception ex) { ShowError(ex.Message); } }
    private void ResetSettings()
    {
        if (!smokeMode && MessageBox.Show(this, "Standardwerte laden? Intervall, Hotkeys, Spielbereich und Teamflächen werden zurückgesetzt. Die gespeicherte Datei bleibt bis zum Speichern erhalten.", "Profil zurücksetzen", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        automation.Stop("Standardwerte geladen"); settings = new(); LoadFields(); RegisterKeys(); dirty = true;
        ClearError(); UpdateProfileState(); AddLog("Standardprofil geladen. Zum Behalten speichern."); DrawOverlay();
    }

    private void DiscardChanges()
    {
        try { automation.Stop("Änderungen verworfen"); settings = SettingsStore.Load(); hasSavedProfile = File.Exists(SettingsStore.FilePath); dirty = false; startupSettingsError = null; LoadFields(); RegisterKeys(); ClearError(); DrawOverlay(); AddLog("Ungespeicherte Änderungen verworfen."); }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private void ApplyPreviewSettings() { if (ApplyPreviewSettingsCore()) { if (!PreviewVisible) ShowPage(0); _ = RefreshPreview(true); } }

    private bool ApplyPreviewSettingsCore()
    {
        try { automation.Stop("Einrichtung prüfen"); settings = ReadFields(); LoadRegion(); referenceMode = false; ClearError(); UpdateProfileState(); UpdatePreviewTimer(); return true; }
        catch (Exception ex) { ShowError(ex.Message); return false; }
    }
    private void StartTeam(Team team)
    {
        try
        {
            if (startupSettingsError != null) throw new InvalidOperationException(startupSettingsError);
            var next = ReadFields();
            try { if (dirty) ApplyHotkeys(next); }
            catch (Exception ex) { AddLog("Tastenkürzel nicht verfügbar: " + ex.Message); }
            settings = next; LoadRegion(); referenceMode = false; ClearError(); ShowPage(1);
            UpdateProfileState(); automation.Start(team, next);
            if (!smokeMode && next.FocusGameOnTeamActivation && !screen.TryBringGameToForeground(next))
                AddLog("Spielfenster konnte nicht in den Vordergrund geholt werden. Zum Spiel wechseln; die Teamaktivierung wartet auf Spielfokus.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }
    private void OnAutomation(AutomationSnapshot snapshot)
    {
        if (closing) return;
        lock (uiUpdateGate)
        {
            pendingUiSnapshot = snapshot;
            var now = Stopwatch.GetTimestamp();
            bool changed = lastUiState != snapshot.State || lastUiTeam != snapshot.Team || lastUiReason != snapshot.Reason;
            if (uiUpdateQueued || (!changed && lastUiUpdate != 0 && Stopwatch.GetElapsedTime(lastUiUpdate, now).TotalMilliseconds < (liveUpdatesEnabled ? 250 : 1000))) return;
            uiUpdateQueued = true; lastUiUpdate = now; lastUiState = snapshot.State; lastUiTeam = snapshot.Team; lastUiReason = snapshot.Reason;
        }
        Dispatcher.BeginInvoke(() =>
        {
            AutomationSnapshot current;
            lock (uiUpdateGate) { current = pendingUiSnapshot!; uiUpdateQueued = false; }
            if (closing) return;
            UpdateRunDisplay(current);
            if (current.State != RunState.Stopped) CancelRegionDrag();
            if (liveUpdatesEnabled && !referenceMode && current.Detection != null) UpdateDetection(current.Detection, current.JoinedDetection);
            if (logLines.LastOrDefault()?.EndsWith(current.Reason) != true) AddLog(current.Reason);
        });
    }
    private void SetLiveUpdates(bool enabled)
    {
        liveUpdatesEnabled = enabled; settings.LivePreviewEnabled = enabled;
        if (enabled) { UpdatePreviewTimer(); if (IsLoaded) _ = RefreshPreview(); }
        else
        {
            timer.Stop(); referenceMode = false; preview.Source = null; lastImage = null; lastGeometry = null;
            overlay.Children.Clear(); probes.ItemsSource = null;
            geometryText.Text = "Automatische Vorschau aus. Erkennung und Klicksteuerung bleiben aktiv.";
            detectionText.Text = "Detaildiagnose pausiert"; joinedText.Text = "";
            SetPreviewEmpty("Vorschau pausiert", "„Live-Bild laden“ erstellt eine einzelne Aufnahme. Automatische Aktualisierung lässt sich unter Konfiguration einschalten.");
        }
    }
    private async Task RefreshPreview(bool force = false)
    {
        if ((!liveUpdatesEnabled && !force) || !PreviewVisible || previewBusy || closing || referenceMode || dragStart != null) return; previewBusy = true;
        try
        {
            // Never use partially edited UI settings to control an active run.
            var config = settings;
            var result = await Task.Run(() => { var target = screen.ResolveTarget(config); if (target == null) return (Image: (BitmapSource?)null, Target: (TargetGeometry?)null, Detection: (DetectionResult?)null, Joined: (DetectionResult?)null); using var f = screen.Capture(target); var d = detector.Detect(f, config); var joined = joinedDetector.Detect(f, config); return (Image: ToSource(f.Bitmap), Target: target, Detection: d, Joined: joined); });
            if (closing || referenceMode || (!liveUpdatesEnabled && !force) || settings != config || dragStart != null) return;
            if (result.Target == null)
            {
                lastGeometry = null; lastImage = null; preview.Source = null; overlay.Children.Clear(); probes.ItemsSource = null;
                geometryText.Text = "Kein passendes Spielfenster gefunden.";
                setupFeedback.Text = "Wardogs starten. Stimmen Fenstertitel, Prozessname und gewählter Monitor?";
                detectionText.Text = "Dialog: kein Bild verfügbar"; joinedText.Text = "";
                SetPreviewEmpty("Spielfenster fehlt", "Wardogs öffnen. Unter Einrichtung den Titel- und Prozessfilter prüfen und dann „Spiel suchen / Bild laden“ wählen.");
                return;
            }
            lastGeometry = result.Target; lastImage = result.Image; preview.Source = lastImage;
            previewEmpty.Visibility = Visibility.Collapsed;
            previewSource.Text = liveUpdatesEnabled ? "Live-Bild · automatische Aktualisierung" : "Einzelaufnahme · automatische Vorschau aus";
            var r = result.Target.Bounds; geometryText.Text = $"{result.Target.Description}\n{r.Width}×{r.Height} px · Ursprung ({r.X},{r.Y}) · Fokus: {(result.Target.IsForeground ? "Spiel" : "anderes Fenster")} · Geometrie: {(result.Target.IsCalibrated ? "freigegeben" : "Kalibrierung nötig")}";
            UpdateDetection(result.Detection!, result.Joined); DrawOverlay();
            setupFeedback.Text = result.Target.IsCalibrated ? "Spielfenster gefunden. Teamrahmen prüfen; im Testmodus den Ablauf kontrollieren." : "Spielfenster gefunden. Abweichende Geometrie: Teamflächen prüfen und anschließend bestätigen.";
        }
        catch (Exception ex) { geometryText.Text = "Aufnahme nicht verfügbar: " + ex.Message; preview.Source = null; lastImage = null; lastGeometry = null; overlay.Children.Clear(); probes.ItemsSource = null; detectionText.Text = "Dialog: kein Bild verfügbar"; joinedText.Text = ""; SetPreviewEmpty("Aufnahme fehlgeschlagen", "Spielfenster sichtbar machen und „Live-Bild laden“ erneut wählen. Details stehen oben und im Diagnoseprotokoll."); }
        finally { previewBusy = false; }
    }
    private void LoadReference() => LoadEmbeddedReference("WardogsTeamselector.Reference", "Referenzbild");
    private void LoadJoinedReference() => LoadEmbeddedReference("WardogsTeamselector.Joined", "Folgescreen-Referenz");
    private void LoadEmbeddedReference(string resource, string title)
    {
        try
        {
            var config = ReadFields(); settings = config; LoadRegion();
            automation.Stop("Referenzprüfung"); referenceMode = true; UpdatePreviewTimer(); ClearError();
            using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream(resource) ?? throw new FileNotFoundException("Referenzbild fehlt.");
            using var bmp = new Bitmap(stream);
            var g = new TargetGeometry(new(0, 0, bmp.Width, bmp.Height), IntPtr.Zero, false, title, true);
            using var frame = new CaptureFrame((Bitmap)bmp.Clone(), g);
            lastImage = ToSource(bmp); preview.Source = lastImage; lastGeometry = g;
            previewEmpty.Visibility = Visibility.Collapsed; previewSource.Text = title + " · statisch, kein Live-Bild";
            geometryText.Text = $"{bmp.Width}×{bmp.Height} px · eingebettetes Testbild · keine Eingaben";
            UpdateDetection(detector.Detect(frame, config), joinedDetector.Detect(frame, config)); DrawOverlay();
            AddLog(title + " geprüft: " + detectionText.Text + " · " + joinedText.Text);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }
    private void UpdateDetection(DetectionResult d, DetectionResult? joined = null) { detectionText.Text = $"Dialog: {(d.IsMatch ? "erkannt" : "nicht erkannt")} · Score {d.Score:P0}"; detectionText.Foreground = d.IsMatch ? new SolidColorBrush(Color.FromRgb(120, 220, 160)) : Muted; joinedText.Text = joined == null ? "" : $"Folgescreen: {(joined.IsMatch ? "HUD erkannt" : "nicht erkannt")} · Score {joined.Score:P0}"; probes.ItemsSource = d.Probes.Concat(joined?.Probes.Select(p => p with { Name = "HUD · " + p.Name }) ?? Enumerable.Empty<ProbeResult>()).ToArray(); }
    private static BitmapSource ToSource(Bitmap bitmap) { using var stream = new MemoryStream(); bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png); stream.Position = 0; var img = new BitmapImage(); img.BeginInit(); img.CacheOption = BitmapCacheOption.OnLoad; img.StreamSource = stream; img.EndInit(); img.Freeze(); return img; }
    private (double X, double Y, double W, double H) ImageArea()
    {
        if (lastImage == null) return (0, 0, 0, 0); var scale = Math.Min(previewHost.ActualWidth / lastImage.PixelWidth, previewHost.ActualHeight / lastImage.PixelHeight); var w = lastImage.PixelWidth * scale; var h = lastImage.PixelHeight * scale; return ((previewHost.ActualWidth - w) / 2, (previewHost.ActualHeight - h) / 2, w, h);
    }
    private void DrawOverlay()
    {
        overlay.Children.Clear(); var a = ImageArea(); if (a.W <= 0) return;
        foreach (var probe in DialogDetector.GetProbeAreas(new System.Drawing.Size(lastImage!.PixelWidth, lastImage.PixelHeight), settings)) { var p = probe.Bounds; var box = new System.Windows.Shapes.Rectangle { Width = Math.Max(2, p.Width * a.W / lastImage.PixelWidth), Height = Math.Max(2, p.Height * a.H / lastImage.PixelHeight), Stroke = Brushes.Gold, StrokeThickness = 1, IsHitTestVisible = false }; Canvas.SetLeft(box, a.X + p.X * a.W / lastImage.PixelWidth); Canvas.SetTop(box, a.Y + p.Y * a.H / lastImage.PixelHeight); overlay.Children.Add(box); }
        foreach (var probe in JoinedScreenDetector.GetProbeAreas(new System.Drawing.Size(lastImage.PixelWidth, lastImage.PixelHeight))) { var p = probe.Bounds; var box = new System.Windows.Shapes.Rectangle { Width = Math.Max(2, p.Width * a.W / lastImage.PixelWidth), Height = Math.Max(2, p.Height * a.H / lastImage.PixelHeight), Stroke = Brushes.Cyan, StrokeThickness = 1, IsHitTestVisible = false }; Canvas.SetLeft(box, a.X + p.X * a.W / lastImage.PixelWidth); Canvas.SetTop(box, a.Y + p.Y * a.H / lastImage.PixelHeight); overlay.Children.Add(box); }
        foreach (var r in settings.Regions) { var box = new System.Windows.Shapes.Rectangle { Width = r.Width * a.W, Height = r.Height * a.H, Stroke = TeamBrush(r.Team), StrokeThickness = 2, IsHitTestVisible = false }; Canvas.SetLeft(box, a.X + r.X * a.W); Canvas.SetTop(box, a.Y + r.Y * a.H); overlay.Children.Add(box); var dot = new System.Windows.Shapes.Ellipse { Width = 8, Height = 8, Fill = TeamBrush(r.Team), IsHitTestVisible = false }; Canvas.SetLeft(dot, a.X + (r.X + r.Width / 2) * a.W - 4); Canvas.SetTop(dot, a.Y + (r.Y + r.Height / 2) * a.H - 4); overlay.Children.Add(dot); }
        if (dragStart is Point start && dragEnd is Point end) DrawDragSelection(start, end);
    }
    private void PreviewDown(object sender, MouseButtonEventArgs e)
    {
        if (pages.SelectedIndex != 0 || drawRegion.IsChecked != true || automation.Snapshot.State != RunState.Stopped || lastImage == null) return;
        var point = e.GetPosition(overlay); var area = ImageArea();
        if (point.X < area.X || point.Y < area.Y || point.X > area.X + area.W || point.Y > area.Y + area.H) return;
        dragStart = point; DrawDragSelection(point, point);
        if (!overlay.CaptureMouse()) CancelRegionDrag();
        e.Handled = true;
    }
    private void PreviewMove(object sender, MouseEventArgs e)
    {
        if (dragStart is not Point start || !overlay.IsMouseCaptured) return;
        if (e.LeftButton != MouseButtonState.Pressed) { CancelRegionDrag(); return; }
        DrawDragSelection(start, e.GetPosition(overlay));
    }

    private void DrawDragSelection(Point start, Point end)
    {
        var area = ImageArea();
        end = new Point(Math.Clamp(end.X, area.X, area.X + area.W), Math.Clamp(end.Y, area.Y, area.Y + area.H));
        dragEnd = end;
        Canvas.SetLeft(dragOutline, Math.Min(start.X, end.X));
        Canvas.SetTop(dragOutline, Math.Min(start.Y, end.Y));
        dragOutline.Width = Math.Abs(end.X - start.X); dragOutline.Height = Math.Abs(end.Y - start.Y);
        if (!overlay.Children.Contains(dragOutline)) overlay.Children.Add(dragOutline);
    }

    private void CancelRegionDrag() { dragStart = null; dragEnd = null; overlay.Children.Remove(dragOutline); if (overlay.IsMouseCaptured) overlay.ReleaseMouseCapture(); }
    private void PreviewUp(object sender, MouseButtonEventArgs e)
    {
        if (dragStart is not Point start) return;
        CancelRegionDrag();
        if (pages.SelectedIndex != 0 || drawRegion.IsChecked != true || automation.Snapshot.State != RunState.Stopped) return;
        var end = e.GetPosition(overlay); var a = ImageArea(); if (a.W <= 0) return;
        var x1 = Math.Clamp((Math.Min(start.X, end.X) - a.X) / a.W, 0, 1); var y1 = Math.Clamp((Math.Min(start.Y, end.Y) - a.Y) / a.H, 0, 1); var x2 = Math.Clamp((Math.Max(start.X, end.X) - a.X) / a.W, 0, 1); var y2 = Math.Clamp((Math.Max(start.Y, end.Y) - a.Y) / a.H, 0, 1);
        if (x2 - x1 < 0.005 || y2 - y1 < 0.005) return; var t = (selectedTeam.SelectedItem as TeamChoice)?.Team ?? Team.Blue; settings.Regions = settings.Regions.Select(r => r.Team == t ? new TeamRegion(t, x1, y1, x2 - x1, y2 - y1) : r).ToList(); LoadRegion(); MarkDirty(); DrawOverlay(); AddLog("Teamfläche geändert: " + TeamName(t));
    }
    private void LoadRegion()
    {
        if (!fields.ContainsKey("rx")) return;
        bool wasLoading = loadingFields; loadingFields = true;
        editingTeam = (selectedTeam.SelectedItem as TeamChoice)?.Team ?? Team.Blue;
        var region = settings.Regions.First(r => r.Team == editingTeam);
        fields["rx"].Text = Format(region.X * 100); fields["ry"].Text = Format(region.Y * 100);
        fields["rw"].Text = Format(region.Width * 100); fields["rh"].Text = Format(region.Height * 100);
        regionDirty = false; loadingFields = wasLoading;
    }

    private TeamRegion ReadRegion(Team team)
    {
        var region = new TeamRegion(team, Number("rx") / 100, Number("ry") / 100, Number("rw") / 100, Number("rh") / 100);
        if (region.X < 0 || region.X >= 1) throw FieldError("rx", "Links muss zwischen 0 und weniger als 100 % liegen.");
        if (region.Y < 0 || region.Y >= 1) throw FieldError("ry", "Oben muss zwischen 0 und weniger als 100 % liegen.");
        if (region.Width <= 0 || region.X + region.Width > 1) throw FieldError("rw", "Die Breite muss größer als 0 sein; die Teamfläche muss vollständig im Bild liegen.");
        if (region.Height <= 0 || region.Y + region.Height > 1) throw FieldError("rh", "Die Höhe muss größer als 0 sein; die Teamfläche muss vollständig im Bild liegen.");
        return region;
    }

    private void ChangeSelectedTeam()
    {
        if (loadingFields) return;
        try
        {
            if (regionDirty)
            {
                var region = ReadRegion(editingTeam);
                settings.Regions = settings.Regions.Select(r => r.Team == editingTeam ? region : r).ToList();
            }
            LoadRegion(); DrawOverlay();
        }
        catch (Exception ex)
        {
            loadingFields = true;
            selectedTeam.SelectedItem = selectedTeam.Items.Cast<TeamChoice>().First(t => t.Team == editingTeam);
            loadingFields = false; ShowError(ex.Message);
        }
    }

    private void ApplyRegion()
    {
        try
        {
            var region = ReadRegion(editingTeam);
            automation.Stop("Teamfläche geändert");
            settings.Regions = settings.Regions.Select(r => r.Team == editingTeam ? region : r).ToList();
            LoadRegion(); MarkDirty(); ClearError(); DrawOverlay(); AddLog("Teamfläche übernommen: " + TeamName(editingTeam));
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }
    private void SaveImage() { if (lastImage == null) { ShowError("Zuerst ein Bild aufnehmen."); return; } var dialog = new SaveFileDialog { Filter = "PNG-Bild|*.png", FileName = "wardogs-aufnahme.png" }; if (dialog.ShowDialog() != true) return; try { var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(lastImage)); using var file = File.Create(dialog.FileName); encoder.Save(file); } catch (Exception ex) { ShowError(ex.Message); } }
    private void ExportLog() { var dialog = new SaveFileDialog { Filter = "Textdatei|*.txt", FileName = "wardogs-diagnose.txt" }; if (dialog.ShowDialog() != true) return; try { File.WriteAllText(dialog.FileName, "WardogsTeamselector\n" + geometryText.Text + "\n" + detectionText.Text + "\n" + string.Join("\n", logLines) + "\n\nMessflächen:\n" + string.Join("\n", (probes.ItemsSource as IReadOnlyList<ProbeResult> ?? Array.Empty<ProbeResult>()).Select(p => $"{p.Name}: {p.Score:F3} | Soll {p.Expected} | Ist {p.Actual}"))); } catch (Exception ex) { ShowError(ex.Message); } }
    private void ShowError(string message) { errorText.Text = message; errorText.Visibility = Visibility.Visible; AddLog("Fehler: " + message); }
    private void ClearError() { errorText.Text = ""; errorText.Visibility = Visibility.Collapsed; }
    private void AddLog(string message) { logLines.Add(DateTime.Now.ToString("HH:mm:ss.fff") + "  " + message); if (logLines.Count > 500) logLines.RemoveAt(0); log.Text = string.Join(Environment.NewLine, logLines); log.ScrollToEnd(); }
    private void MarkDirty(bool stopRun = true) { if (loadingFields) return; dirty = true; if (stopRun && automation.Snapshot.State != RunState.Stopped) automation.Stop("Einstellungen bearbeitet"); UpdateProfileState(); }
    private void Changed(object sender, RoutedEventArgs e) => MarkDirty();
    private double Number(string key)
    {
        var text = fields[key].Text.Trim().Replace(',', '.');
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !double.IsFinite(number)) throw FieldError(key, "Eine gültige Zahl eingeben. Dezimalstellen können mit Komma oder Punkt getrennt werden.");
        return number;
    }
    private int Integer(string key, string message) { if (!int.TryParse(fields[key].Text.Trim(), out int number)) throw FieldError(key, message); return number; }
    private ArgumentException FieldError(string key, string message)
    {
        var box = fields[key]; FocusInput(box, fieldPages[key], message);
        return new ArgumentException(message);
    }

    private void FocusInput(Control input, int page, string message)
    {
        input.BorderBrush = ErrorBrush; input.ToolTip = message;
        if (input.Parent is Border frame) frame.BorderBrush = ErrorBrush;
        ShowPage(page);
        DependencyObject? parent = input;
        while (parent != null) { if (parent is Expander expander) expander.IsExpanded = true; parent = VisualTreeHelper.GetParent(parent); }
        Dispatcher.BeginInvoke(() => { input.BringIntoView(); input.Focus(); if (input is TextBox box) box.SelectAll(); });
    }
    private static string Format(double n) => n.ToString("0.####", CultureInfo.CurrentCulture);
    private static TextBlock Heading(string text) => new() { Text = text, Foreground = Brushes.WhiteSmoke, FontSize = 17, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 24, 0, 12), TextWrapping = TextWrapping.Wrap };
    private static Button Button(string text, Action action)
    {
        var b = new Button { Content = IconLabel(text, IconForAction(text)), Margin = new Thickness(0, 0, 8, 8) };
        AutomationProperties.SetName(b, text);
        b.Click += (_, _) => action();
        return b;
    }
    private static string TeamName(Team t) => t switch { Team.Blue => "Blau", Team.Red => "Rot", _ => "Grün" };
    private static Brush TeamBrush(Team t) => new SolidColorBrush(t switch { Team.Blue => Color.FromRgb(22, 112, 163), Team.Red => Color.FromRgb(164, 56, 53), _ => Color.FromRgb(34, 124, 77) });
    private sealed record MonitorChoice(string? Id, string Name);
    private sealed record TeamChoice(Team Team, string Name);
    private void SaveRender(string path, Window? window = null)
    {
        // Render the actual client surface; Window bounds also include non-rendered OS chrome.
        window ??= this;
        var content = (FrameworkElement)window.Content;
        var bounds = new Rect(0, 0, content.ActualWidth, content.ActualHeight);
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(window.Background, null, bounds);
            drawing.DrawRectangle(new VisualBrush(content), null, bounds);
        }
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(bounds.Width), (int)Math.Ceiling(bounds.Height), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path); encoder.Save(file);
    }
}
