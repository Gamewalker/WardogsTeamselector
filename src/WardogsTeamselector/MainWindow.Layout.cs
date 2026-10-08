using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WardogsTeamselector.Core;

namespace WardogsTeamselector;

// THESIS: Four task areas guide setup, operation, configuration and diagnosis.
// OWN-WORLD: Dark native Windows utility, Segoe UI, explicit labels and team colors.
// STORY: Connect the game, check click areas, run a team, investigate only as needed.
// FIRST VIEWPORT: Task tabs below a persistent status and stop control; setup pairs
// editable geometry with its preview, while operation dedicates the space to teams.
// FORM: User-specified four-area workflow, code-led within the established identity.
// FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
public sealed partial class MainWindow
{
    private readonly TabControl pages = new();
    private readonly Grid setupPreviewSlot = new(), diagnosticPreviewSlot = new();
    private readonly DockPanel previewPane = new();
    private readonly TextBlock previewEmpty = new(), previewSource = new();
    private readonly TextBlock modeText = new(), profileText = new(), operationSummary = new(), runReason = new();
    private readonly TextBlock operationState = new(), setupFeedback = new();
    private readonly CheckBox drawRegion = new() { Content = "Teamfläche im Bild zeichnen" };
    private Button saveButton = null!, discardButton = null!, stopButton = null!;
    private Border modeNotice = null!;

    private void Build()
    {
        var root = new DockPanel { Margin = new Thickness(20) };
        Content = root;

        var header = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        header.ColumnDefinitions.Add(new());
        header.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);
        var identity = new StackPanel { Orientation = Orientation.Horizontal };
        identity.Children.Add(new Image { Source = Icon, Width = 38, Height = 38, Margin = new Thickness(0, 0, 12, 0) });
        var title = new StackPanel();
        title.Children.Add(new TextBlock { Text = "WardogsTeamselector", FontSize = 24, FontWeight = FontWeights.SemiBold });
        status.Text = "Bereit";
        status.Foreground = Muted;
        title.Children.Add(status);
        identity.Children.Add(title);
        header.Children.Add(identity);
        stopButton = Button("Stopp · ESC", () => automation.Stop("Manuell gestoppt"));
        stopButton.Width = 154;
        stopButton.Margin = new Thickness(16, 0, 0, 0);
        stopButton.Background = TeamBrush(Team.Red);
        stopButton.Foreground = Brushes.White;
        stopButton.VerticalAlignment = VerticalAlignment.Center;
        stopButton.ToolTip = "Beendet den aktuellen Lauf sofort. ESC funktioniert auch im Spiel.";
        Grid.SetColumn(stopButton, 1);
        header.Children.Add(stopButton);

        errorText.Foreground = ErrorBrush;
        errorText.TextWrapping = TextWrapping.Wrap;
        errorText.Margin = new Thickness(0, 0, 0, 10);
        errorText.Visibility = Visibility.Collapsed;
        DockPanel.SetDock(errorText, Dock.Top);
        root.Children.Add(errorText);

        var footer = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        footer.ColumnDefinitions.Add(new());
        footer.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(footer);
        profileText.VerticalAlignment = VerticalAlignment.Center;
        profileText.TextWrapping = TextWrapping.Wrap;
        profileText.Margin = new Thickness(0, 0, 16, 0);
        footer.Children.Add(profileText);
        var profileActions = new StackPanel { Orientation = Orientation.Horizontal };
        discardButton = Button("Änderungen verwerfen", DiscardChanges);
        saveButton = Button("Einstellungen speichern", () => SaveSettings());
        saveButton.Margin = new Thickness(0);
        profileActions.Children.Add(discardButton);
        profileActions.Children.Add(saveButton);
        Grid.SetColumn(profileActions, 1);
        footer.Children.Add(profileActions);

        BuildPreviewPane();
        pages.Background = Background;
        pages.Padding = new Thickness(16);
        pages.BorderBrush = BorderBrushColor;
        pages.Items.Add(Page("1. Einrichtung", BuildSetup()));
        pages.Items.Add(Page("2. Betrieb", BuildOperation()));
        pages.Items.Add(Page("3. Konfiguration", BuildConfiguration()));
        pages.Items.Add(Page("4. Diagnose", BuildDiagnostics()));
        pages.SelectionChanged += (_, e) =>
        {
            if (e.Source != pages) return;
            CancelRegionDrag();
            UpdatePreviewLocation();
        };
        root.Children.Add(pages);
    }

    private UIElement BuildSetup()
    {
        var grid = Split(320);
        var form = new StackPanel { Margin = new Thickness(0, 0, 20, 0) };
        grid.Children.Add(Scroll(form));
        Grid.SetColumn(setupPreviewSlot, 1);
        grid.Children.Add(setupPreviewSlot);
        form.Children.Add(PageTitle("Spiel verbinden", "Wardogs öffnen und den Teamauswahlbildschirm anzeigen."));
        AddField(form, "title", "Fenstertitel enthält", 0);
        AddField(form, "process", "Prozessname enthält (ohne .exe)", 0);
        form.Children.Add(Label("Monitor", monitor));
        monitor.MinHeight = 32;
        monitor.Foreground = Brushes.Black;
        monitor.SelectionChanged += (_, _) => MarkDirty();
        form.Children.Add(monitor);
        var connectionActions = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
        connectionActions.Children.Add(Button("Spiel suchen / Bild laden", ApplyPreviewSettings));
        connectionActions.Children.Add(Button("Monitore aktualisieren", RefreshMonitors));
        form.Children.Add(connectionActions);
        setupFeedback.Text = "Automatisch wird der Clientbereich des passenden Spielfensters verwendet.";
        setupFeedback.Foreground = Muted;
        setupFeedback.TextWrapping = TextWrapping.Wrap;
        setupFeedback.Margin = new Thickness(0, 8, 0, 0);
        form.Children.Add(setupFeedback);

        form.Children.Add(Heading("Teamflächen prüfen"));
        form.Children.Add(Hint("Die farbigen Rahmen müssen auf den drei Teamkarten liegen. Ihre Mitte ist der Klickpunkt."));
        form.Children.Add(Label("Team für die Kalibrierung", selectedTeam));
        selectedTeam.ItemsSource = Enum.GetValues<Team>().Select(t => new TeamChoice(t, TeamName(t))).ToList();
        selectedTeam.DisplayMemberPath = "Name";
        selectedTeam.MinHeight = 32;
        selectedTeam.Foreground = Brushes.Black;
        selectedTeam.SelectedIndex = 0;
        selectedTeam.SelectionChanged += (_, _) => ChangeSelectedTeam();
        form.Children.Add(selectedTeam);
        drawRegion.Foreground = Foreground;
        drawRegion.Margin = new Thickness(0, 10, 0, 4);
        drawRegion.Checked += (_, _) => UpdateDrawingMode();
        drawRegion.Unchecked += (_, _) => UpdateDrawingMode();
        form.Children.Add(drawRegion);
        form.Children.Add(Hint("Zum Ändern Zeichnen einschalten und im Bild ein Rechteck ziehen. Es werden dabei keine Klicks gesendet."));
        var coordinates = new StackPanel();
        var row = new Grid();
        row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new());
        var position = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
        var size = new StackPanel(); Grid.SetColumn(size, 1);
        AddField(position, "rx", "Links (%)", 0); AddField(position, "ry", "Oben (%)", 0);
        AddField(size, "rw", "Breite (%)", 0); AddField(size, "rh", "Höhe (%)", 0);
        row.Children.Add(position); row.Children.Add(size); coordinates.Children.Add(row);
        coordinates.Children.Add(Button("Teamfläche übernehmen", ApplyRegion));
        form.Children.Add(Expand("Teamfläche als Prozentwerte", coordinates));

        var manual = new StackPanel();
        AddField(manual, "bounds", "X,Y,Breite,Höhe in Desktop-Pixeln", 0);
        manual.Children.Add(Hint("Leer lassen für automatische Erkennung. Der Bereich muss im Spielfenster liegen; negative Monitorursprünge sind möglich."));
        form.Children.Add(Expand("Spielbereich manuell begrenzen", manual));
        calibrated.Content = "Abweichende Geometrie geprüft";
        calibrated.Foreground = Foreground;
        calibrated.Margin = new Thickness(0, 16, 0, 5);
        calibrated.Checked += Changed; calibrated.Unchecked += Changed;
        form.Children.Add(calibrated);
        form.Children.Add(Hint("Nur für andere Seitenverhältnisse als 16:9 nötig. Erst bestätigen, wenn Bild und Klickflächen stimmen."));
        var finish = Button("Speichern & zum Betrieb", () => { if (SaveSettings()) ShowPage(1); });
        finish.Margin = new Thickness(0, 18, 0, 8);
        form.Children.Add(finish);
        return grid;
    }

    private UIElement BuildOperation()
    {
        var body = new StackPanel();
        body.Children.Add(PageTitle("Team aktivieren", "Einmal auswählen, dann zum Spiel wechseln. Der Lauf wartet auf den stabil erkannten Dialog und Spielfokus."));

        var mode = new StackPanel();
        dryRun.Content = "Testmodus verwenden · keine Mauseingaben";
        dryRun.Foreground = Foreground;
        dryRun.Checked += Changed; dryRun.Unchecked += Changed;
        mode.Children.Add(dryRun);
        modeText.TextWrapping = TextWrapping.Wrap;
        modeText.Margin = new Thickness(0, 6, 0, 0);
        mode.Children.Add(modeText);
        modeNotice = new Border { Child = mode, Background = SurfaceBrush, Padding = new Thickness(16), Margin = new Thickness(0, 10, 0, 20) };
        body.Children.Add(modeNotice);

        var teams = new Grid();
        foreach (var team in Enum.GetValues<Team>())
        {
            teams.ColumnDefinitions.Add(new());
            var t = team;
            var button = Button(TeamName(t) + " aktivieren", () => StartTeam(t));
            button.MinHeight = 78;
            button.FontSize = 18;
            button.FontWeight = FontWeights.SemiBold;
            button.Background = TeamBrush(t);
            button.Foreground = Brushes.White;
            button.Margin = new Thickness(0, 0, team == Team.Green ? 0 : 12, 0);
            teamButtons[t] = button;
            Grid.SetColumn(button, (int)team);
            teams.Children.Add(button);
        }
        body.Children.Add(teams);
        body.Children.Add(Heading("Aktueller Lauf"));
        operationState.FontSize = 24;
        operationState.FontWeight = FontWeights.SemiBold;
        operationState.Text = "Bereit für die Teamwahl";
        operationState.TextWrapping = TextWrapping.Wrap;
        body.Children.Add(operationState);
        runReason.Foreground = Muted;
        runReason.Text = "Noch kein Lauf gestartet. Ein Team auswählen oder dessen F-Taste drücken.";
        runReason.TextWrapping = TextWrapping.Wrap;
        runReason.Margin = new Thickness(0, 6, 0, 12);
        body.Children.Add(runReason);
        counters.FontSize = 16;
        counters.Text = "Simulierte Klicks: 0  ·  Letztes Intervall: –";
        counters.TextWrapping = TextWrapping.Wrap;
        body.Children.Add(counters);

        body.Children.Add(new Separator { Background = BorderBrushColor, Margin = new Thickness(0, 22, 0, 12) });
        operationSummary.Foreground = Muted;
        operationSummary.TextWrapping = TextWrapping.Wrap;
        body.Children.Add(operationSummary);
        var links = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        links.Children.Add(Button("Spiel / Klickflächen prüfen", () => ShowPage(0)));
        links.Children.Add(Button("Intervall / Hotkeys ändern", () => ShowPage(2)));
        body.Children.Add(links);
        body.Children.Add(Hint("Nach dem ersten Klick wird bis zu den fünf stabil erkannten weißen HUD-Balken weitergeklickt. ESC, Fokusverlust und Aufnahmefehler stoppen den Lauf.", 18));
        return Scroll(body);
    }

    private UIElement BuildConfiguration()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new()); grid.ColumnDefinitions.Add(new());
        var left = new StackPanel { Margin = new Thickness(0, 0, 28, 0) };
        var right = new StackPanel(); Grid.SetColumn(right, 1);
        grid.Children.Add(left); grid.Children.Add(right);
        left.Children.Add(PageTitle("Klickverhalten", "Diese Werte gelten für den nächsten aktivierten Lauf."));
        AddField(left, "min", "Minimales Klickintervall (ms)", 2);
        AddField(left, "max", "Maximales Klickintervall (ms)", 2);
        left.Children.Add(Hint("Zufällig zwischen Minimum und Maximum. Standard: 50–70 ms. Erlaubt: 50–60000 ms."));
        left.Children.Add(Heading("Globale Tastenkürzel"));
        foreach (var team in Enum.GetValues<Team>())
        {
            var box = new ComboBox { ItemsSource = Enumerable.Range(1, 24).Select(n => "F" + n).ToList(), MinHeight = 32, Foreground = Brushes.Black, Margin = new Thickness(0, 0, 0, 8) };
            box.Margin = new Thickness(0);
            var inputFrame = new Border { Child = box, BorderBrush = Brushes.Transparent, BorderThickness = new Thickness(2), Margin = new Thickness(0, 0, 0, 6) };
            AutomationProperties.SetName(box, "Team " + TeamName(team));
            box.SelectionChanged += (_, _) => { inputFrame.BorderBrush = Brushes.Transparent; box.ClearValue(Control.BorderBrushProperty); box.ClearValue(ToolTipProperty); MarkDirty(); };
            keyBoxes[team] = box;
            left.Children.Add(Label("Team " + TeamName(team), box)); left.Children.Add(inputFrame);
        }
        left.Children.Add(Hint("Drei verschiedene F-Tasten wählen. Ein Tastendruck aktiviert; ESC beendet immer und wird an das Spiel weitergegeben."));
        left.Children.Add(Heading("Vorschau & Leistung"));
        liveUpdates.Content = "Live-Vorschau automatisch aktualisieren";
        liveUpdates.Foreground = Foreground;
        liveUpdates.Checked += (_, _) => { SetLiveUpdates(true); MarkDirty(false); };
        liveUpdates.Unchecked += (_, _) => { SetLiveUpdates(false); MarkDirty(false); };
        left.Children.Add(liveUpdates);
        left.Children.Add(Hint("Zusätzliche Bildaufnahmen gibt es nur in Einrichtung und Diagnose. Im Betrieb läuft allein die nötige Erkennung. Referenzprüfungen bleiben manuell verfügbar.", 6));

        right.Children.Add(PageTitle("Erkennung abstimmen", "Nur anpassen, wenn die Einrichtung trotz passender Klickflächen keinen Dialog erkennt."));
        AddField(right, "threshold", "Erkennungsschwelle (%)", 2);
        right.Children.Add(Hint("Standard: 90 %. Erlaubt: 50–100 %. Ein niedrigerer Wert erkennt großzügiger, erhöht aber das Risiko von Fehlbefunden."));
        var calibration = new StackPanel();
        AddField(calibration, "offsetx", "Dialogverschiebung X (% der Bildbreite)", 2);
        AddField(calibration, "offsety", "Dialogverschiebung Y (% der Bildhöhe)", 2);
        AddField(calibration, "scale", "Dialogskalierung (1 = Referenz)", 2);
        calibration.Children.Add(Hint("Verschiebt die Erkennungsflächen. Die Klickflächen der Teams werden separat unter Einrichtung angepasst."));
        right.Children.Add(Expand("Erweiterte Dialogkalibrierung", calibration));
        var previewAction = Button("Übernehmen & in Einrichtung prüfen", () => { if (ApplyPreviewSettingsCore()) ShowPage(0); });
        previewAction.Margin = new Thickness(0, 16, 0, 0);
        right.Children.Add(previewAction);
        right.Children.Add(Heading("Profil"));
        right.Children.Add(Hint("Einstellungen werden für diesen Windows-Benutzer gespeichert. Ungespeicherte Änderungen bleiben in der Fußleiste sichtbar."));
        right.Children.Add(Button("Standardwerte laden …", ResetSettings));
        right.Children.Add(Hint("Setzt auch die Teamflächen und Tastenkürzel zurück. Die gespeicherte Datei wird erst beim Speichern ersetzt.", 6));
        return Scroll(grid);
    }

    private UIElement BuildDiagnostics()
    {
        var grid = Split(420);
        var detail = new Grid { Margin = new Thickness(0, 0, 20, 0) };
        detail.RowDefinitions.Add(new() { Height = GridLength.Auto });
        detail.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
        detail.RowDefinitions.Add(new() { Height = GridLength.Auto });
        detail.RowDefinitions.Add(new() { Height = new GridLength(0.7, GridUnitType.Star) });
        var heading = new StackPanel();
        heading.Children.Add(PageTitle("Erkennung untersuchen", "Referenzen prüfen nur die Erkennung. Sie senden keine Eingaben und stoppen einen laufenden Versuch."));
        var references = new WrapPanel { Margin = new Thickness(0, 8, 0, 8) };
        references.Children.Add(Button("Dialogreferenz prüfen", LoadReference));
        references.Children.Add(Button("HUD-Referenz prüfen", LoadJoinedReference));
        references.Children.Add(Button("Diagnose exportieren", ExportLog));
        heading.Children.Add(references);
        heading.Children.Add(Heading("Messflächen · Soll / Ist"));
        detail.Children.Add(heading);

        probes.AutoGenerateColumns = false;
        probes.MinHeight = 100;
        probes.Foreground = Foreground;
        probes.Background = SurfaceBrush;
        probes.RowBackground = SurfaceBrush;
        probes.AlternatingRowBackground = BrushFrom(35, 45, 50);
        probes.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
        probes.HorizontalGridLinesBrush = BorderBrushColor;
        foreach (var column in new[] { ("Messfläche", "Name", 140d), ("Score", "Score", 54d), ("Soll", "Expected", 95d), ("Ist", "Actual", 95d) })
        {
            var binding = new System.Windows.Data.Binding(column.Item2);
            if (column.Item2 == "Score") binding.StringFormat = "P0";
            probes.Columns.Add(new DataGridTextColumn { Header = column.Item1, Binding = binding, Width = column.Item3 });
        }
        var headerStyle = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
        headerStyle.Setters.Add(new Setter(Control.BackgroundProperty, BrushFrom(48, 62, 69)));
        headerStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.WhiteSmoke));
        headerStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(6)));
        probes.ColumnHeaderStyle = headerStyle;
        Grid.SetRow(probes, 1); detail.Children.Add(probes);
        var logHeading = Heading("Ereignisprotokoll"); Grid.SetRow(logHeading, 2); detail.Children.Add(logHeading);
        log.MinHeight = 90;
        log.Background = SurfaceBrush;
        log.Foreground = Foreground;
        log.Padding = new Thickness(8);
        Grid.SetRow(log, 3); detail.Children.Add(log);
        grid.Children.Add(detail);
        Grid.SetColumn(diagnosticPreviewSlot, 1); grid.Children.Add(diagnosticPreviewSlot);
        return grid;
    }

    private void BuildPreviewPane()
    {
        var info = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        info.Children.Add(Heading("Bild & Erkennungsstatus"));
        previewSource.Foreground = Foreground;
        previewSource.TextWrapping = TextWrapping.Wrap;
        previewSource.FontWeight = FontWeights.SemiBold;
        info.Children.Add(previewSource);
        geometryText.Foreground = Muted; geometryText.TextWrapping = TextWrapping.Wrap;
        geometryText.Margin = new Thickness(0, 6, 0, 6); info.Children.Add(geometryText);
        detectionText.TextWrapping = TextWrapping.Wrap; info.Children.Add(detectionText);
        joinedText.TextWrapping = TextWrapping.Wrap; info.Children.Add(joinedText);
        var actions = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
        actions.Children.Add(Button("Live-Bild laden", ApplyPreviewSettings));
        actions.Children.Add(Button("Bild speichern", SaveImage));
        info.Children.Add(actions);
        DockPanel.SetDock(info, Dock.Top); previewPane.Children.Add(info);
        var legend = Hint("Teamfarben: Klickflächen  ·  Gelb: Dialogprüfung  ·  Cyan: HUD-Prüfung", 8);
        DockPanel.SetDock(legend, Dock.Bottom); previewPane.Children.Add(legend);
        previewHost.Background = Brushes.Black;
        previewHost.MinHeight = 160;
        previewHost.ClipToBounds = true;
        previewHost.Children.Add(preview);
        previewEmpty.TextWrapping = TextWrapping.Wrap;
        previewEmpty.Foreground = Muted;
        previewEmpty.HorizontalAlignment = HorizontalAlignment.Center;
        previewEmpty.VerticalAlignment = VerticalAlignment.Center;
        previewEmpty.Margin = new Thickness(24);
        previewEmpty.MaxWidth = 360;
        previewHost.Children.Add(previewEmpty);
        previewHost.Children.Add(overlay);
        previewPane.Children.Add(previewHost);
        previewHost.SizeChanged += (_, _) => DrawOverlay();
        overlay.MouseLeftButtonDown += PreviewDown;
        overlay.MouseMove += PreviewMove;
        overlay.MouseLeftButtonUp += PreviewUp;
        overlay.LostMouseCapture += (_, _) => CancelRegionDrag();
        SetPreviewEmpty("Noch kein Bild", "Wardogs öffnen und „Spiel suchen / Bild laden“ wählen. Unter Diagnose stehen zusätzlich Referenzbilder bereit.");
    }

    private void ShowPage(int index) => pages.SelectedIndex = index;

    private void UpdatePreviewLocation()
    {
        setupPreviewSlot.Children.Clear();
        diagnosticPreviewSlot.Children.Clear();
        if (pages.SelectedIndex == 0) setupPreviewSlot.Children.Add(previewPane);
        if (pages.SelectedIndex == 3) diagnosticPreviewSlot.Children.Add(previewPane);
        UpdateDrawingMode();
        UpdatePreviewTimer();
        if (PreviewVisible && !referenceMode) _ = RefreshPreview();
    }

    private bool PreviewVisible => pages.SelectedIndex is 0 or 3;

    private void UpdatePreviewTimer()
    {
        if (IsLoaded && PreviewVisible && liveUpdatesEnabled && !referenceMode) timer.Start();
        else timer.Stop();
    }

    private void UpdateDrawingMode()
    {
        CancelRegionDrag();
        overlay.Cursor = pages.SelectedIndex == 0 && drawRegion.IsChecked == true ? System.Windows.Input.Cursors.Cross : System.Windows.Input.Cursors.Arrow;
    }

    private void SetPreviewEmpty(string title, string explanation)
    {
        previewSource.Text = title;
        previewEmpty.Text = explanation;
        previewEmpty.Visibility = Visibility.Visible;
    }

    private void UpdateProfileState()
    {
        profileText.Text = dirty ? "Ungespeicherte Änderungen" : hasSavedProfile ? "Einstellungen gespeichert" : "Standardprofil · noch nicht gespeichert";
        profileText.Foreground = dirty ? WarningBrush : Muted;
        saveButton.IsEnabled = dirty || !hasSavedProfile || startupSettingsError != null;
        discardButton.Visibility = dirty ? Visibility.Visible : Visibility.Collapsed;
        UpdateOperationSummary();
    }

    private void UpdateOperationSummary()
    {
        bool testing = dryRun.IsChecked == true;
        modeText.Text = testing ? "Erkennung und Ablauf prüfen. Es werden keine Klicks an das Spiel gesendet." : "Echte Klicks sind aktiviert. Spiel im Vordergrund halten; ESC stoppt sofort.";
        modeText.Foreground = testing ? Muted : WarningBrush;
        modeNotice.BorderBrush = testing ? BorderBrushColor : WarningBrush;
        modeNotice.BorderThickness = new Thickness(1);
        operationSummary.Text = $"Klickintervall: {fields["min"].Text}–{fields["max"].Text} ms  ·  Fenster: {fields["title"].Text}\nBeitritt: fünf HUD-Balken für mindestens 0,5 s  ·  Stopp: ESC";
        status.ToolTip = testing ? "Testmodus · keine Mauseingaben" : "Echte Klicks aktiviert";
        var current = automation.Snapshot;
        string state = current.State switch { RunState.Waiting => "Wartet · " + TeamName(current.Team ?? Team.Blue), RunState.Clicking => "Klickt · " + TeamName(current.Team ?? Team.Blue), _ => "Gestoppt" };
        status.Text = state + (testing ? " · Testmodus" : " · echte Klicks");
    }

    private static TabItem Page(string title, UIElement content) => new()
    {
        Header = new TextBlock { Text = title, Foreground = Brushes.Black }, Content = content, Padding = new Thickness(16, 10, 16, 10),
        Foreground = Brushes.WhiteSmoke, Background = BrushFrom(230, 234, 236)
    };

    private static Grid Split(double leftWidth)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new() { Width = new GridLength(leftWidth) });
        grid.ColumnDefinitions.Add(new());
        return grid;
    }

    private static ScrollViewer Scroll(UIElement content) => new()
    {
        Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(0, 0, 6, 0)
    };

    private static StackPanel PageTitle(string title, string description)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 21, FontWeight = FontWeights.SemiBold, Foreground = Brushes.WhiteSmoke, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(Hint(description, 5));
        return panel;
    }

    private static TextBlock Hint(string text, double top = 0) => new()
    {
        Text = text, Foreground = Muted, TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, top, 0, 6), LineHeight = 20
    };

    private static Expander Expand(string title, UIElement content) => new()
    {
        Header = title, Content = content, Foreground = Brushes.WhiteSmoke,
        Margin = new Thickness(0, 14, 0, 0), Padding = new Thickness(0, 6, 0, 0)
    };

    private void AddField(Panel panel, string key, string name, int page)
    {
        var box = new TextBox { MinHeight = 32, Padding = new Thickness(6), Foreground = Brushes.Black, Background = Brushes.White, Margin = new Thickness(0, 0, 0, 6) };
        fields[key] = box;
        fieldPages[key] = page;
        AutomationProperties.SetName(box, name);
        box.TextChanged += (_, _) => { box.ClearValue(Control.BorderBrushProperty); box.ClearValue(ToolTipProperty); if (!loadingFields && key is "rx" or "ry" or "rw" or "rh") regionDirty = true; MarkDirty(); };
        panel.Children.Add(Label(name, box));
        panel.Children.Add(box);
    }

    private static Label Label(string text, UIElement target) => new()
    {
        Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap },
        Target = target, Foreground = Brushes.WhiteSmoke, Padding = new Thickness(0),
        Margin = new Thickness(0, 7, 0, 4)
    };

    private static SolidColorBrush BrushFrom(byte red, byte green, byte blue) => new(Color.FromRgb(red, green, blue));
    private static readonly Brush SurfaceBrush = BrushFrom(28, 38, 43);
    private static readonly Brush BorderBrushColor = BrushFrom(65, 79, 85);
    private static readonly Brush ErrorBrush = BrushFrom(255, 161, 137);
    private static readonly Brush WarningBrush = BrushFrom(245, 204, 123);
}
