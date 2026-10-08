using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
public sealed class MainWindow : Window
{
    private readonly ScreenService screen = new();
    private readonly DialogDetector detector = new();
    private readonly JoinedScreenDetector joinedDetector = new();
    private readonly AutomationController automation;
    private HotkeyService? hotkeys;
    private AppSettings settings = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private bool previewBusy, closing, referenceMode, dirty;
    private volatile bool liveUpdatesEnabled = true;
    private readonly object uiUpdateGate = new();
    private AutomationSnapshot? pendingUiSnapshot;
    private bool uiUpdateQueued;
    private long lastUiUpdate;
    private RunState? lastUiState;
    private string? lastUiReason;
    private readonly Dictionary<string, TextBox> fields = new();
    private readonly Dictionary<Team, ComboBox> keyBoxes = new();
    private readonly Dictionary<Team, Button> teamButtons = new();
    private readonly ComboBox monitor = new(), selectedTeam = new();
    private readonly CheckBox dryRun = new() { Content = "Testmodus – keine Mauseingaben", IsChecked = true }, calibrated = new() { Content = "Geometrie für dieses Profil geprüft" };
    private readonly CheckBox liveUpdates = new() { Content = "Live-Bild und Detaildiagnose aktualisieren", IsChecked = true };
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
    private string? startupSettingsError;
    private static readonly Brush Muted = new SolidColorBrush(Color.FromRgb(159, 177, 185));

    public MainWindow(bool registerGlobalHotkeys = true)
    {
        Title = "WardogsTeamselector"; Width = 1180; Height = 820; MinWidth = 920; MinHeight = 660;
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/app.ico"));
        Background = new SolidColorBrush(Color.FromRgb(19, 26, 30)); Foreground = Brushes.WhiteSmoke; FontFamily = new System.Windows.Media.FontFamily("Segoe UI"); FontSize = 14;
        automation = new(screen, detector, new WindowsClickSink(), joinedDetector);
        automation.Updated += OnAutomation;
        try { settings = SettingsStore.Load(); } catch (Exception ex) { startupSettingsError = "Gespeichertes Profil ungültig: " + ex.Message + " Bitte Einstellungen prüfen und speichern."; }
        Build(); LoadFields();
        SourceInitialized += (_, _) => { if (!registerGlobalHotkeys) return; hotkeys = new(this); hotkeys.TeamPressed += StartTeam; hotkeys.EscapePressed += () => automation.Stop("ESC – abgebrochen"); RegisterKeys(); };
        Loaded += async (_, _) => { if (startupSettingsError != null) ShowError(startupSettingsError); await RefreshPreview(); if (liveUpdatesEnabled && !referenceMode) timer.Start(); };
        timer.Tick += async (_, _) => await RefreshPreview();
        Closing += (_, _) => { closing = true; timer.Stop(); automation.Dispose(); hotkeys?.Dispose(); };
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) automation.Stop("ESC – abgebrochen"); };
    }

    private void Build()
    {
        var root = new DockPanel { Margin = new Thickness(24) }; Content = root;
        var head = new StackPanel { Margin = new Thickness(0, 0, 0, 20) }; DockPanel.SetDock(head, Dock.Top); root.Children.Add(head);
        head.Children.Add(new TextBlock { Text = "WardogsTeamselector", FontSize = 26, FontWeight = FontWeights.SemiBold });
        head.Children.Add(new TextBlock { Text = "Team aktivieren. Auf die Auswahl warten. ESC beendet den Lauf.", Foreground = Muted, Margin = new Thickness(0, 5, 0, 0) });
        status.Text = "Gestoppt"; status.FontSize = 18; status.FontWeight = FontWeights.SemiBold; status.Margin = new Thickness(0, 12, 0, 0); head.Children.Add(status);
        errorText.Foreground = new SolidColorBrush(Color.FromRgb(255, 161, 137)); errorText.TextWrapping = TextWrapping.Wrap; errorText.Margin = new Thickness(0, 7, 0, 0); head.Children.Add(errorText);
        var footer = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 15, 0, 0) }; DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
        footer.Children.Add(Button("Einstellungen speichern", SaveSettings)); footer.Children.Add(Button("Profil zurücksetzen", ResetSettings)); footer.Children.Add(Button("Diagnose exportieren", ExportLog));
        var tabs = new TabControl { Background = Background, BorderBrush = new SolidColorBrush(Color.FromRgb(65, 79, 85)) }; root.Children.Add(tabs);
        var control = new Grid { Margin = new Thickness(16) }; control.ColumnDefinitions.Add(new() { Width = new GridLength(300) }); control.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
        var left = new StackPanel { Margin = new Thickness(0, 0, 24, 0) }; control.Children.Add(new ScrollViewer { Content = left, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        left.Children.Add(Heading("Team wählen"));
        foreach (var team in Enum.GetValues<Team>()) { var t = team; var b = Button(TeamName(t) + " aktivieren · F" + (settings.Hotkeys[t] - 0x6F), () => StartTeam(t)); teamButtons[t] = b; b.HorizontalAlignment = HorizontalAlignment.Stretch; b.Margin = new Thickness(0, 0, 0, 8); b.Background = TeamBrush(t); b.Foreground = Brushes.White; left.Children.Add(b); }
        left.Children.Add(Button("Stopp · ESC", () => automation.Stop("Manuell gestoppt")));
        left.Children.Add(Heading("Klickintervall")); AddField(left, "min", "Minimum A (ms)"); AddField(left, "max", "Maximum B (ms)");
        left.Children.Add(new TextBlock { Text = "Zufällig zwischen A und B. Minimum: 50 ms.", Foreground = Muted, TextWrapping = TextWrapping.Wrap });
        left.Children.Add(new TextBlock { Text = "Weiterklicken bis die fünf weißen HUD-Balken erkannt werden. ESC stoppt jederzeit.", Foreground = Muted, TextWrapping = TextWrapping.Wrap });
        dryRun.Margin = new Thickness(0, 15, 0, 10); dryRun.Foreground = Foreground; dryRun.Checked += Changed; dryRun.Unchecked += Changed; left.Children.Add(dryRun);
        counters.TextWrapping = TextWrapping.Wrap; counters.Foreground = Muted; left.Children.Add(counters);
        var right = new DockPanel(); Grid.SetColumn(right, 1); control.Children.Add(right);
        var info = new StackPanel { Margin = new Thickness(0, 0, 0, 12) }; DockPanel.SetDock(info, Dock.Top); right.Children.Add(info); info.Children.Add(Heading("Bildschirm & Erkennung"));
        liveUpdates.Foreground = Foreground; liveUpdates.Margin = new Thickness(0, 0, 0, 8); liveUpdates.ToolTip = "Aus: keine zusätzliche Vorschauaufnahme oder Detailaktualisierung. Automatische Erkennung und Klicks bleiben aktiv. Zum Behalten Einstellungen speichern."; liveUpdates.Checked += (_, _) => SetLiveUpdates(true); liveUpdates.Unchecked += (_, _) => SetLiveUpdates(false); info.Children.Add(liveUpdates);
        geometryText.Foreground = Muted; geometryText.TextWrapping = TextWrapping.Wrap; info.Children.Add(geometryText); detectionText.Margin = new Thickness(0, 8, 0, 0); info.Children.Add(detectionText); info.Children.Add(joinedText);
        var actions = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) }; info.Children.Add(actions);
        actions.Children.Add(Button("Live-Bild", () => { referenceMode = false; liveUpdates.IsChecked = true; _ = RefreshPreview(); }));
        actions.Children.Add(Button("Referenz prüfen", LoadReference)); actions.Children.Add(Button("Folgescreen prüfen", LoadJoinedReference)); actions.Children.Add(Button("Bild speichern", SaveImage));
        previewHost.Background = Brushes.Black; previewHost.Children.Add(preview); previewHost.Children.Add(overlay); right.Children.Add(previewHost);
        previewHost.SizeChanged += (_, _) => DrawOverlay(); overlay.MouseLeftButtonDown += PreviewDown; overlay.MouseLeftButtonUp += PreviewUp;
        tabs.Items.Add(new TabItem { Header = "Steuerung & Live-Bild", Content = control });
        tabs.Items.Add(new TabItem { Header = "Monitor & Kalibrierung", Content = BuildCalibration() });
        probes.AutoGenerateColumns = false; foreach (var column in new[] { ("Messfläche", "Name"), ("Score", "Score"), ("Soll", "Expected"), ("Ist", "Actual") }) probes.Columns.Add(new DataGridTextColumn { Header = column.Item1, Binding = new System.Windows.Data.Binding(column.Item2), Width = column.Item2 == "Name" ? new DataGridLength(190) : column.Item2 == "Score" ? new DataGridLength(60) : new DataGridLength(1, DataGridLengthUnitType.Star) });
        probes.Background = Background; probes.Foreground = Brushes.WhiteSmoke; probes.RowBackground = new SolidColorBrush(Color.FromRgb(28, 38, 43)); probes.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(35, 45, 50)); probes.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal; probes.HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(65, 79, 85));
        var headerStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader)); headerStyle.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(48, 62, 69)))); headerStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.WhiteSmoke)); headerStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(6))); probes.ColumnHeaderStyle = headerStyle;
        log.Background = new SolidColorBrush(Color.FromRgb(28, 38, 43)); log.Foreground = Brushes.WhiteSmoke;
        var diagnostics = new Grid { Margin = new Thickness(16) }; diagnostics.RowDefinitions.Add(new() { Height = GridLength.Auto }); diagnostics.RowDefinitions.Add(new() { Height = new GridLength(2, GridUnitType.Star) }); diagnostics.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) }); var text = new TextBlock { Text = "Messflächen: Soll-/Ist-Farben und Einzelwerte. Protokoll: Zustandswechsel, Fehler und Stoppgrund.", TextWrapping = TextWrapping.Wrap, Foreground = Muted, Margin = new Thickness(0, 0, 0, 12) }; diagnostics.Children.Add(text);
        Grid.SetRow(probes, 1); probes.Margin = new Thickness(0, 0, 0, 16); diagnostics.Children.Add(probes); Grid.SetRow(log, 2); diagnostics.Children.Add(log); tabs.Items.Add(new TabItem { Header = "Diagnose", Content = diagnostics });
    }

    private UIElement BuildCalibration()
    {
        var grid = new Grid { Margin = new Thickness(16) }; grid.ColumnDefinitions.Add(new()); grid.ColumnDefinitions.Add(new());
        var a = new StackPanel { Margin = new Thickness(0, 0, 24, 0) }; var b = new StackPanel(); Grid.SetColumn(b, 1); grid.Children.Add(a); grid.Children.Add(b);
        a.Children.Add(Heading("Spielbereich")); a.Children.Add(Label("Monitor")); monitor.MinHeight = 30; monitor.SelectionChanged += (_, _) => MarkDirty(); a.Children.Add(monitor); a.Children.Add(Button("Monitore neu erkennen", RefreshMonitors));
        AddField(a, "title", "Spielfenster enthält (Titel)"); AddField(a, "process", "Spielprozess enthält (Name ohne .exe)"); a.Children.Add(new TextBlock { Text = "Titel und Prozess müssen passen. Das Spiel muss für echte Klicks im Vordergrund sein.", Foreground = Muted, TextWrapping = TextWrapping.Wrap });
        AddField(a, "bounds", "Manueller Bereich: X,Y,Breite,Höhe (leer = automatisch)");
        calibrated.Foreground = Foreground; calibrated.Margin = new Thickness(0, 8, 0, 0); calibrated.Checked += Changed; calibrated.Unchecked += Changed; a.Children.Add(calibrated);
        a.Children.Add(Heading("Sprachunabhängige Erkennung")); AddField(a, "threshold", "Erkennungsschwelle (50–100 %, Standard 90)"); AddField(a, "offsetx", "Dialogverschiebung X (% der Breite)"); AddField(a, "offsety", "Dialogverschiebung Y (% der Höhe)"); AddField(a, "scale", "Dialogskalierung (1 = Referenz)");
        a.Children.Add(new TextBlock { Text = "Rahmen und Flächen werden geprüft, keine Überschrift und keine Teamverfügbarkeit. Nach Änderungen erst im Testmodus prüfen.", Foreground = Muted, TextWrapping = TextWrapping.Wrap });
        a.Children.Add(new TextBlock { Text = "Nach dem ersten Klick wird bis zur stabilen Erkennung der fünf weißen HUD-Balken weitergeklickt (mindestens 0,5 s). ESC stoppt jederzeit.", Foreground = Muted, TextWrapping = TextWrapping.Wrap });
        b.Children.Add(Heading("Teamflächen überschreiben")); selectedTeam.ItemsSource = Enum.GetValues<Team>().Select(t => new TeamChoice(t, TeamName(t))).ToList(); selectedTeam.DisplayMemberPath = "Name"; selectedTeam.SelectedIndex = 0; selectedTeam.SelectionChanged += (_, _) => LoadRegion(); b.Children.Add(selectedTeam);
        AddField(b, "rx", "Links (% des Spielbereichs)"); AddField(b, "ry", "Oben (%)"); AddField(b, "rw", "Breite (%)"); AddField(b, "rh", "Höhe (%)"); b.Children.Add(Button("Teamfläche übernehmen", ApplyRegion));
        b.Children.Add(new TextBlock { Text = "Alternativ: Team hier wählen, im Live-Bild ein Rechteck aufziehen. Klickpunkt ist dessen Mitte. Blaue, rote und grüne Rahmen zeigen die gespeicherten Flächen.", TextWrapping = TextWrapping.Wrap, Foreground = Muted, Margin = new Thickness(0, 5, 0, 0) });
        b.Children.Add(Heading("Globale Teamhotkeys")); foreach (var t in Enum.GetValues<Team>()) { b.Children.Add(Label(TeamName(t))); var box = new ComboBox { ItemsSource = Enumerable.Range(1, 24).Select(n => "F" + n).ToList(), MinHeight = 30, Margin = new Thickness(0, 0, 0, 8) }; box.SelectionChanged += (_, _) => MarkDirty(); keyBoxes[t] = box; b.Children.Add(box); }
        b.Children.Add(new TextBlock { Text = "Einmal drücken aktiviert. ESC bricht global ab und wird weiterhin an das Spiel weitergegeben.", Foreground = Muted, TextWrapping = TextWrapping.Wrap });
        return new ScrollViewer { Content = grid, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    private void LoadFields()
    {
        fields["min"].Text = settings.MinIntervalMs.ToString(); fields["max"].Text = settings.MaxIntervalMs.ToString(); fields["title"].Text = settings.WindowTitleContains;
        fields["process"].Text = settings.ProcessNameContains;
        liveUpdates.IsChecked = settings.LivePreviewEnabled;
        fields["threshold"].Text = Format(settings.DetectionThreshold * 100); fields["offsetx"].Text = Format(settings.DetectionOffsetX * 100); fields["offsety"].Text = Format(settings.DetectionOffsetY * 100); fields["scale"].Text = Format(settings.DetectionScale);
        fields["bounds"].Text = settings.ManualBounds is Rectangle r ? $"{r.X},{r.Y},{r.Width},{r.Height}" : ""; dryRun.IsChecked = settings.DryRun; calibrated.IsChecked = settings.GeometryCalibrated;
        foreach (var t in Enum.GetValues<Team>()) keyBoxes[t].SelectedIndex = settings.Hotkeys[t] - 0x70;
        RefreshMonitors(); LoadRegion(); dirty = false;
    }
    private AppSettings ReadFields()
    {
        var s = new AppSettings { MinIntervalMs = int.Parse(fields["min"].Text), MaxIntervalMs = int.Parse(fields["max"].Text), WindowTitleContains = fields["title"].Text.Trim(), ProcessNameContains = fields["process"].Text.Trim(), DetectionThreshold = Number("threshold") / 100, DetectionOffsetX = Number("offsetx") / 100, DetectionOffsetY = Number("offsety") / 100, DetectionScale = Number("scale"), DryRun = dryRun.IsChecked == true, GeometryCalibrated = calibrated.IsChecked == true, MonitorId = (monitor.SelectedItem as MonitorChoice)?.Id, Regions = settings.Regions.ToList(), Hotkeys = keyBoxes.ToDictionary(k => k.Key, k => 0x70 + k.Value.SelectedIndex) };
        if (!string.IsNullOrWhiteSpace(fields["bounds"].Text)) { var v = fields["bounds"].Text.Split(',').Select(int.Parse).ToArray(); if (v.Length != 4) throw new ArgumentException("Spielbereich benötigt X,Y,Breite,Höhe."); s.ManualBounds = new(v[0], v[1], v[2], v[3]); }
        s.LivePreviewEnabled = liveUpdatesEnabled;
        s.Validate(); return s;
    }
    private void RefreshMonitors()
    {
        string? id = (monitor.SelectedItem as MonitorChoice)?.Id ?? settings.MonitorId;
        var items = new List<MonitorChoice> { new(null, "Automatisch · Spielfenster") }; items.AddRange(screen.GetMonitors().Select(m => new MonitorChoice(m.Id, $"{m.Name} · {m.Bounds.Width}×{m.Bounds.Height} · ({m.Bounds.X},{m.Bounds.Y})")));
        if (id != null && !items.Any(m => m.Id == id)) items.Add(new(id, "Nicht verbunden · " + id)); monitor.ItemsSource = items; monitor.DisplayMemberPath = "Name"; monitor.SelectedItem = items.FirstOrDefault(m => m.Id == id) ?? items[0];
    }
    private void SaveSettings()
    {
        try { automation.Stop("Einstellungen geändert"); var next = ReadFields(); ApplyHotkeys(next); SettingsStore.Save(next); settings = next; startupSettingsError = null; dirty = false; errorText.Text = ""; AddLog("Einstellungen gespeichert · " + SettingsStore.FilePath); referenceMode = false; _ = RefreshPreview(); } catch (Exception ex) { ShowError(ex.Message); }
    }
    private void ApplyHotkeys(AppSettings next)
    {
        try { hotkeys?.Apply(next); foreach (var team in Enum.GetValues<Team>()) { teamButtons[team].Content = TeamName(team) + " aktivieren · F" + (next.Hotkeys[team] - 0x6F); teamButtons[team].ToolTip = "Einmal drücken aktiviert das Team. ESC bricht ab."; } }
        catch { foreach (var team in Enum.GetValues<Team>()) { teamButtons[team].Content = TeamName(team) + " aktivieren · Hotkey inaktiv"; teamButtons[team].ToolTip = "Hotkey-Konflikt: Andere Tasten wählen und Einstellungen speichern."; } throw; }
    }
    private void RegisterKeys() { try { ApplyHotkeys(settings); } catch (Exception ex) { ShowError(ex.Message); } }
    private void ResetSettings() { automation.Stop("Profil zurückgesetzt"); settings = new(); LoadFields(); RegisterKeys(); AddLog("Standardprofil geladen. Zum Behalten speichern."); }
    private void StartTeam(Team team)
    {
        try { if (startupSettingsError != null) throw new InvalidOperationException(startupSettingsError); var next = ReadFields(); if (dirty) { ApplyHotkeys(next); settings = next; dirty = false; } referenceMode = false; errorText.Text = ""; automation.Start(team, next); } catch (Exception ex) { ShowError(ex.Message); }
    }
    private void OnAutomation(AutomationSnapshot snapshot)
    {
        if (closing) return;
        lock (uiUpdateGate)
        {
            pendingUiSnapshot = snapshot;
            var now = Stopwatch.GetTimestamp();
            bool changed = lastUiState != snapshot.State || lastUiReason != snapshot.Reason;
            if (uiUpdateQueued || (!changed && lastUiUpdate != 0 && Stopwatch.GetElapsedTime(lastUiUpdate, now).TotalMilliseconds < (liveUpdatesEnabled ? 250 : 1000))) return;
            uiUpdateQueued = true; lastUiUpdate = now; lastUiState = snapshot.State; lastUiReason = snapshot.Reason;
        }
        Dispatcher.BeginInvoke(() =>
        {
            AutomationSnapshot current;
            lock (uiUpdateGate) { current = pendingUiSnapshot!; uiUpdateQueued = false; }
            if (closing) return;
            status.Text = current.State switch { RunState.Waiting => "Wartet auf die Teamauswahl · " + TeamName(current.Team ?? Team.Blue), RunState.Clicking => "Klickt · " + TeamName(current.Team ?? Team.Blue), _ => "Gestoppt" };
            counters.Text = $"Klicks: {current.ClickCount}\nLetztes Intervall: {current.IntervalMs} ms\n{current.Reason}";
            if (liveUpdatesEnabled && current.Detection != null) UpdateDetection(current.Detection, current.JoinedDetection);
            if (logLines.LastOrDefault()?.EndsWith(current.Reason) != true) AddLog(current.Reason);
        });
    }
    private void SetLiveUpdates(bool enabled)
    {
        liveUpdatesEnabled = enabled; settings.LivePreviewEnabled = enabled;
        if (enabled) { referenceMode = false; if (IsLoaded) { timer.Start(); _ = RefreshPreview(); } }
        else { timer.Stop(); preview.Source = null; lastImage = null; lastGeometry = null; overlay.Children.Clear(); probes.ItemsSource = null; geometryText.Text = "Live-Anzeige aus · Erkennung und Klicksteuerung bleiben aktiv."; detectionText.Text = "Detaildiagnose pausiert"; joinedText.Text = ""; }
    }
    private async Task RefreshPreview()
    {
        if (!liveUpdatesEnabled || previewBusy || closing || referenceMode) return; previewBusy = true;
        try
        {
            // Never use partially edited UI settings to control an active run.
            var config = settings;
            var result = await Task.Run(() => { var target = screen.ResolveTarget(config); if (target == null) return (Image: (BitmapSource?)null, Target: (TargetGeometry?)null, Detection: (DetectionResult?)null, Joined: (DetectionResult?)null); using var f = screen.Capture(target); var d = detector.Detect(f, config); var joined = joinedDetector.Detect(f, config); return (Image: ToSource(f.Bitmap), Target: target, Detection: d, Joined: joined); });
            if (closing || referenceMode || !liveUpdatesEnabled) return;
            if (result.Target == null) { lastGeometry = null; lastImage = null; preview.Source = null; overlay.Children.Clear(); geometryText.Text = "Kein passendes Spielfenster. Wardogs starten oder den Fensterfilter unter Monitor & Kalibrierung anpassen."; detectionText.Text = "Kein Bild verfügbar"; joinedText.Text = ""; return; }
            lastGeometry = result.Target; lastImage = result.Image; preview.Source = lastImage;
            var r = result.Target.Bounds; geometryText.Text = $"{result.Target.Description}\n{r.Width}×{r.Height} px · Ursprung ({r.X},{r.Y}) · Fokus: {(result.Target.IsForeground ? "Spiel" : "anderes Fenster")} · Geometrie: {(result.Target.IsCalibrated ? "freigegeben" : "Kalibrierung nötig")}";
            UpdateDetection(result.Detection!, result.Joined); DrawOverlay();
        }
        catch (Exception ex) { geometryText.Text = "Aufnahme nicht verfügbar: " + ex.Message; preview.Source = null; lastImage = null; overlay.Children.Clear(); }
        finally { previewBusy = false; }
    }
    private void LoadReference() => LoadEmbeddedReference("WardogsTeamselector.Reference", "Referenzbild");
    private void LoadJoinedReference() => LoadEmbeddedReference("WardogsTeamselector.Joined", "Folgescreen-Referenz");
    private void LoadEmbeddedReference(string resource, string title)
    {
        try { automation.Stop("Referenzprüfung"); referenceMode = true; using var stream = typeof(MainWindow).Assembly.GetManifestResourceStream(resource) ?? throw new FileNotFoundException("Referenzbild fehlt."); using var bmp = new Bitmap(stream); var g = new TargetGeometry(new(0, 0, bmp.Width, bmp.Height), IntPtr.Zero, false, title, true); using var frame = new CaptureFrame((Bitmap)bmp.Clone(), g); lastImage = ToSource(bmp); preview.Source = lastImage; lastGeometry = g; geometryText.Text = $"{title} · {bmp.Width}×{bmp.Height} · keine Eingaben"; var config = ReadFields(); UpdateDetection(detector.Detect(frame, config), joinedDetector.Detect(frame, config)); DrawOverlay(); } catch (Exception ex) { ShowError(ex.Message); }
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
    }
    private void PreviewDown(object sender, MouseButtonEventArgs e) { if (automation.Snapshot.State != RunState.Stopped || lastImage == null) return; dragStart = e.GetPosition(overlay); overlay.CaptureMouse(); }
    private void PreviewUp(object sender, MouseButtonEventArgs e)
    {
        overlay.ReleaseMouseCapture(); if (dragStart is not Point start) return; dragStart = null; var end = e.GetPosition(overlay); var a = ImageArea(); if (a.W <= 0) return;
        var x1 = Math.Clamp((Math.Min(start.X, end.X) - a.X) / a.W, 0, 1); var y1 = Math.Clamp((Math.Min(start.Y, end.Y) - a.Y) / a.H, 0, 1); var x2 = Math.Clamp((Math.Max(start.X, end.X) - a.X) / a.W, 0, 1); var y2 = Math.Clamp((Math.Max(start.Y, end.Y) - a.Y) / a.H, 0, 1);
        if (x2 - x1 < 0.005 || y2 - y1 < 0.005) return; var t = (selectedTeam.SelectedItem as TeamChoice)?.Team ?? Team.Blue; settings.Regions = settings.Regions.Select(r => r.Team == t ? new TeamRegion(t, x1, y1, x2 - x1, y2 - y1) : r).ToList(); LoadRegion(); MarkDirty(); DrawOverlay(); AddLog("Teamfläche geändert: " + TeamName(t));
    }
    private void LoadRegion() { if (!fields.ContainsKey("rx")) return; var t = (selectedTeam.SelectedItem as TeamChoice)?.Team ?? Team.Blue; var r = settings.Regions.First(r => r.Team == t); fields["rx"].Text = Format(r.X * 100); fields["ry"].Text = Format(r.Y * 100); fields["rw"].Text = Format(r.Width * 100); fields["rh"].Text = Format(r.Height * 100); }
    private void ApplyRegion() { try { automation.Stop("Teamfläche geändert"); var t = (selectedTeam.SelectedItem as TeamChoice)?.Team ?? Team.Blue; var region = new TeamRegion(t, Number("rx") / 100, Number("ry") / 100, Number("rw") / 100, Number("rh") / 100); var next = ReadFields(); next.Regions = next.Regions.Select(r => r.Team == t ? region : r).ToList(); next.Validate(); settings.Regions = next.Regions; MarkDirty(); DrawOverlay(); AddLog("Teamfläche übernommen: " + TeamName(t)); } catch (Exception ex) { ShowError(ex.Message); } }
    private void SaveImage() { if (lastImage == null) { ShowError("Zuerst ein Bild aufnehmen."); return; } var dialog = new SaveFileDialog { Filter = "PNG-Bild|*.png", FileName = "wardogs-aufnahme.png" }; if (dialog.ShowDialog() != true) return; try { var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(lastImage)); using var file = File.Create(dialog.FileName); encoder.Save(file); } catch (Exception ex) { ShowError(ex.Message); } }
    private void ExportLog() { var dialog = new SaveFileDialog { Filter = "Textdatei|*.txt", FileName = "wardogs-diagnose.txt" }; if (dialog.ShowDialog() != true) return; try { File.WriteAllText(dialog.FileName, "WardogsTeamselector\n" + geometryText.Text + "\n" + detectionText.Text + "\n" + string.Join("\n", logLines) + "\n\nMessflächen:\n" + string.Join("\n", (probes.ItemsSource as IReadOnlyList<ProbeResult> ?? Array.Empty<ProbeResult>()).Select(p => $"{p.Name}: {p.Score:F3} | Soll {p.Expected} | Ist {p.Actual}"))); } catch (Exception ex) { ShowError(ex.Message); } }
    private void ShowError(string message) { errorText.Text = message; AddLog("Fehler: " + message); }
    private void AddLog(string message) { logLines.Add(DateTime.Now.ToString("HH:mm:ss.fff") + "  " + message); if (logLines.Count > 500) logLines.RemoveAt(0); log.Text = string.Join(Environment.NewLine, logLines); log.ScrollToEnd(); }
    private void MarkDirty() { dirty = true; if (automation.Snapshot.State != RunState.Stopped) automation.Stop("Einstellungen bearbeitet"); }
    private void Changed(object sender, RoutedEventArgs e) => MarkDirty();
    private double Number(string key) { var text = fields[key].Text.Trim().Replace(',', '.'); return double.Parse(text, CultureInfo.InvariantCulture); }
    private static string Format(double n) => n.ToString("0.####", CultureInfo.CurrentCulture);
    private void AddField(Panel panel, string key, string name) { panel.Children.Add(Label(name)); var box = new TextBox { MinHeight = 29, Padding = new Thickness(5), Margin = new Thickness(0, 0, 0, 7) }; box.TextChanged += (_, _) => MarkDirty(); fields[key] = box; panel.Children.Add(box); }
    private static TextBlock Label(string text) => new() { Text = text, Foreground = Brushes.WhiteSmoke, Margin = new Thickness(0, 5, 0, 4), TextWrapping = TextWrapping.Wrap };
    private static TextBlock Heading(string text) => new() { Text = text, Foreground = Brushes.WhiteSmoke, FontSize = 17, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 9) };
    private static Button Button(string text, Action action) { var b = new Button { Content = text, Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 8, 0), MinHeight = 34 }; b.Click += (_, _) => action(); return b; }
    private static string TeamName(Team t) => t switch { Team.Blue => "Blau", Team.Red => "Rot", _ => "Grün" };
    private static Brush TeamBrush(Team t) => new SolidColorBrush(t switch { Team.Blue => Color.FromRgb(22, 112, 163), Team.Red => Color.FromRgb(164, 56, 53), _ => Color.FromRgb(34, 124, 77) });
    private sealed record MonitorChoice(string? Id, string Name);
    private sealed record TeamChoice(Team Team, string Name);
    internal async Task CaptureSmokeImages(string directory)
    {
        if (Icon == null) throw new InvalidOperationException("App-Icon fehlt.");
        // Exercise the preview toggle even when the saved profile has it disabled.
        liveUpdates.IsChecked = true;
        timer.Stop(); LoadReference(); await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        Directory.CreateDirectory(directory);
        var tabs = FindTabs((DependencyObject)Content)!;
        for (int i = 0; i < tabs.Items.Count; i++) { tabs.SelectedIndex = i; UpdateLayout(); await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle); SaveRender(Path.Combine(directory, $"gui-{i}.png")); }
        Width = MinWidth; Height = MinHeight; tabs.SelectedIndex = 0; UpdateLayout(); await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle); SaveRender(Path.Combine(directory, "gui-small.png"));
        if (!detectionText.Text.Contains("Dialog: erkannt")) throw new InvalidOperationException("Referenzprüfung in GUI fehlgeschlagen: " + detectionText.Text);
        LoadJoinedReference(); UpdateLayout(); await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle); SaveRender(Path.Combine(directory, "gui-joined.png"));
        if (!joinedText.Text.Contains("HUD erkannt") || !detectionText.Text.Contains("Dialog: nicht erkannt")) throw new InvalidOperationException("Folgescreenprüfung in GUI fehlgeschlagen: " + joinedText.Text);
        automation.Start(Team.Blue, new AppSettings { DryRun = true, WindowTitleContains = "UI-Prüfung-" + Guid.NewGuid().ToString("N") });
        liveUpdates.IsChecked = false;
        await RefreshPreview();
        if (timer.IsEnabled || preview.Source != null || probes.ItemsSource != null || automation.Snapshot.State != RunState.Waiting) throw new InvalidOperationException("Deaktivierte Live-Anzeige beeinflusst die Steuerung oder aktualisiert weiter.");
        automation.Stop("UI-Prüfung beendet");
        UpdateLayout(); SaveRender(Path.Combine(directory, "gui-live-off.png"));
        if (!string.IsNullOrEmpty(errorText.Text)) throw new InvalidOperationException(errorText.Text);
    }
    private static TabControl? FindTabs(DependencyObject root) { if (root is TabControl t) return t; for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) { var found = FindTabs(VisualTreeHelper.GetChild(root, i)); if (found != null) return found; } return null; }
    private void SaveRender(string path) { var bitmap = new RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(this); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var file = File.Create(path); encoder.Save(file); }
}
