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
        Check(Icon != null, "App icon loaded");
        Check(pages.Items.Count == 4, "Four task areas");
        Check(pages.SelectedIndex == (hasSavedProfile && startupSettingsError == null ? 1 : 0), "Startup follows saved profile");

        // Use an in-memory fixture: smoke mode never writes the user's profile or registers hotkeys.
        settings = new AppSettings(); startupSettingsError = null; dirty = false;
        LoadFields(); ApplyHotkeys(settings);
        Check(!dirty && !regionDirty, "Loading fields does not create unsaved edits");
        ShowPage(0); LoadReference(); await Settle();
        Check(detectionText.Text.Contains("Dialog: erkannt"), "Embedded dialog reference detected");

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
                SaveRender(Path.Combine(directory, $"{size.Item1}-{page + 1}.png"));
            }
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
        SaveRender(Path.Combine(directory, "waiting.png"));
        foreach (int page in new[] { 0, 2, 3, 1 }) ShowPage(page);
        Check(automation.Snapshot.State == RunState.Waiting, "Navigating between tasks preserves an active run");
        liveUpdates.IsChecked = false; await RefreshPreview();
        Check(!timer.IsEnabled && preview.Source == null && probes.ItemsSource == null && automation.Snapshot.State == RunState.Waiting, "Disabling preview frees detail resources without stopping automation");
        stopButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        Check(automation.Snapshot.State == RunState.Stopped, "Persistent stop control stops the run");

        ShowPage(3); LoadJoinedReference(); await Settle();
        Check(joinedText.Text.Contains("HUD erkannt") && detectionText.Text.Contains("Dialog: nicht erkannt") && !timer.IsEnabled, "HUD reference works while automatic preview is off");
        SaveRender(Path.Combine(directory, "hud-reference.png"));
        referenceMode = false; await RefreshPreview(true); await Settle();
        Check(previewEmpty.Visibility == Visibility.Visible && previewSource.Text == "Spielfenster fehlt", "Single capture exposes missing-window empty state");
        SaveRender(Path.Combine(directory, "missing-window.png"));
        Check(errorText.Visibility == Visibility.Collapsed, "Expected validation errors cleared");
        File.WriteAllText(Path.Combine(directory, "checks.txt"), "PASS: startup routing, four areas, all tabs at both sizes, navigation, drawing guard, region drafts, validation recovery, duplicate hotkeys, dirty state, preview lifecycle, global stop, reference checks and empty state. No mouse input sent; no profile saved.");
    }

    private async Task Settle()
    {
        UpdateLayout();
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        UpdateLayout();
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("GUI check failed: " + message);
    }

    private static System.Collections.Generic.IEnumerable<DependencyObject> LogicalElements(DependencyObject root)
    {
        yield return root;
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            foreach (var element in LogicalElements(child)) yield return element;
    }
}
