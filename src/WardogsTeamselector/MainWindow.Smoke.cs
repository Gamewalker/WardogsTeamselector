using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WardogsTeamselector.Core;

namespace WardogsTeamselector;

public sealed partial class MainWindow
{
    internal async Task CaptureSmokeImages(string directory)
    {
        Directory.CreateDirectory(directory);
        LocalizeInterface();
        Check(Localization.CurrentLanguage == "en" && ((Localization.Language)languageSelector.SelectedItem).Code == "en", "First launch selects English");
        Check(ButtonLabel(stopButton) == "Stop · ESC" && (profileText.Text.Contains("Default profile") || hasSavedProfile), "English header and profile");
        SaveRender(Path.Combine(directory, "english-startup.png"));
        foreach (var language in Localization.Languages)
        {
            languageSelector.SelectedItem = language;
            LocalizeInterface();
            Check(ButtonLabel(stopButton) == Localization.Text("Stopp · ESC"), "Stop label follows language: " + language.Code);
        }
        languageSelector.SelectedItem = Localization.Languages.Single(l => l.Code == "de");
        LocalizeInterface();
        Check(Icon != null, "App icon loaded");
        Check(Title.Contains(BuildDescription), "Current build is visible in window title");
        Check(!restartUpdateButton.IsEnabled, "Restart requires a verified update");
        Check(pages.Items.Count == 4, "Four task areas");
        Check(updateStatus.Text.Contains("GUI-Prüflauf") && !updateTimer.IsEnabled && stagedUpdate == null, "Smoke mode never checks or stages updates");
        Check(pages.SelectedIndex == (hasSavedProfile && startupSettingsError == null ? 1 : 0), "Startup follows saved profile");

        // Use an in-memory fixture: smoke mode never writes the user's profile or registers hotkeys.
        settings = new AppSettings(); startupSettingsError = null; dirty = false;
        LoadFields(); ApplyHotkeys(settings);
        Check(!dirty && !regionDirty, "Loading fields does not create unsaved edits");
        Check(focusGame.IsChecked == true && ReadFields().FocusGameOnTeamActivation, "Game focus defaults to enabled");
        Check(!stopButton.IsEnabled, "Stop is disabled before activation");
        Check(!LogicalElements((DependencyObject)((TabItem)pages.Items[1]).Content).Contains(dryRun) && LogicalElements((DependencyObject)((TabItem)pages.Items[3]).Content).Contains(dryRun), "Test-mode control belongs exclusively to diagnosis");
        Check(operationState.TextAlignment == TextAlignment.Center && runReason.TextAlignment == TextAlignment.Center && counters.TextAlignment == TextAlignment.Center, "Current run is centered");
        Check(teamButtons.Values.All(button => button.HorizontalContentAlignment == HorizontalAlignment.Center) && teamKeyLabels.Values.All(label => label.TextAlignment == TextAlignment.Center), "Hotkeys are centered on team buttons");
        ShowPage(0); LoadReference(); await Settle();
        Check(detectionText.Text.Contains("Dialog: erkannt"), "Embedded dialog reference detected");

        foreach (var code in new[] { "en", "ar", "zh-CN" })
        {
            languageSelector.SelectedItem = Localization.Languages.Single(l => l.Code == code);
            foreach (var size in new[] { ("desktop", 1180d, 820d), ("small", MinWidth, MinHeight) })
            {
                Width = size.Item2; Height = size.Item3;
                for (int page = 0; page < pages.Items.Count; page++)
                {
                    ShowPage(page); await Settle();
                    Check(!dirty && !regionDirty, "Translation and navigation preserve saved field state");
                    var headerPosition = languageSelector.TranslatePoint(new Point(languageSelector.ActualWidth, 0), (UIElement)Content);
                    Check(headerPosition.X <= ((FrameworkElement)Content).ActualWidth + 1, "Language selector fits the header at minimum width");
                    SaveRender(Path.Combine(directory, $"{code}-{size.Item1}-{page + 1}.png"));
                }
            }
        }
        languageSelector.SelectedItem = Localization.Languages.Single(l => l.Code == "de");
        ShowPage(0); await Settle();

        foreach (var size in new[] { ("desktop", 1180d, 820d), ("small", MinWidth, MinHeight) })
        {
            Width = size.Item2; Height = size.Item3;
            for (int page = 0; page < pages.Items.Count; page++)
            {
                ShowPage(page); await Settle();
                foreach (var scroll in LogicalElements(((TabItem)pages.Items[page]).Content as DependencyObject ?? pages).OfType<ScrollViewer>()) scroll.ScrollToTop();
                await Settle();
                Check(!dirty, "Navigation does not change settings");
                Check(page is not (0 or 3) || previewPane.Parent == (page == 0 ? setupPreviewSlot : diagnosticPreviewSlot), "Preview belongs to selected task");
                if (page is 0 or 3)
                    Check(previewHost.ActualHeight <= ((FrameworkElement)previewHost.Parent).ActualHeight + 1, "Preview fits its frame without clipping at either window size");
                SaveRender(Path.Combine(directory, $"{size.Item1}-{page + 1}.png"));
            }
            ShowPage(2);
            Check(LogicalElements((DependencyObject)((TabItem)pages.Items[2]).Content).Contains(automaticUpdates), "Update controls belong to configuration");
            LogicalElements((DependencyObject)((TabItem)pages.Items[2]).Content).OfType<ScrollViewer>().First().ScrollToEnd();
            await Settle();
            SaveRender(Path.Combine(directory, $"{size.Item1}-updates.png"));
            ShowPage(0);
            var expanders = LogicalElements((DependencyObject)Content).OfType<Expander>().ToArray();
            var coordinates = expanders.Single(e => (string)e.Header == "Teamfläche als Prozentwerte");
            coordinates.IsExpanded = true;
            await Settle();
            LogicalElements(((TabItem)pages.Items[0]).Content as DependencyObject ?? pages).OfType<ScrollViewer>().First().ScrollToEnd();
            await Settle();
            SaveRender(Path.Combine(directory, $"{size.Item1}-calibration.png"));
            coordinates.IsExpanded = false;
            ShowPage(2);
            var calibration = expanders.Single(e => (string)e.Header == "Erweiterte Dialogkalibrierung");
            calibration.IsExpanded = true;
            await Settle(); fields["scale"].BringIntoView(); await Settle();
            SaveRender(Path.Combine(directory, $"{size.Item1}-advanced.png"));
            calibration.IsExpanded = false;
        }
        Check(overlay.Cursor != System.Windows.Input.Cursors.Cross, "Diagnosis does not offer region drawing");
        ShowPage(0); drawRegion.IsChecked = true;
        await Settle();
        Check(overlay.Cursor == System.Windows.Input.Cursors.Cross, "Drawing requires explicit setup mode");
        var area = ImageArea();
        dragStart = new System.Windows.Point(area.X + area.W * .3864, area.Y + area.H * .4374);
        DrawDragSelection(dragStart.Value, new System.Windows.Point(area.X + area.W * .48, area.Y + area.H * .63));
        Check(overlay.Children.Contains(dragOutline) && dragOutline.Width > 0 && dragOutline.Height > 0, "Drawing shows a bounded selection rectangle");
        await Settle();
        SaveRender(Path.Combine(directory, "drawing-selection.png"));
        ShowPage(1);
        Check(dragStart == null && dragEnd == null && !overlay.IsMouseCaptured && !overlay.Children.Contains(dragOutline), "Navigation cancels a calibration drag and clears feedback");
        drawRegion.IsChecked = false;

        // Percent edits participate in Save/Start and survive selecting another team.
        ShowPage(0); fields["rw"].Text = "8";
        Check(Math.Abs(ReadFields().Regions.Single(r => r.Team == Team.Blue).Width - .08) < .000001, "Pending region fields are applied to the next configuration");
        selectedTeam.SelectedIndex = 1;
        Check(Math.Abs(settings.Regions.Single(r => r.Team == Team.Blue).Width - .08) < .000001, "Region edits survive team selection");
        fields["rw"].Text = "120";
        selectedTeam.SelectedIndex = 2;
        Check(selectedTeam.SelectedIndex == 1 && errorText.Visibility == Visibility.Visible, "Invalid region blocks team change and exposes recovery");
        fields["rw"].Text = "8"; ApplyRegion(); ClearError();

        // Bad input must take the user to the offending control without starting a run.
        ShowPage(1); fields["min"].Text = "49"; StartTeam(Team.Blue);
        Check(pages.SelectedIndex == 2 && errorText.Visibility == Visibility.Visible && automation.Snapshot.State == RunState.Stopped, "Invalid interval navigates to configuration and blocks activation");
        await Settle(); SaveRender(Path.Combine(directory, "validation.png"));
        fields["min"].Text = "50"; ClearError();
        keyBoxes[Team.Red].SelectedIndex = keyBoxes[Team.Blue].SelectedIndex;
        StartTeam(Team.Blue);
        Check(pages.SelectedIndex == 2 && errorText.Text.Contains("andere F-Taste") && automation.Snapshot.State == RunState.Stopped, "Duplicate hotkeys block activation");
        await Settle();
        Check(errorText.Text.Contains("Blau und Rot verwenden F6") && keyBoxes[Team.Red].IsKeyboardFocused, "Conflicting key and teams are named, and the conflicting input is focused");
        SaveRender(Path.Combine(directory, "hotkey-validation.png"));
        keyBoxes[Team.Red].SelectedIndex = 6; ClearError();

        fields["title"].Text = "UI-Prüfung-" + Guid.NewGuid().ToString("N");
        fields["process"].Text = "KeinSpielprozess";
        StartTeam(Team.Blue); await Settle();
        Check(dirty && profileText.Text.Contains("Ungespeicherte"), "Activation does not claim edits are saved");
        Check(pages.SelectedIndex == 1 && automation.Snapshot.State == RunState.Waiting && !timer.IsEnabled, "Operation waits without extra preview captures");
        string draftTitle = fields["title"].Text;
        languageSelector.SelectedItem = Localization.Languages.Single(l => l.Code == "en");
        await Settle();
        Check(automation.Snapshot.State == RunState.Waiting && dirty && fields["title"].Text == draftTitle, "Language switching preserves an active run and unsaved drafts");
        Check(teamStateLabels[Team.Blue].Text == Localization.Text("Aktiv · wartet"), "Active markers change language");
        SaveRender(Path.Combine(directory, "english-waiting.png"));
        languageSelector.SelectedItem = Localization.Languages.Single(l => l.Code == "ar");
        await Settle();
        Check(operationState.FlowDirection == FlowDirection.RightToLeft, "Arabic text uses RTL");
        SaveRender(Path.Combine(directory, "arabic-waiting.png"));
        languageSelector.SelectedItem = Localization.Languages.Single(l => l.Code == "de");
        await Settle();
        Check(stopButton.IsEnabled && teamStateLabels[Team.Blue].Text == "Aktiv · wartet" && teamStateLabels.Where(pair => pair.Key != Team.Blue).All(pair => pair.Value.Text == "Aktivieren"), "Exactly the active team is marked while waiting, and stop is enabled");
        SaveRender(Path.Combine(directory, "waiting.png"));
        Width = 1180; Height = 820; await Settle();
        SaveRender(Path.Combine(directory, "desktop-waiting.png"));
        Width = MinWidth; Height = MinHeight; await Settle();
        foreach (int page in new[] { 0, 2, 3, 1 }) ShowPage(page);
        Check(automation.Snapshot.State == RunState.Waiting, "Navigating between tasks preserves an active run");
        focusGame.IsChecked = false;
        Check(!ReadFields().FocusGameOnTeamActivation && dirty && automation.Snapshot.State == RunState.Waiting, "Disabling game focus applies to next activation without stopping the current run");
        liveUpdates.IsChecked = false; await RefreshPreview();
        Check(!timer.IsEnabled && preview.Source == null && probes.ItemsSource == null && automation.Snapshot.State == RunState.Waiting, "Disabling preview frees detail resources without stopping automation");
        StartTeam(Team.Red); await Settle();
        Check(!settings.FocusGameOnTeamActivation, "Team activation respects disabled game focus");
        Check(teamStateLabels[Team.Red].Text == "Aktiv · wartet" && teamStateLabels[Team.Blue].Text == "Aktivieren", "Switching teams moves the active marker");
        SaveRender(Path.Combine(directory, "red-waiting.png"));
        StartTeam(Team.Green); await Settle();
        Check(teamStateLabels[Team.Green].Text == "Aktiv · wartet" && teamStateLabels[Team.Red].Text == "Aktivieren", "Green is marked without retaining another active team");
        ShowPage(3); dryRun.IsChecked = false; await Settle();
        Check(automation.Snapshot.State == RunState.Stopped && !stopButton.IsEnabled && teamStateLabels.Values.All(label => label.Text == "Aktivieren"), "Changing test mode in diagnosis stops the run and clears markers");
        dryRun.IsChecked = true;
        StartTeam(Team.Blue); await Settle();
        stopButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        await Settle();
        Check(automation.Snapshot.State == RunState.Stopped && !stopButton.IsEnabled && teamStateLabels.Values.All(label => label.Text == "Aktivieren"), "Stopping disables stop and clears the active team");
        SaveRender(Path.Combine(directory, "stopped.png"));

        // UI-only snapshot fixture: the controller stays stopped and sends no input.
        OnAutomation(new AutomationSnapshot(RunState.Clicking, Team.Green, 12, 61, "Testmodus: Klick simuliert", null, null));
        await Settle();
        Check(operationState.Text == "Klickt · Grün" && teamStateLabels[Team.Green].Text == "Aktiv · klickt" && stopButton.IsEnabled, "Clicking state has clear phase, team and active controls");
        SaveRender(Path.Combine(directory, "clicking-fixture.png"));
        OnAutomation(automation.Snapshot); await Settle();

        ShowPage(3); LoadJoinedReference(); await Settle();
        Check(joinedText.Text.Contains("HUD erkannt") && detectionText.Text.Contains("Dialog: nicht erkannt") && !timer.IsEnabled, "HUD reference works while automatic preview is off");
        SaveRender(Path.Combine(directory, "hud-reference.png"));
        referenceMode = false; await RefreshPreview(true); await Settle();
        Check(previewEmpty.Visibility == Visibility.Visible && previewSource.Text == "Spielfenster fehlt", "Single capture exposes missing-window empty state");
        SaveRender(Path.Combine(directory, "missing-window.png"));
        Check(errorText.Visibility == Visibility.Collapsed, "Expected validation errors cleared");
        var about = BuildAboutWindow();
        about.Show(); await Settle();
        SaveRender(Path.Combine(directory, "about.png"), about);
        about.Width = about.MinWidth; about.Height = about.MinHeight;
        await Settle();
        SaveRender(Path.Combine(directory, "about-small.png"), about);
        ((ScrollViewer)about.Content).ScrollToEnd(); await Settle();
        SaveRender(Path.Combine(directory, "about-small-bottom.png"), about);
        about.Close();
        File.WriteAllText(Path.Combine(directory, "checks.txt"), "PASS: startup routing, four areas, both sizes, navigation, drawing, region drafts, validation, hotkeys, dirty state, preview lifecycle, diagnosis-only test mode, centered run and hotkeys, exclusive active-team markers, waiting/switching/stopping and UI-only clicking snapshot, conditional stop, reference checks and empty state. No mouse input sent; no profile saved. clicking-fixture.png uses a UI snapshot fixture while the controller is stopped.");
    }

    private async Task Settle()
    {
        LocalizeInterface();
        UpdateLayout();
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        LocalizeInterface();
        UpdateLayout();
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("GUI check failed: " + message);
    }

    private static string ButtonLabel(Button button) => button.Content is string label ? label
        : LogicalElements(button).OfType<TextBlock>().Single().Text;

    private static System.Collections.Generic.IEnumerable<DependencyObject> LogicalElements(DependencyObject root)
    {
        yield return root;
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            foreach (var element in LogicalElements(child)) yield return element;
    }
}
