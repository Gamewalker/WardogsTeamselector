using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WardogsTeamselector.Core;
using WardogsTeamselector.Groups;

namespace WardogsTeamselector;

public sealed partial class MainWindow
{
    internal async Task CaptureSmokeImages(string directory)
    {
        Directory.CreateDirectory(directory);
        // Render the actual WPF icon family at button size for one visual inspection.
        var iconGallery = new System.Windows.Controls.Primitives.UniformGrid { Columns = 6, Background = Background };
        TextBlock.SetForeground(iconGallery, Foreground);
        foreach (var icon in Enum.GetValues<ActionIcon>())
        {
            var cell = new StackPanel { Margin = new Thickness(12) };
            var glyph = IconPath(icon); glyph.HorizontalAlignment = HorizontalAlignment.Center;
            cell.Children.Add(glyph);
            cell.Children.Add(new TextBlock { Text = icon.ToString(), TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 8, 0, 0), FontSize = 12 });
            iconGallery.Children.Add(cell);
        }
        var gallerySize = new Size(780, Math.Ceiling(iconGallery.Children.Count / 6d) * 72);
        iconGallery.Measure(gallerySize); iconGallery.Arrange(new Rect(gallerySize)); iconGallery.UpdateLayout();
        var galleryWindow = new Window { Content = iconGallery, Background = Background };
        SaveRender(Path.Combine(directory, "button-icons.png"), galleryWindow);
        galleryWindow.Close();
        LocalizeInterface();
        Check(Localization.CurrentLanguage == "en" && ((Localization.Language)languageSelector.SelectedItem).Code == "en", "First launch selects English");
        Check(profileText.Text.Contains("Default profile") || hasSavedProfile, "English profile");
        Check(groupService.Text == GroupProfile.DefaultServiceUrl, "New group settings show the default service address");
        var invitationToken = GroupMembership.NewToken();
        var initialPage = pages.SelectedIndex;
        var invitation = "https://groups.example/invite/" + GroupMembership.NewId() + "#" + invitationToken;
        Exception? invitationFailure = null;
        bool invitationDialogSeen = false;
        var invitationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        invitationTimer.Tick += (_, _) =>
        {
            var dialog = OwnedWindows.Cast<Window>().FirstOrDefault(w => w.IsLoaded);
            if (dialog == null) return;
            invitationTimer.Stop();
            try
            {
                var children = ((StackPanel)dialog.Content).Children.Cast<UIElement>();
                var fields = children.OfType<TextBox>().ToList();
                Check(fields.Count == 1, "App invitation asks only for the player name");
                var labels = children.OfType<TextBlock>().Select(x => x.Text).ToList();
                Check(labels.Any(x => x.Contains("https://groups.example")), "App invitation shows the destination service before confirming");
                Check(!labels.Any(x => x.Contains(invitationToken)), "App invitation keeps its secret out of the dialog");
                SaveRender(Path.Combine(directory, "invitation-dialog.png"), dialog);
                invitationDialogSeen = true;
            }
            catch (Exception ex) { invitationFailure = ex; }
            finally { dialog.DialogResult = false; }
        };
        var initialGroupCount = groupProfile.Groups.Count;
        invitationTimer.Start();
        ReceiveInvitationActivation("wardogs://join/#" + Uri.EscapeDataString(invitation));
        while (receivingInvitation) await Task.Delay(50);
        invitationTimer.Stop();
        if (invitationFailure != null) throw invitationFailure;
        Check(invitationDialogSeen && groupProfile.Groups.Count == initialGroupCount, "Cancelling an app invitation does not save or join a group");
        Check(groupStatus.Text == "Gruppenbeitritt abgebrochen. Es wurde keine Anfrage gesendet." && managementGroupStatus.Text == groupStatus.Text, "Cancelled invitation remains visible in group management");
        ReceiveInvitationActivation("wardogs://invalid/");
        while (receivingInvitation) await Task.Delay(50);
        Check(groupStatus.Text == "Ungültiger Einladungslink." && managementGroupStatus.Text == groupStatus.Text, "Invalid invitation feedback is not overwritten for an empty group list");
        ShowPage(initialPage);
        var originalCheckboxes = PersistedCheckboxes().ToDictionary(pair => pair.Key, pair => pair.Value.IsChecked == true);
        shareTeamWithGroup.IsChecked = true;
        Check(checkboxPreferences["ShareTeamWithGroup"], "Sharing checkbox is saved immediately without saving the profile");
        var restoredCheckboxes = new System.Collections.Generic.Dictionary<string, bool>(originalCheckboxes)
            { ["ShareTeamWithGroup"] = true, ["AutoFollow"] = true, ["FocusGame"] = false, ["LivePreview"] = false };
        checkboxPreferences = restoredCheckboxes;
        ApplyCheckboxPreferences();
        Check(shareTeamWithGroup.IsChecked == true && groupAuto.IsChecked == true && focusGame.IsChecked == false && !liveUpdatesEnabled && !groupFollow.Enabled,
            "Saved checkboxes restore without triggering a join while startup fields are loading");
        closing = true;
        try { StopGroupFollow(); }
        finally { closing = false; }
        Check(checkboxPreferences["AutoFollow"], "Closing the app preserves Auto for the next launch");
        checkboxPreferences = originalCheckboxes; ApplyCheckboxPreferences();
        SaveRender(Path.Combine(directory, "english-startup.png"));
        foreach (var language in Localization.Languages)
        {
            languageSelector.SelectedItem = language;
            LocalizeInterface();
            UpdateRunDisplay(new AutomationSnapshot(RunState.Waiting, Team.Blue, 0, 0, "", null, null));
            LocalizeInterface();
            Check(teamNameLabels[Team.Blue].Text == Localization.Text("Stopp"), "Stop label follows language: " + language.Code);
            UpdateRunDisplay(automation.Snapshot);
        }
        languageSelector.SelectedItem = Localization.Languages.Single(l => l.Code == "de");
        LocalizeInterface();
        Check(Icon is System.Windows.Media.Imaging.BitmapSource { PixelWidth: >= 256, PixelHeight: >= 256 }, "HD app icon loaded");
        Check(Title.Contains(BuildDescription), "Current build is visible in window title");
        Check(!restartUpdateButton.IsEnabled, "Restart requires a verified update");
        Check(headerUpdateButton.Visibility == Visibility.Collapsed && !headerUpdateButton.IsEnabled, "Header update action is hidden without a new version");
        availableUpdate = new Updates.UpdateAsset(CurrentBuild + 1, 123, 4, new string('a', 64));
        updateBusy = true;
        UpdateHeaderUpdateButton();
        Check(headerUpdateButton.Visibility == Visibility.Visible && !headerUpdateButton.IsEnabled, "Header shows a new version while downloading and prevents duplicate downloads");
        updateBusy = false;
        UpdateHeaderUpdateButton();
        Check(headerUpdateButton.IsEnabled, "Failed downloads can be retried from the header");
        InstallHeaderUpdate();
        Check(!updateBusy && stagedUpdate == null && !restartAfterUpdate, "Smoke header action never downloads or restarts");
        stagedUpdate = Path.Combine(directory, "missing-update-fixture.exe");
        stagedAsset = availableUpdate;
        UpdateHeaderUpdateButton();
        Check(headerUpdateButton.IsEnabled, "Verified update can be installed from the header");
        stagedUpdate = null; stagedAsset = null;
        Check(pages.Items.Count == 5, "Five task areas including group management");
        Check(updateStatus.Text.Contains("GUI-Prüflauf") && !updateTimer.IsEnabled && stagedUpdate == null, "Smoke mode never checks or stages updates");
        Check(pages.SelectedIndex == (hasSavedProfile && startupSettingsError == null ? 1 : 0), "Startup follows saved profile");

        // Use an in-memory fixture: smoke mode never writes the user's profile or registers hotkeys.
        settings = new AppSettings(); startupSettingsError = null; dirty = false;
        LoadFields(); ApplyHotkeys(settings);
        Check(settings.StopHotkey == 0x23 && dryRun.IsChecked == false, "New profiles use End and disable test mode");
        foreach (var key in HotkeyChoice.All)
            Check(IconForAction("Stopp · " + key.Name) == ActionIcon.Stop, "Stop icon supports " + key.Name);
        Check(!dirty && !regionDirty, "Loading fields does not create unsaved edits");
        Check(focusGame.IsChecked == true && ReadFields().FocusGameOnTeamActivation, "Game focus defaults to enabled");
        foreach (var code in new[] { "en", "de" })
        {
            languageSelector.SelectedItem = Localization.Languages.Single(l => l.Code == code);
            LocalizeInterface();
            Check(operationFocusHint.Text == Localization.Text("Auto-Fokus ist an: Ein Team aktivieren, und die App holt das Spiel in den Vordergrund."), "Operation explains automatic focus: " + code);
            focusGame.IsChecked = false; LocalizeInterface();
            Check(operationFocusHint.Text == Localization.Text("Auto-Fokus ist aus: Ein Team aktivieren, dann selbst zum Spiel wechseln."), "Operation explains manual focus immediately: " + code);
            dirty = false; LoadFields(); LocalizeInterface();
            Check(focusGame.IsChecked == true && operationFocusHint.Text == Localization.Text("Auto-Fokus ist an: Ein Team aktivieren, und die App holt das Spiel in den Vordergrund."), "Loading saved fields restores automatic focus hint: " + code);
        }
        Check(teamButtons.Values.All(button => button.Opacity == 1), "All teams fully visible before activation");
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
                    Check(profileFooter.Visibility == (page is 1 or 4 ? Visibility.Collapsed : Visibility.Visible), "Profile save actions stay hidden in operation and group administration");
                    var languagePosition = languageSelector.TranslatePoint(new Point(), (UIElement)Content);
                    var aboutButton = ((StackPanel)languageSelector.Parent).Children.OfType<Button>().Single(button => button != headerUpdateButton);
                    var aboutPosition = aboutButton.TranslatePoint(new Point(), (UIElement)Content);
                    Check(Math.Abs(languagePosition.Y + languageSelector.ActualHeight / 2 - aboutPosition.Y - aboutButton.ActualHeight / 2) < 1, "Language selector is centered with the about button");
                    var headerPosition = languageSelector.TranslatePoint(new Point(languageSelector.ActualWidth, 0), (UIElement)Content);
                    Check(headerPosition.X <= ((FrameworkElement)Content).ActualWidth + 1, "Language selector fits the header at minimum width");
                    var header = (Grid)((StackPanel)languageSelector.Parent).Parent;
                    var identityTitle = (FrameworkElement)((Grid)header.Children[0]).Children[1];
                    var identityEnd = identityTitle.TranslatePoint(new Point(identityTitle.ActualWidth, 0), (UIElement)Content);
                    var updatePosition = headerUpdateButton.TranslatePoint(new Point(), (UIElement)Content);
                    Check(identityEnd.X <= updatePosition.X + 1, "Identity text does not overlap the update action at minimum width");
                    SaveRender(Path.Combine(directory, $"{code}-{size.Item1}-{page + 1}.png"));
                    var leftTab = (TabItem)pages.Items[1]; var rightTab = (TabItem)pages.Items[2]; var lastTab = (TabItem)pages.Items[4];
                    var leftEdge = leftTab.TranslatePoint(new Point(leftTab.ActualWidth, 0), pages);
                    var rightEdge = rightTab.TranslatePoint(new Point(), pages);
                    var lastEdge = lastTab.TranslatePoint(new Point(lastTab.ActualWidth, 0), pages);
                    Check(rightEdge.Y > leftEdge.Y || rightEdge.X >= leftEdge.X + 10, "Left and right navigation areas do not overlap");
                    Check(lastEdge.X <= pages.ActualWidth + 1, "Right navigation fits minimum width");
                }
            }
        }
        DiscardStagedUpdate();
        Check(headerUpdateButton.Visibility == Visibility.Collapsed && !headerUpdateButton.IsEnabled, "Discarding an update hides its header action");
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
        Check(operationTabs.Items.Count == 2 && !groupFollow.Enabled && groupAuto.IsChecked != true, "Two join modes and Auto off at startup");
        var fixtureOwner = new GroupMembership { ServiceUrl = "https://groups.example", GroupId = GroupMembership.NewId(), MemberId = GroupMembership.NewId(), Token = GroupMembership.NewToken(), InviteToken = GroupMembership.NewToken(), Name = "Freunde", DisplayName = "Ersteller", Role = "Owner", Status = "Approved" };
        groupProfile.Groups.Add(fixtureOwner); ReloadGroupPickers();
        groupPicker.SelectedItem = fixtureOwner;
        selectedGroupSnapshot = new GroupSnapshot(1, fixtureOwner.GroupId, fixtureOwner.Name, 3, 1, "Red", "Approved", "Owner", fixtureOwner.MemberId, new() { new(fixtureOwner.MemberId, fixtureOwner.DisplayName, "Approved", "Owner", 0), new(GroupMembership.NewId(), "Mitspieler", "Pending", "Member", 0) });
        groupStatus.Text = DescribeGroup(selectedGroupSnapshot); UpdateGroupControls();
        Check(shareTeamWithGroup.IsChecked != true && !shareGroupPicker.IsEnabled, "Group sharing starts off with a disabled picker");
        shareTeamWithGroup.IsChecked = true;
        Check(shareGroupPicker.IsEnabled, "Sharing checkbox enables the group picker");
        groupBusy = true; UpdateGroupControls();
        Check(!shareGroupPicker.IsEnabled && teamButtons.Values.All(button => !button.IsEnabled), "Publishing locks group and team selection");
        var pendingShareGeneration = groupFollow.Generation;
        shareTeamWithGroup.IsChecked = false;
        Check(groupFollow.Generation != pendingShareGeneration && !shareGroupPicker.IsEnabled && teamButtons.Values.All(button => button.IsEnabled), "Disabling sharing invalidates a pending join and restores local buttons");
        groupBusy = false; UpdateGroupControls();
        ShowPage(1); operationTabs.SelectedIndex = 1;
        Check(groupRequests.Items.Count == 1 && groupMembers.Items.Count == 1, "Requests are separate from confirmed members");
        Check(!LogicalElements((DependencyObject)((TabItem)pages.Items[1]).Content).Contains(groupMembers) && LogicalElements((DependencyObject)((TabItem)pages.Items[4]).Content).Contains(groupMembers), "Administration belongs exclusively to group management");
        Check(ReferenceEquals(groupPicker.SelectedItem, shareGroupPicker.SelectedItem) && ReferenceEquals(groupPicker.SelectedItem, managementGroupPicker.SelectedItem), "All modes share one active group");
        foreach (var size in new[] { ("desktop", 1180d, 820d), ("small", MinWidth, MinHeight) })
        {
            Width = size.Item2; Height = size.Item3; await Settle();
            operationTabs.SelectedIndex = 0; shareTeamWithGroup.IsChecked = true; await Settle();
            Check(shareGroupPicker.TranslatePoint(new Point(0, shareGroupPicker.ActualHeight), this).Y < teamButtons[Team.Blue].TranslatePoint(new Point(), this).Y, "Group sharing sits above the normal team buttons");
            SaveRender(Path.Combine(directory, size.Item1 + "-share-team.png"));
            shareTeamWithGroup.IsChecked = false; await Settle();
            SaveRender(Path.Combine(directory, size.Item1 + "-local-team.png"));
            operationTabs.SelectedIndex = 1; await Settle();
            SaveRender(Path.Combine(directory, size.Item1 + "-groups.png"));
            ShowPage(4); await Settle(); SaveRender(Path.Combine(directory, size.Item1 + "-group-management.png"));
            groupRequests.SelectedIndex = 0; groupRequests.Focus(); await Settle();
            SaveRender(Path.Combine(directory, size.Item1 + "-group-request-selected.png"));
            managementGroupPicker.Focus(); groupRequests.BringIntoView(); await Settle();
            SaveRender(Path.Combine(directory, size.Item1 + "-group-request-selected-inactive.png"));
            groupMembers.SelectedIndex = 0; groupMembers.Focus(); await Settle();
            SaveRender(Path.Combine(directory, size.Item1 + "-group-member-selected.png"));
            managementGroupPicker.Focus(); groupMembers.BringIntoView(); await Settle();
            SaveRender(Path.Combine(directory, size.Item1 + "-group-member-selected-inactive.png"));
            groupMembers.SelectedIndex = -1;
            var managementScroll = (ScrollViewer)((TabItem)pages.Items[4]).Content;
            managementScroll.ScrollToBottom(); await Settle();
            SaveRender(Path.Combine(directory, size.Item1 + "-group-member-actions.png"));
            var managementExpanders = LogicalElements(managementScroll).OfType<Expander>().ToArray();
            foreach (var expander in managementExpanders) expander.IsExpanded = true;
            await Settle(); managementScroll.ScrollToBottom(); await Settle();
            SaveRender(Path.Combine(directory, size.Item1 + "-group-management-service.png"));
            foreach (var expander in managementExpanders.Skip(1)) expander.IsExpanded = false;
            managementScroll.ScrollToTop(); ShowPage(1);
        }
        var fixtureGeneration = groupFollow.Begin(fixtureOwner, true);
        groupFollow.Apply(fixtureGeneration, selectedGroupSnapshot, System.Diagnostics.Stopwatch.GetTimestamp());
        UpdateGroupControls(); Check(groupStopButton.IsEnabled, "Global Stop is available while Auto waits");
        followedSettings = new AppSettings(); followedMembership = fixtureOwner;
        groupUiLoading = true; groupAuto.IsChecked = true; groupUiLoading = false;
        automation.Stop("Spielfokus verloren", AutomationStopCause.Safety);
        TickGroupFollow();
        Check(groupFollow.Enabled && groupFollow.Auto && groupAuto.IsChecked == true,
            "Auto remains enabled after focus loss and waits for another dialog");
        foreach (var reason in new[] { "Beobachtung fehlgeschlagen", "Spielbereich geändert", "Aufnahme/Steuerung fehlgeschlagen" })
        {
            nextGroupRecovery = 0;
            automation.Stop(reason, AutomationStopCause.Safety);
            TickGroupFollow();
            Check(groupFollow.Enabled && groupFollow.Auto && groupAuto.IsChecked == true && automation.Snapshot.State == RunState.Stopped
                && automation.Snapshot.StopCause == AutomationStopCause.GroupUpdate,
                "Auto recovers to observation without input after: " + reason);
        }
        groupFollow.Disconnected(fixtureGeneration);
        automation.Stop("Gruppenverbindung pausiert", AutomationStopCause.GroupUpdate);
        TickGroupFollow();
        Check(groupFollow.Enabled && groupAuto.IsChecked == true && automation.Snapshot.State == RunState.Stopped,
            "Temporary disconnection keeps Auto enabled without starting clicks");
        groupFollow.Apply(fixtureGeneration, selectedGroupSnapshot, System.Diagnostics.Stopwatch.GetTimestamp());
        var focusRequests = 0;
        var backgroundDialog = new AutomationSnapshot(RunState.Stopped, null, 0, 0, "", new DetectionResult(true, 1, Array.Empty<ProbeResult>(), ""),
            new TargetGeometry(new System.Drawing.Rectangle(0, 0, 1600, 900), new IntPtr(42), false, "Fixture", true));
        var focusConfig = new AppSettings { DryRun = false };
        Check(!GroupDialogReady(backgroundDialog, focusConfig, true, () => focusRequests++) && focusRequests == 1,
            "Returning background dialog requests focus before foreground gating");
        Check(!GroupDialogReady(backgroundDialog, focusConfig, true, () => focusRequests++) && focusRequests == 1,
            "Same dialog does not repeatedly steal focus");
        Check(GroupDialogReady(backgroundDialog with { Geometry = backgroundDialog.Geometry! with { IsForeground = true } }, focusConfig, true, () => focusRequests++),
            "Fresh focused observation releases dialog validation");
        GroupDialogReady(backgroundDialog with { Detection = null }, focusConfig, true, () => focusRequests++);
        Check(!GroupDialogReady(backgroundDialog, focusConfig, false, () => focusRequests++) && focusRequests == 1,
            "Disabled foreground option leaves background dialog waiting without focusing");
        GroupDialogReady(backgroundDialog with { Detection = null }, focusConfig, true, () => focusRequests++);
        Check(!GroupDialogReady(backgroundDialog, focusConfig, true, () => focusRequests++) && focusRequests == 2,
            "New dialog appearance requests focus again");
        automation.Stop("HUD: Beitritt erkannt", AutomationStopCause.Joined);
        UpdateRunDisplay(automation.Snapshot);
        Check(groupFollow.Enabled && groupFollow.Auto && operationState.Text.StartsWith("Wartet") && runReason.Text == "Warte auf Auswahldialog",
            "Auto shows waiting for the next dialog after HUD success instead of stopped");
        ToggleTeam(Enum.Parse<Team>(groupFollow.Team!));
        Check(!groupFollow.Enabled && automation.Snapshot.State == RunState.Stopped, "Waiting Auto team button stops instead of starting another run");
        StopAll("ESC fixture");
        Check(!operationState.Text.StartsWith("Wartet"), "Explicit stop removes the Auto waiting display");
        Check(!groupFollow.Apply(fixtureGeneration, selectedGroupSnapshot, System.Diagnostics.Stopwatch.GetTimestamp()), "Delayed online update cannot rearm after ESC");
        var oneTimeGeneration = groupFollow.Begin(fixtureOwner, false);
        followedSettings = new AppSettings(); followedMembership = fixtureOwner;
        groupFollow.Apply(oneTimeGeneration, selectedGroupSnapshot, System.Diagnostics.Stopwatch.GetTimestamp());
        automation.Stop("Spielfokus verloren", AutomationStopCause.Safety);
        TickGroupFollow();
        Check(!groupFollow.Enabled, "One-time join still ends after an attempt fails");
        var secondOwner = GroupRecoveryCodec.ImportAdmin(GroupRecoveryCodec.ExportAdmin(fixtureOwner));
        secondOwner.GroupId = GroupMembership.NewId(); secondOwner.Name = "Weitere Gruppe";
        groupProfile.Groups.Add(secondOwner); ReloadGroupPickers();
        var switchGeneration = groupFollow.Begin(fixtureOwner, true);
        shareGroupPicker.SelectedItem = secondOwner;
        Check(SelectedGroup == secondOwner && managementGroupPicker.SelectedItem == secondOwner && !groupFollow.Enabled && groupSync == null, "Manual group switch stops previous group and updates administration");
        Check(!groupFollow.Apply(switchGeneration, selectedGroupSnapshot ?? new GroupSnapshot(1, fixtureOwner.GroupId, fixtureOwner.Name, 3, 1, "Red", "Approved", "Owner", fixtureOwner.MemberId, new()), System.Diagnostics.Stopwatch.GetTimestamp()), "Old group cannot rearm after switching active group");
        managementGroupPicker.SelectedItem = fixtureOwner;
        Check(SelectedGroup == fixtureOwner && shareGroupPicker.SelectedItem == fixtureOwner && groupProfile.ActiveGroupKey == fixtureOwner.Key, "Management selection updates the active group in both modes");
        groupProfile = new(); selectedGroupSnapshot = null; ReloadGroupPickers(); operationTabs.SelectedIndex = 0;
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
        Check(pages.SelectedIndex == 2 && errorText.Text.Contains("andere Taste") && automation.Snapshot.State == RunState.Stopped, "Duplicate hotkeys block activation");
        await Settle();
        Check(errorText.Text.Contains("Blau und Rot verwenden F6") && keyBoxes[Team.Red].IsKeyboardFocused, "Conflicting key and teams are named, and the conflicting input is focused");
        SaveRender(Path.Combine(directory, "hotkey-validation.png"));
        keyBoxes[Team.Red].SelectedIndex = 6; ClearError();
        stopKeyBox.SelectedItem = keyBoxes[Team.Blue].SelectedItem;
        StartTeam(Team.Blue); await Settle();
        Check(errorText.Text.Contains("Stopp und Blau") && stopKeyBox.IsKeyboardFocused && automation.Snapshot.State == RunState.Stopped,
            "Stop/team conflicts block activation and focus the stop selector");
        stopKeyBox.SelectedItem = HotkeyChoice.All.First(key => key.Code == 0x23);
        keyBoxes[Team.Blue].SelectedItem = HotkeyChoice.All.First(key => key.Code == 0x1B);
        keyBoxes[Team.Red].SelectedItem = HotkeyChoice.All.First(key => key.Code == 0x2D);
        keyBoxes[Team.Green].SelectedItem = HotkeyChoice.All.First(key => key.Code == 0x21);
        var extendedKeys = ReadFields();
        Check(extendedKeys.StopHotkey == 0x23 && extendedKeys.Hotkeys[Team.Blue] == 0x1B && extendedKeys.Hotkeys[Team.Red] == 0x2D && extendedKeys.Hotkeys[Team.Green] == 0x21,
            "All selectors accept ESC and special keys independently");
        var originalSettings = settings; settings = extendedKeys;
        ApplyHotkeys(settings); UpdateOperationSummary();
        Check(teamKeyLabels[Team.Blue].Text == "ESC" && teamKeyLabels[Team.Red].Text == "EINFG"
            && groupStopButton.Content is StackPanel stopLabel && stopLabel.Children[1] is TextBlock stopText && stopText.Text.Contains("ENDE"),
            "Operation shows selected special keys and retains the stop icon");
        SaveRender(Path.Combine(directory, "extended-hotkeys.png"));
        settings = originalSettings;
        foreach (var team in Enum.GetValues<Team>()) keyBoxes[team].SelectedItem = HotkeyChoice.All.First(key => key.Code == settings.Hotkeys[team]);
        stopKeyBox.SelectedItem = HotkeyChoice.All.First(key => key.Code == settings.StopHotkey);
        ApplyHotkeys(settings); ClearError();

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
        Check(teamStateLabels[Team.Blue].Text == "Aktiv · wartet" && teamStateLabels.Where(pair => pair.Key != Team.Blue).All(pair => pair.Value.Text == "Aktivieren"), "Exactly the active team is marked while waiting, and stop is enabled");
        Check(teamNameLabels[Team.Blue].Text == "Stopp" && teamButtons[Team.Blue].Opacity == 1 && teamButtons.Where(pair => pair.Key != Team.Blue).All(pair => pair.Value.Opacity == 0.45), "Active team is stop action and other teams fade");
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
        ShowPage(3); dryRun.IsChecked = dryRun.IsChecked != true; await Settle();
        Check(automation.Snapshot.State == RunState.Stopped && teamStateLabels.Values.All(label => label.Text == "Aktivieren"), "Changing test mode in diagnosis stops the run and clears markers");
        dryRun.IsChecked = true;
        StartTeam(Team.Blue); await Settle();
        teamButtons[Team.Blue].RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        await Settle();
        Check(automation.Snapshot.State == RunState.Stopped && teamStateLabels.Values.All(label => label.Text == "Aktivieren") && teamButtons.Values.All(button => button.Opacity == 1), "Stopping restores all team buttons");
        SaveRender(Path.Combine(directory, "stopped.png"));

        // UI-only snapshot fixture: the controller stays stopped and sends no input.
        OnAutomation(new AutomationSnapshot(RunState.Clicking, Team.Green, 12, 61, "Testmodus: Klick simuliert", null, null));
        await Settle();
        Check(operationState.Text == "Klickt · Grün" && teamStateLabels[Team.Green].Text == "Aktiv · klickt", "Clicking state has clear phase, team and active controls");
        Check(teamNameLabels[Team.Green].Text == "Stopp", "Stop remains available during clicking");
        SaveRender(Path.Combine(directory, "clicking-fixture.png"));
        OnAutomation(new AutomationSnapshot(RunState.Stopped, Team.Green, 12, 61, "Team erfolgreich ausgewählt", null, null));
        await Settle();
        Check(teamButtons.Values.All(button => button.Opacity == 1) && teamNameLabels[Team.Green].Text == "Grün", "Successful selection restores all team buttons");
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
        File.WriteAllText(Path.Combine(directory, "checks.txt"), "PASS: startup routing, five areas, split navigation, single active group, group administration, both sizes, navigation, drawing, region drafts, validation, hotkeys, dirty state, preview lifecycle, diagnosis-only test mode, centered run and hotkeys, exclusive active-team markers, waiting/switching/stopping and UI-only clicking snapshot, conditional stop, reference checks and empty state. No mouse input sent; no profile saved. clicking-fixture.png uses a UI snapshot fixture while the controller is stopped.");
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
