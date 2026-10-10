using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using WardogsTeamselector.Core;
using WardogsTeamselector.Groups;

namespace WardogsTeamselector;

public sealed partial class MainWindow
{
    private readonly TabControl operationTabs = new() { Padding = new Thickness(20) };
    private readonly ComboBox groupPicker = new() { DisplayMemberPath = "Label", MinWidth = 260 };
    private readonly ComboBox shareGroupPicker = new() { DisplayMemberPath = "Label", MinWidth = 240 };
    private readonly ComboBox managementGroupPicker = new() { DisplayMemberPath = "Label", MinWidth = 260 };
    private readonly TextBlock managementGroupStatus = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 8) };
    private readonly CheckBox shareTeamWithGroup = new() { Content = "Mit Gruppe teilen", VerticalAlignment = VerticalAlignment.Center };
    private readonly ListBox groupMembers = new() { DisplayMemberPath = "Label", MinHeight = 100, MaxHeight = 220 };
    private readonly ListBox groupRequests = new() { DisplayMemberPath = "Label", MinHeight = 70, MaxHeight = 160 };
    private readonly TextBlock groupStatus = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 8) };
    private readonly CheckBox groupAuto = new() { Content = "Auto folgen · beim nächsten Teamauswahlbildschirm", Margin = new Thickness(0, 8, 0, 8) };
    private readonly TextBox groupService = new() { MinWidth = 260 };
    private readonly GroupApiClient groupApi = new();
    private readonly GroupFollowCoordinator groupFollow = new();
    private readonly DispatcherTimer groupTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly CancellationTokenSource groupLifetime = new();
    private GroupProfile groupProfile = new();
    private GroupSyncClient? groupSync;
    private GroupSnapshot? selectedGroupSnapshot;
    private GroupSnapshot? displayedGroupSnapshot;
    private GroupMembership? followedMembership;
    private AppSettings? followedSettings;
    private bool groupUiLoading, groupBusy, groupStorageFailed;
    private int stableGroupDialog;
    private long lastGroupObservation;
    private long nextGroupRecovery;
    private bool groupDialogFocusRequested;
    private CancellationTokenSource? groupJoinRefresh;
    private Button groupStopButton = null!;
    private Button groupJoinButton = null!;
    private readonly System.Collections.Generic.List<Button> groupOwnerButtons = new();
    private GroupMembership? SelectedGroup => groupPicker.SelectedItem as GroupMembership;

    private UIElement BuildGroupMode()
    {
        var body = new StackPanel();
        body.Children.Add(PageTitle("Gemeinsam einem Team beitreten", "Der Ersteller teilt ein Team. Bestätigte Mitglieder können einmal beitreten oder der Auswahl automatisch folgen."));
        body.Children.Add(groupPicker);
        AutomationProperties.SetName(groupPicker, "Gespeicherte Gruppen");
        groupPicker.SelectionChanged += async (_, _) =>
        {
            if (groupUiLoading) return;
            StopGroupFollow(); selectedGroupSnapshot = null; UpdateGroupControls();
            SynchronizeGroupPickers();
            groupProfile.ActiveGroupKey = SelectedGroup?.Key;
            try { SaveGroups(); }
            catch { groupStorageFailed = true; groupStatus.Text = "Gruppenspeicher nicht verfügbar · Auto wurde gestoppt."; UpdateGroupControls(); return; }
            if (!smokeMode) await RefreshSelectedGroupAsync();
        };
        body.Children.Add(groupStatus);
        var actions = new WrapPanel();
        groupJoinButton = GroupButton("Einmal beitreten", () => BeginGroupFollowAsync(false)); actions.Children.Add(groupJoinButton);
        actions.Children.Add(GroupButton("Aktualisieren", RefreshSelectedGroupAsync));
        body.Children.Add(actions); body.Children.Add(groupAuto);
        groupAuto.Checked += async (_, _) => { if (!groupUiLoading) await RunGroupUiAction(() => BeginGroupFollowAsync(true)); };
        groupAuto.Unchecked += (_, _) => { if (!groupUiLoading) StopGroupFollow(); };
        var manageButton = Button("Gruppen verwalten", () => ShowPage(4));
        manageButton.Margin = new Thickness(0, 8, 8, 8);
        body.Children.Add(manageButton);
        return Scroll(body);
    }
    private UIElement BuildGroupManagement()
    {
        var body = new StackPanel();
        body.Children.Add(PageTitle("Gruppenverwaltung", "Gruppen erstellen, Einladungen teilen und Mitglieder freigeben. Im Betrieb ist immer genau eine Gruppe aktiv."));
        body.Children.Add(managementGroupPicker);
        AutomationProperties.SetName(managementGroupPicker, "Aktive Gruppe verwalten");
        managementGroupPicker.SelectionChanged += (_, _) => SelectActiveGroup(managementGroupPicker);
        body.Children.Add(managementGroupStatus);
        body.Children.Add(GroupButton("Aktualisieren", RefreshSelectedGroupAsync));
        var manage = new WrapPanel { Margin = new Thickness(0, 12, 0, 8) };
        manage.Children.Add(GroupButton("Gruppe erstellen", CreateGroupAsync));
        manage.Children.Add(GroupButton("Gruppe beitreten", JoinGroupAsync));
        manage.Children.Add(GroupButton("Gruppe verlassen", LeaveGroupAsync));
        manage.Children.Add(Button("Aus Liste entfernen", RemoveLocalGroup));
        body.Children.Add(manage);
        var owner = new StackPanel();
        owner.Children.Add(Hint("Gruppenverwaltung · für Ersteller. Namen sind Anzeigenamen; die kurze Kennung unterscheidet gleichnamige Personen."));
        owner.Children.Add(Heading("Offene Beitrittsanfragen"));
        owner.Children.Add(groupRequests);
        groupRequests.SelectionChanged += (_, _) => { if (groupRequests.SelectedItem != null) groupMembers.SelectedItem = null; };
        groupMembers.SelectionChanged += (_, _) => { if (groupMembers.SelectedItem != null) groupRequests.SelectedItem = null; };
        var requestActions = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
        foreach (var (label, action) in new[] { ("Bestätigen", "approve"), ("Ablehnen", "reject") })
        {
            var button = GroupButton(label, () => MemberActionAsync(action)); requestActions.Children.Add(button); groupOwnerButtons.Add(button);
        }
        owner.Children.Add(requestActions);
        owner.Children.Add(Heading("Bestätigte Mitglieder"));
        owner.Children.Add(groupMembers);
        var memberActions = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
        var removeMember = GroupButton("Mitglied entfernen", () => MemberActionAsync("remove"));
        memberActions.Children.Add(removeMember); groupOwnerButtons.Add(removeMember);
        owner.Children.Add(memberActions);
        owner.Children.Add(new Separator { Background = BorderBrushColor, Margin = new Thickness(0, 16, 0, 0) });
        owner.Children.Add(Heading("Gruppenverwaltung"));
        var inviteActions = new WrapPanel();
        var copy = Button("Einladungslink kopieren", CopyGroupInvitation); inviteActions.Children.Add(copy); groupOwnerButtons.Add(copy);
        foreach (var (label, action) in new[] { ("Neuen Einladungslink erzeugen", "invite"), ("Auswahl zurücknehmen", "clear"), ("Gruppe löschen", "delete") })
        {
            var button = GroupButton(label, () => OwnerActionAsync(action)); inviteActions.Children.Add(button); groupOwnerButtons.Add(button);
        }
        owner.Children.Add(inviteActions);
        body.Children.Add(new Expander { Header = "Gruppe verwalten", Content = owner, IsExpanded = true });
        var admin = new StackPanel();
        admin.Children.Add(Hint("Das administrative Token bleibt verborgen. Ein privater Übertragungscode enthält ausschließlich die Rechte der ausgewählten Gruppe. Importieren teilt den Zugang; exklusiv übernehmen widerruft bisherige Adminzugänge."));
        var adminActions = new WrapPanel();
        var exportAdmin = Button("Adminzugang kopieren", ExportSelectedAdmin); adminActions.Children.Add(exportAdmin); groupOwnerButtons.Add(exportAdmin);
        adminActions.Children.Add(GroupButton("Adminzugang importieren", () => ImportSelectedAdminAsync(false)));
        adminActions.Children.Add(GroupButton("Adminzugang exklusiv übernehmen", () => ImportSelectedAdminAsync(true)));
        admin.Children.Add(adminActions);
        body.Children.Add(new Expander { Header = "Gruppe übertragen", Content = admin });
        var recovery = new WrapPanel();
        recovery.Children.Add(Button("Wiederherstellungscode exportieren", ExportGroups));
        recovery.Children.Add(Button("Wiederherstellungscode importieren", ImportGroups));
        recovery.Children.Add(GroupButton("Eigenen Zugangscode ersetzen", RotateGroupCredentialAsync));
        body.Children.Add(new Expander { Header = "PC-Wechsel und Wiederherstellung", Content = recovery });
        var service = new StackPanel();
        service.Children.Add(Hint("Adresse des Gruppendienstes. Einladungslinks enthalten die passende Adresse bereits. Eine eigene Bereitstellung benötigt ausschließlich Cloudflare Workers Free."));
        var deploymentGuide = new Hyperlink(new Run("Deployment-Anleitung im Repository-Wiki"))
        {
            NavigateUri = new Uri(ProjectUrl + "/blob/main/docs/wiki/Eigenen-Gruppendienst-deployen.md"),
            Foreground = BrushFrom(145, 200, 246)
        };
        deploymentGuide.RequestNavigate += (_, e) => { OpenProjectLink(e.Uri.AbsoluteUri); e.Handled = true; };
        var deploymentHelp = Hint("", 4);
        deploymentHelp.Margin = new Thickness(0, 4, 0, 12);
        deploymentHelp.Inlines.Add(deploymentGuide);
        service.Children.Add(deploymentHelp);
        service.Children.Add(groupService); AutomationProperties.SetName(groupService, "Gruppendienst-Adresse");
        var saveService = Button("Dienstadresse speichern", SaveGroupService);
        saveService.Margin = new Thickness(0, 8, 8, 8);
        service.Children.Add(saveService);
        body.Children.Add(new Expander { Header = "Gruppendienst einrichten", Content = service });
        return Scroll(body);
    }
    private void BuildManualGroupActions(StackPanel body)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new());
        row.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        shareTeamWithGroup.Margin = new Thickness(0, 0, 16, 0);
        shareTeamWithGroup.Checked += (_, _) => UpdateGroupControls();
        shareTeamWithGroup.Unchecked += (_, _) =>
        {
            if (groupBusy) StopGroupFollow();
            UpdateGroupControls();
        };
        row.Children.Add(shareTeamWithGroup);
        shareGroupPicker.IsEnabled = false;
        Grid.SetColumn(shareGroupPicker, 1); row.Children.Add(shareGroupPicker);
        shareGroupPicker.SelectionChanged += (_, _) => SelectActiveGroup(shareGroupPicker);
        AutomationProperties.SetName(shareGroupPicker, "Gruppe für geteilte Auswahl");
        var configureGroup = Button("Gruppen verwalten", () => ShowPage(4));
        configureGroup.Margin = new Thickness(8, 0, 0, 0);
        configureGroup.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(configureGroup, 2); row.Children.Add(configureGroup);
        body.Children.Add(row);
    }
    private async void ActivateTeam(Team team)
    {
        if (shareTeamWithGroup.IsChecked != true) { StartTeam(team); return; }
        if (groupBusy) return;
        await RunGroupUiAction(async () =>
        {
            try { await ShareAndJoinAsync(team); }
            catch (Exception ex) { if (!closing) ShowError(ex.Message); throw; }
        });
    }
    private Button GroupButton(string label, Func<Task> action) => Button(label, async () => await RunGroupUiAction(action));
    private async Task RunGroupUiAction(Func<Task> action)
    {
        if (groupBusy || closing) return;
        groupBusy = true; UpdateGroupControls();
        try { await action(); }
        catch (OperationCanceledException) { if (!closing) groupStatus.Text = "Aktion abgebrochen oder Dienst nicht rechtzeitig erreichbar."; }
        catch (GroupApiException ex) { groupStatus.Text = ex.Message; }
        catch (ArgumentException ex) { groupStatus.Text = ex.Message; }
        catch (Exception) { groupStatus.Text = "Gruppenaktion fehlgeschlagen. Verbindung und Gruppenspeicher prüfen."; }
        finally { groupBusy = false; if (!closing) UpdateGroupControls(); }
    }
    private void InitializeGroups()
    {
        GroupMember.TranslateDisplay = Localization.Text;
        if (!smokeMode)
        {
            try { groupProfile = GroupMembershipStore.Load(); }
            catch (Exception) { groupStorageFailed = true; groupStatus.Text = "Gruppenspeicher konnte nicht geladen werden. Datei sichern und Wiederherstellungscode importieren."; }
        }
        if (string.IsNullOrWhiteSpace(groupProfile.ServiceUrl)) groupProfile.ServiceUrl = GroupProfile.DefaultServiceUrl;
        groupService.Text = groupProfile.ServiceUrl;
        ReloadGroupPickers();
        groupTimer.Tick += (_, _) => { managementGroupStatus.Text = groupStatus.Text; TickGroupFollow(); }; groupTimer.Start();
    }
    private void SaveGroups()
    {
        if (smokeMode) return;
        if (groupStorageFailed) throw new InvalidOperationException("Gruppenspeicher nicht verfügbar.");
        GroupMembershipStore.Save(groupProfile);
    }
    private void ReloadGroupPickers()
    {
        var selected = SelectedGroup;
        groupUiLoading = true;
        try
        {
            groupPicker.ItemsSource = null; groupPicker.ItemsSource = groupProfile.Groups;
            groupPicker.SelectedItem = selected != null && groupProfile.Groups.Contains(selected) ? selected : groupProfile.Groups.FirstOrDefault(g => g.Key == groupProfile.ActiveGroupKey) ?? groupProfile.Groups.FirstOrDefault();
            shareGroupPicker.ItemsSource = null; shareGroupPicker.ItemsSource = groupProfile.Groups;
            managementGroupPicker.ItemsSource = null; managementGroupPicker.ItemsSource = groupProfile.Groups;
            shareGroupPicker.SelectedItem = groupPicker.SelectedItem;
            managementGroupPicker.SelectedItem = groupPicker.SelectedItem;
        }
        finally { groupUiLoading = false; }
        UpdateGroupControls();
    }
    private void SelectActiveGroup(ComboBox source)
    {
        if (groupUiLoading) return;
        groupPicker.SelectedItem = source.SelectedItem;
    }
    private void SynchronizeGroupPickers()
    {
        groupUiLoading = true;
        try { shareGroupPicker.SelectedItem = SelectedGroup; managementGroupPicker.SelectedItem = SelectedGroup; }
        finally { groupUiLoading = false; }
    }
    private void UpdateGroupControls()
    {
        if (groupJoinButton == null) return;
        var group = SelectedGroup; bool available = !groupBusy && !groupStorageFailed;
        groupJoinButton.IsEnabled = available && group?.Status == "Approved" && selectedGroupSnapshot?.Team != null;
        groupAuto.IsEnabled = available && group?.Status == "Approved";
        shareGroupPicker.IsEnabled = available && shareTeamWithGroup.IsChecked == true;
        foreach (var button in teamButtons.Values) button.IsEnabled = shareTeamWithGroup.IsChecked != true || !groupBusy;
        foreach (var button in groupOwnerButtons) button.IsEnabled = available && group?.Role == "Owner" && group.Status == "Approved";
        if (!ReferenceEquals(displayedGroupSnapshot, selectedGroupSnapshot))
        {
            displayedGroupSnapshot = selectedGroupSnapshot;
            groupMembers.ItemsSource = selectedGroupSnapshot?.Members.Where(x => x.Status == "Approved").ToList();
            groupRequests.ItemsSource = selectedGroupSnapshot?.Members.Where(x => x.Status == "Pending").ToList();
        }
        groupStopButton.IsEnabled = groupFollow.Enabled;
        groupStopButton.Visibility = groupFollow.Enabled ? Visibility.Visible : Visibility.Collapsed;
        if (group == null && !groupStorageFailed && string.IsNullOrEmpty(groupStatus.Text)) groupStatus.Text = "Noch keine Gruppe gespeichert. Gruppe erstellen oder Einladung einfügen.";
        managementGroupStatus.Text = groupStatus.Text;
    }
    private void SaveGroupService()
    {
        try { groupProfile.ServiceUrl = GroupServiceAddress.Normalize(groupService.Text); SaveGroups(); groupStatus.Text = "Gruppendienst-Adresse gespeichert."; }
        catch (ArgumentException ex) { groupStatus.Text = ex.Message; }
        catch (Exception) { groupStatus.Text = "Gruppendienst-Adresse konnte nicht gespeichert werden."; }
    }
    private async Task CreateGroupAsync()
    {
        var input = GroupPrompt("Gruppe erstellen", ("Gruppenname", ""), ("Dein Name", "")); if (input == null) return;
        var service = GroupServiceAddress.Normalize(groupService.Text);
        var group = new GroupMembership { ServiceUrl = service, GroupId = GroupMembership.NewId(), MemberId = GroupMembership.NewId(), Token = GroupMembership.NewToken(), InviteToken = GroupMembership.NewToken(), Name = input[0].Trim(), DisplayName = input[1].Trim(), Role = "Owner", Status = "Pending", RegistrationPending = true };
        group.Validate(); groupProfile.Groups.Add(group); groupProfile.ServiceUrl = service;
        // Persist credentials before requesting creation, so an interrupted response is recoverable.
        try { SaveGroups(); } catch { groupProfile.Groups.Remove(group); throw; }
        ReloadGroupPickers(); groupPicker.SelectedItem = group;
        await CompleteRegistrationAsync(group);
        groupStatus.Text = "Gruppe erstellt. Einladungslink kopieren und mit deinen Leuten teilen.";
    }
    private Task JoinGroupAsync() => JoinGroupAsync(null);
    private async Task JoinGroupAsync(string? invitationLink)
    {
        var linkedInvitation = invitationLink == null ? default : GroupServiceAddress.ParseInvitation(invitationLink);
        if (invitationLink != null)
        {
            var existing = groupProfile.Groups.FirstOrDefault(x => x.ServiceUrl == linkedInvitation.ServiceUrl && x.GroupId == linkedInvitation.GroupId);
            if (existing != null) { groupPicker.SelectedItem = existing; await RefreshSelectedGroupAsync(); return; }
        }
        var input = invitationLink == null
            ? GroupPrompt("Gruppe beitreten", ("Vollständiger Einladungslink", ""), ("Dein Name", ""))
            : GroupPrompt("Gruppe beitreten", $"{linkedInvitation.ServiceUrl}\n{linkedInvitation.GroupId}", ("Dein Name", ""));
        if (input == null) { groupStatus.Text = "Gruppenbeitritt abgebrochen. Es wurde keine Anfrage gesendet."; return; }
        var invitation = GroupServiceAddress.ParseInvitation(invitationLink ?? input[0]);
        if (groupProfile.Groups.Any(x => x.ServiceUrl == invitation.ServiceUrl && x.GroupId == invitation.GroupId)) throw new ArgumentException("Diese Gruppe ist bereits gespeichert. Aktualisieren oder die alte Mitgliedschaft aus der Liste entfernen.");
        var group = new GroupMembership { ServiceUrl = invitation.ServiceUrl, GroupId = invitation.GroupId, MemberId = GroupMembership.NewId(), Token = GroupMembership.NewToken(), InviteToken = invitation.InviteToken, Name = "Neue Gruppenanfrage", DisplayName = input[invitationLink == null ? 1 : 0].Trim(), RegistrationPending = true };
        group.Validate(); groupProfile.Groups.Add(group);
        try { SaveGroups(); } catch { groupProfile.Groups.Remove(group); throw; }
        ReloadGroupPickers(); groupPicker.SelectedItem = group;
        groupStatus.Text = "Beitrittsanfrage wird gesendet …";
        managementGroupStatus.Text = groupStatus.Text;
        await CompleteRegistrationAsync(group);
        groupStatus.Text = "Anfrage gesendet · wartet auf Bestätigung. „Aktualisieren“ prüft die Freigabe.";
    }
    private readonly System.Collections.Generic.Queue<string> invitationActivations = new();
    private bool receivingInvitation;
    public async void ReceiveInvitationActivation(string value)
    {
        if (closing) return;
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Show(); Activate();
        if (value.Length == 0) return;
        if (invitationActivations.Count >= 10) return;
        invitationActivations.Enqueue(value);
        if (receivingInvitation) return;
        receivingInvitation = true;
        try
        {
            while (invitationActivations.Count != 0 && !closing)
            {
                while (groupBusy && !closing) await Task.Delay(100);
                if (closing) return;
                var next = invitationActivations.Dequeue();
                ShowPage(4);
                groupStatus.Text = "Einladung empfangen. Bitte deinen Namen eingeben und den Beitritt bestätigen.";
                await RunGroupUiAction(() => JoinGroupAsync(GroupInvitationActivation.Parse(next)));
            }
        }
        finally { receivingInvitation = false; }
    }
    private async Task CompleteRegistrationAsync(GroupMembership group)
    {
        var snapshot = group.Role == "Owner" ? await groupApi.CreateAsync(group, groupLifetime.Token) : await groupApi.JoinAsync(group, group.InviteToken ?? "", groupLifetime.Token);
        group.Apply(snapshot); group.RegistrationPending = false; SaveGroups();
        if (SelectedGroup == group) selectedGroupSnapshot = snapshot;
        ReloadGroupPickers();
    }
    private async Task RefreshSelectedGroupAsync()
    {
        var group = SelectedGroup; if (group == null || closing || groupStorageFailed) return;
        try
        {
            if (group.PendingToken != null) await ResolvePendingCredentialAsync(group);
            if (group.RegistrationPending) await CompleteRegistrationAsync(group);
            else
            {
                var snapshot = await groupApi.GetAsync(group, groupLifetime.Token);
                if (closing || !groupProfile.Groups.Contains(group)) return;
                group.Apply(snapshot); SaveGroups();
                if (SelectedGroup == group) { selectedGroupSnapshot = snapshot; groupStatus.Text = DescribeGroup(snapshot); }
                ReloadGroupPickers();
            }
        }
        catch (GroupApiException ex)
        {
            if (ex.AccessRevoked) { group.Status = "Removed"; SaveGroups(); if (followedMembership == group) StopGroupFollow(); ReloadGroupPickers(); }
            if (SelectedGroup == group) groupStatus.Text = ex.Message;
        }
        catch (Exception) { if (SelectedGroup == group) groupStatus.Text = "Gruppendienst nicht erreichbar. Mit „Aktualisieren“ erneut versuchen."; }
    }
    private static string DescribeGroup(GroupSnapshot snapshot) => $"Gruppe: {snapshot.Name} · {GroupMember.StatusLabel(snapshot.YourStatus)}\n" + (snapshot.YourStatus == "Approved" ? snapshot.Team == null ? "Noch keine Teamauswahl veröffentlicht." : $"Auswahl: {TeamName(Enum.Parse<Team>(snapshot.Team))}" : "Der Ersteller muss deine Anfrage bestätigen.");
    private async Task BeginGroupFollowAsync(bool auto)
    {
        if (startupSettingsError != null) throw new ArgumentException(startupSettingsError);
        var group = SelectedGroup ?? throw new ArgumentException("Zuerst eine Gruppe wählen.");
        if (group.Status != "Approved") throw new ArgumentException("Die Mitgliedschaft muss zuerst bestätigt werden.");
        var config = ReadFields(); config.Validate();
        StopAll("Gruppenbeitritt vorbereiten");
        var generation = groupFollow.Begin(group, auto); followedMembership = group; followedSettings = config;
        automation.Stop("Warte auf aktuelle Gruppenauswahl", AutomationStopCause.GroupUpdate);
        groupUiLoading = true; groupAuto.IsChecked = auto; groupUiLoading = false;
        SaveCheckboxPreferences();
        UpdateGroupControls();
        try
        {
            // Auto's sync worker performs the initial authorization and retries
            // temporary failures. A failed first HTTP request must not turn Auto off.
            if (!auto)
            {
                var snapshot = await groupApi.GetAsync(group, groupLifetime.Token);
                if (!groupFollow.Enabled || generation != groupFollow.Generation || closing) return;
                if (snapshot.YourStatus != "Approved") throw new GroupApiException("Mitgliedschaft nicht bestätigt.", "membership_revoked");
                ApplyFollowedGroup(generation, group, snapshot);
                if (snapshot.Team == null) { StopGroupFollow(); groupStatus.Text = "Noch keine Auswahl veröffentlicht."; return; }
            }
            if (!groupFollow.Enabled || generation != groupFollow.Generation || closing) return;
            if (auto) automation.Observe(config);
            groupSync = new(groupApi, group);
            groupSync.AccessRevoked += ex => Dispatcher.BeginInvoke(() =>
            {
                if (closing || generation != groupFollow.Generation) return;
                group.Status = "Removed"; StopAll("Mitgliedschaft beendet"); SaveGroups(); ReloadGroupPickers(); groupStatus.Text = ex.Message;
            });
            groupSync.Updated += update => Dispatcher.BeginInvoke(() => { if (!closing) ApplyFollowedGroup(generation, group, update); });
            groupSync.ConnectionChanged += (online, reason) => Dispatcher.BeginInvoke(() =>
            {
                ApplyGroupConnectionState(generation, online, reason);
            });
            groupSync.Start();
            if (!smokeMode && config.FocusGameOnTeamActivation) screen.TryBringGameToForeground(config);
        }
        catch { if (generation == groupFollow.Generation) StopAll("Gruppenbeitritt fehlgeschlagen"); throw; }
    }
    private void ApplyGroupConnectionState(long generation, bool online, string reason)
    {
        if (closing || generation != groupFollow.Generation || !groupFollow.Enabled) return;
        if (!online) { groupFollow.Disconnected(generation); automation.Observe(groupFollow.Auto ? followedSettings : null); automation.Stop("Gruppenverbindung pausiert", AutomationStopCause.GroupUpdate); stableGroupDialog = 0; }
        groupStatus.Text = selectedGroupSnapshot == null ? reason : DescribeGroup(selectedGroupSnapshot) + "\n" + reason;
        UpdateGroupControls();
    }
    private void ApplyFollowedGroup(long generation, GroupMembership group, GroupSnapshot snapshot)
    {
        if (generation != groupFollow.Generation || !groupFollow.Enabled || closing) return;
        if (snapshot.Revision < groupFollow.Revision) return;
        var changed = groupFollow.Apply(generation, snapshot, snapshot.ReceivedAt == 0 ? Stopwatch.GetTimestamp() : snapshot.ReceivedAt);
        group.Apply(snapshot);
        try { SaveGroups(); } catch { StopAll("Gruppenspeicher nicht verfügbar"); groupStatus.Text = "Gruppenspeicher nicht verfügbar · Auto wurde gestoppt."; return; }
        selectedGroupSnapshot = snapshot;
        groupStatus.Text = DescribeGroup(snapshot) + "\nOnline · Zustandsprüfung alle 15 Sekunden";
        if (snapshot.YourStatus != "Approved") { StopAll("Mitgliedschaft beendet"); ReloadGroupPickers(); return; }
        if (changed) { automation.Stop("Gruppenauswahl aktualisiert", AutomationStopCause.GroupUpdate); stableGroupDialog = 0; lastGroupObservation = 0; }
        automation.RenewOnlineLease(groupFollow.OnlineLeaseDeadline);
        if (followedSettings != null) automation.Observe(groupFollow.Auto || snapshot.Team != null ? followedSettings : null);
        UpdateGroupControls();
    }
    private void TickGroupFollow()
    {
        if (!groupFollow.Enabled || followedSettings == null || closing) return;
        var current = automation.Snapshot;
        if (current.State == RunState.Stopped && current.StopCause is AutomationStopCause.Manual or AutomationStopCause.Safety)
        {
            if (!groupFollow.Auto) { StopGroupFollow(); return; }
            // Explicit stop/ESC and disabling Auto already end the group session.
            // A stopped individual attempt only returns persistent Auto to observation.
            if (Stopwatch.GetTimestamp() < nextGroupRecovery) return;
            nextGroupRecovery = Stopwatch.GetTimestamp() + Stopwatch.Frequency;
            automation.Stop("Warte auf Auswahldialog", AutomationStopCause.GroupUpdate);
            automation.Observe(followedSettings);
            stableGroupDialog = 0; lastGroupObservation = 0; groupDialogFocusRequested = false;
            current = automation.Snapshot;
        }
        if (!groupFollow.HasCurrentState(groupFollow.Generation, Stopwatch.GetTimestamp()))
        {
            if (!groupFollow.Online || groupFollow.Team != null) { if (!groupFollow.Auto) automation.Observe(null); if (current.State != RunState.Stopped) automation.Stop("Gruppenstatus nicht aktuell", AutomationStopCause.GroupUpdate); }
            stableGroupDialog = 0; return;
        }
        if (current.State != RunState.Stopped) return;
        if (current.StopCause == AutomationStopCause.Joined && !groupFollow.Auto) { StopGroupFollow(); return; }
        UpdateRunDisplay(current);
        if (groupFollow.JoinRefreshPending) return;
        if (!GroupDialogReady(current, followedSettings, focusGame.IsChecked == true,
            () => { if (!smokeMode) screen.TryBringGameToForeground(followedSettings); })) { stableGroupDialog = 0; return; }
        // Distinct sparse observations, followed by the controller's own three-frame validation.
        var timestamp = Stopwatch.GetTimestamp();
        if (lastGroupObservation != 0 && Stopwatch.GetElapsedTime(lastGroupObservation, timestamp).TotalMilliseconds < 240) return;
        lastGroupObservation = timestamp;
        if (++stableGroupDialog < 3) return;
        stableGroupDialog = 0;
        _ = RefreshAndStartGroupAttemptAsync();
    }
    private bool GroupDialogReady(AutomationSnapshot current, AppSettings config, bool focusOnActivation, Action requestFocus)
    {
        if (current.Detection?.IsMatch != true || current.JoinedDetection?.IsMatch == true || current.Geometry == null
            || (!config.DryRun && !current.Geometry.IsCalibrated))
        {
            groupDialogFocusRequested = false;
            return false;
        }
        // Recognize a visible background dialog before requiring foreground.
        // Request focus once per appearance, then wait for a fresh focused capture.
        if (!groupDialogFocusRequested)
        {
            groupDialogFocusRequested = true;
            if (focusOnActivation) requestFocus();
        }
        return current.Geometry.IsForeground;
    }
    private async Task RefreshAndStartGroupAttemptAsync()
    {
        var generation = groupFollow.Generation;
        var group = followedMembership;
        var config = followedSettings;
        if (group == null || config == null) return;
        using var refresh = CancellationTokenSource.CreateLinkedTokenSource(groupLifetime.Token);
        groupJoinRefresh = refresh;
        try
        {
            var snapshot = await groupFollow.RefreshForJoinAsync(generation, token => groupApi.GetAsync(group, token), refresh.Token);
            if (snapshot == null || closing || generation != groupFollow.Generation || followedMembership != group) return;
            ApplyFollowedGroup(generation, group, snapshot);
            if (!groupFollow.CanRun(generation, Stopwatch.GetTimestamp()) || !Enum.TryParse<Team>(groupFollow.Team, out var team)) return;
            var current = automation.Snapshot;
            // A stop, group switch, focus loss or vanished dialog while awaiting HTTP must not start input.
            if (current.State != RunState.Stopped || current.StopCause is AutomationStopCause.Manual or AutomationStopCause.Safety
                || current.Detection?.IsMatch != true || current.JoinedDetection?.IsMatch == true
                || current.Geometry?.IsForeground != true || (!config.DryRun && !current.Geometry.IsCalibrated)) return;
            automation.Start(team, config, groupFollow.OnlineLeaseDeadline);
        }
        catch (OperationCanceledException) when (refresh.IsCancellationRequested) { }
        catch (GroupApiException ex) when (ex.AccessRevoked)
        {
            if (!closing && generation == groupFollow.Generation) { StopAll("Mitgliedschaft beendet"); groupStatus.Text = ex.Message; }
        }
        catch (Exception)
        {
            if (!closing && generation == groupFollow.Generation)
            {
                automation.Stop("Warte auf aktuelle Gruppenauswahl", AutomationStopCause.GroupUpdate);
                groupStatus.Text = "Gruppendienst nicht erreichbar. Mit „Aktualisieren“ erneut versuchen.";
            }
        }
        finally { if (groupJoinRefresh == refresh) groupJoinRefresh = null; }
    }
    private async Task ShareAndJoinAsync(Team team)
    {
        var group = SelectedGroup ?? throw new ArgumentException("Eine eigene Gruppe wählen.");
        if (groupStorageFailed) throw new ArgumentException("Gruppenspeicher nicht verfügbar · Auto wurde gestoppt.");
        if (group.Role != "Owner" || group.Status != "Approved" || group.RegistrationPending) throw new ArgumentException("Eine eigene Gruppe wählen.");
        var config = ReadFields(); config.Validate();
        StopAll("Gruppenauswahl veröffentlichen");
        var generation = groupFollow.Generation;
        try
        {
            var snapshot = await groupApi.ActionAsync(group, "publish", new { operationId = GroupMembership.NewId(), team = team.ToString() }, groupLifetime.Token);
            group.Apply(snapshot); SaveGroups(); ReloadGroupPickers();
            if (closing || groupFollow.Generation != generation) return;
            StartTeam(team); groupStatus.Text = "Auswahl online geteilt; lokaler Beitritt gestartet.";
        }
        catch
        {
            if (!closing && groupFollow.Generation == generation && LocalizedMessageBox.Show(this, "Auswahl konnte nicht bestätigt werden. Trotzdem nur lokal beitreten?", "Auswahl nicht geteilt", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) StartTeam(team);
            throw;
        }
    }
    private async Task MemberActionAsync(string action)
    {
        var group = SelectedGroup ?? throw new ArgumentException("Gruppe wählen.");
        var member = (groupRequests.SelectedItem ?? groupMembers.SelectedItem) as GroupMember ?? throw new ArgumentException("Anfrage oder Mitglied auswählen.");
        if (member.Role == "Owner") throw new ArgumentException("Der Ersteller kann nicht entfernt werden.");
        if (action == "remove" && LocalizedMessageBox.Show(this, $"{member.DisplayName} aus der Gruppe entfernen?", "Mitglied entfernen", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        var snapshot = await groupApi.ActionAsync(group, action, new { operationId = GroupMembership.NewId(), memberId = member.Id }, groupLifetime.Token);
        AcceptManagedSnapshot(group, snapshot);
    }
    private async Task OwnerActionAsync(string action)
    {
        var group = SelectedGroup ?? throw new ArgumentException("Gruppe wählen.");
        if (action == "delete" && LocalizedMessageBox.Show(this, "Gruppe für alle Mitglieder endgültig löschen?", "Gruppe löschen", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        if (action == "invite")
        {
            var invite = GroupMembership.NewToken();
            // Preserve a new invitation locally before the server switches its hash.
            group.InviteToken = invite; SaveGroups();
            AcceptManagedSnapshot(group, await groupApi.ActionAsync(group, action, new { operationId = GroupMembership.NewId(), inviteToken = invite }, groupLifetime.Token));
            groupStatus.Text = "Neuer Einladungslink erzeugt. Der alte Link ist widerrufen.";
        }
        else AcceptManagedSnapshot(group, await groupApi.ActionAsync(group, action, new { operationId = GroupMembership.NewId() }, groupLifetime.Token));
    }
    private void AcceptManagedSnapshot(GroupMembership group, GroupSnapshot snapshot)
    {
        group.Apply(snapshot); SaveGroups();
        if (SelectedGroup == group) { selectedGroupSnapshot = snapshot; groupStatus.Text = DescribeGroup(snapshot); }
        if (followedMembership == group && groupFollow.Enabled) ApplyFollowedGroup(groupFollow.Generation, group, snapshot);
        ReloadGroupPickers();
    }
    private async Task LeaveGroupAsync()
    {
        var group = SelectedGroup ?? throw new ArgumentException("Gruppe wählen.");
        if (LocalizedMessageBox.Show(this, "Die Gruppe verlassen? Erneuter Beitritt benötigt eine neue Bestätigung.", "Gruppe verlassen", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        AcceptManagedSnapshot(group, await groupApi.ActionAsync(group, "leave", new { operationId = GroupMembership.NewId() }, groupLifetime.Token));
    }
    private void RemoveLocalGroup()
    {
        var group = SelectedGroup; if (group == null || groupBusy) return;
        if (LocalizedMessageBox.Show(this, "Lokale Zugangsdaten entfernen? Dies verlässt oder löscht die Online-Gruppe nicht. Ohne Wiederherstellungscode gehen deine Rechte auf diesem PC verloren.", "Aus Liste entfernen", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { StopAll("Gruppe aus lokaler Liste entfernt"); groupProfile.Groups.Remove(group); SaveGroups(); selectedGroupSnapshot = null; groupStatus.Text = ""; ReloadGroupPickers(); }
        catch (Exception) { groupStatus.Text = "Lokale Gruppenliste konnte nicht gespeichert werden."; }
    }
    private void CopyGroupInvitation()
    {
        var group = SelectedGroup;
        if (group?.Role != "Owner" || group.InviteToken == null) return;
        try { Clipboard.SetText(group.InvitationLink); groupStatus.Text = "Einladungslink kopiert."; }
        catch (Exception) { groupStatus.Text = "Zwischenablage momentan nicht verfügbar."; }
    }
    private void ExportSelectedAdmin()
    {
        var group = SelectedGroup; if (group == null || groupBusy) return;
        try
        {
            Clipboard.SetText(GroupRecoveryCodec.ExportAdmin(group));
            groupStatus.Text = "Privater Admin-Übertragungscode kopiert. Nur an die gewünschte Person oder Instanz weitergeben.";
        }
        catch { groupStatus.Text = "Adminzugang konnte nicht kopiert werden. Mitgliedschaft und Zwischenablage prüfen."; }
        UpdateGroupControls();
    }
    private async Task ImportSelectedAdminAsync(bool exclusive)
    {
        var code = AdminSecretPrompt(); if (code == null) return;
        var imported = GroupRecoveryCodec.ImportAdmin(code.Trim());
        var snapshot = await groupApi.GetAsync(imported, groupLifetime.Token);
        if (snapshot.YourRole != "Owner" || snapshot.YourStatus != "Approved") throw new ArgumentException("Nur einen bestätigten und aktuellen Adminzugang übertragen.");
        imported.Apply(snapshot);
        if (exclusive && LocalizedMessageBox.Show(this, "Adminzugang exklusiv übernehmen? Alle bisherigen Instanzen und Übertragungscodes verlieren ihre Adminrechte für diese Gruppe.", "Gruppe übertragen", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        var previous = groupProfile.Groups.FirstOrDefault(g => g.Key == imported.Key);
        if (!exclusive && previous != null && LocalizedMessageBox.Show(this, "Den vorhandenen lokalen Zugang dieser Gruppe durch den Adminzugang ersetzen?", "Gruppe übertragen", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        StopAll("Adminzugang importieren");
        if (previous != null) groupProfile.Groups.Remove(previous);
        groupProfile.Groups.Add(imported);
        try { SaveGroups(); }
        catch { groupProfile.Groups.Remove(imported); if (previous != null) groupProfile.Groups.Add(previous); throw; }
        ReloadGroupPickers(); groupPicker.SelectedItem = imported;
        if (exclusive)
        {
            imported.PendingToken = GroupMembership.NewToken(); imported.PendingCredentialOperation = GroupMembership.NewId();
            SaveGroups(); await ResolvePendingCredentialAsync(imported);
        }
        else AcceptManagedSnapshot(imported, snapshot);
        groupStatus.Text = exclusive ? "Adminzugang übernommen. Bisherige Adminzugänge sind widerrufen. Auto ist aus." : "Adminzugang importiert. Weitere Instanzen behalten ihren Zugang. Auto ist aus.";
    }
    private string? AdminSecretPrompt()
    {
        var dialog = new Window { Owner = this, Title = "Gruppe übertragen", Width = 580, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize, Background = Background, Foreground = Foreground, FontFamily = FontFamily, FontSize = FontSize };
        dialog.Resources.MergedDictionaries.Add(CreateTheme());
        var body = new StackPanel { Margin = new Thickness(24) };
        body.Children.Add(Hint("Privaten Admin-Übertragungscode einfügen. Der Code wird nicht angezeigt."));
        var secret = new PasswordBox { MaxLength = 16000, MinHeight = 40, Background = Background, Foreground = Foreground, Padding = new Thickness(8) };
        AutomationProperties.SetName(secret, "Privater Admin-Übertragungscode"); body.Children.Add(secret);
        var row = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        var accept = Button("Bestätigen", () => dialog.DialogResult = true); accept.IsDefault = true; row.Children.Add(accept);
        var cancel = Button("Abbrechen", () => dialog.DialogResult = false); cancel.IsCancel = true; row.Children.Add(cancel);
        body.Children.Add(row); dialog.Content = body; dialog.Loaded += (_, _) => { LocalizeInterface(dialog); secret.Focus(); };
        if (dialog.ShowDialog() != true) return null;
        var result = secret.Password; secret.Clear(); return result;
    }
    private void ExportGroups()
    {
        if (LocalizedMessageBox.Show(this, "Der Wiederherstellungscode enthält alle Gruppenrechte, einschließlich Erstellerrechten. Bewahre ihn privat auf. In die Zwischenablage kopieren?", "Geheimer Wiederherstellungscode", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { Clipboard.SetText(GroupMembershipStore.Export(groupProfile)); groupStatus.Text = "Geheimer Wiederherstellungscode kopiert. Privat sichern und Zwischenablage anschließend leeren."; }
        catch (Exception) { groupStatus.Text = "Wiederherstellungscode konnte nicht exportiert werden."; }
    }
    private void ImportGroups()
    {
        var input = GroupPrompt("Gruppen wiederherstellen", ("Geheimer Wiederherstellungscode", "")); if (input == null) return;
        try
        {
            var imported = GroupMembershipStore.Import(input[0]);
            if (groupStorageFailed || groupProfile.Groups.Count > 0)
                if (LocalizedMessageBox.Show(this, "Die lokale Gruppenliste durch diesen Wiederherstellungscode ersetzen?", "Gruppen wiederherstellen", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            StopAll("Gruppen wiederherstellen");
            if (!smokeMode) GroupMembershipStore.Save(imported);
            groupProfile = imported; groupStorageFailed = false; groupService.Text = imported.ServiceUrl; selectedGroupSnapshot = null; ReloadGroupPickers();
            groupStatus.Text = "Gruppen wiederhergestellt. Auto ist aus. Mit „Aktualisieren“ die Mitgliedschaft prüfen.";
        }
        catch (Exception) { groupStatus.Text = "Ungültiger Wiederherstellungscode oder Gruppenspeicher nicht verfügbar."; }
    }
    private async Task RotateGroupCredentialAsync()
    {
        var group = SelectedGroup ?? throw new ArgumentException("Gruppe wählen.");
        if (LocalizedMessageBox.Show(this, "Alte Wiederherstellungscodes für diese Mitgliedschaft ungültig machen? Danach einen neuen Code exportieren.", "Zugangscode ersetzen", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        StopAll("Zugangscode ersetzen");
        group.PendingToken ??= GroupMembership.NewToken();
        group.PendingCredentialOperation ??= GroupMembership.NewId();
        SaveGroups();
        await ResolvePendingCredentialAsync(group);
        groupStatus.Text = "Zugangscode ersetzt. Jetzt einen neuen Wiederherstellungscode exportieren.";
    }
    private async Task ResolvePendingCredentialAsync(GroupMembership group)
    {
        var replacement = group.PendingToken; if (replacement == null) return;
        var old = group.Token; GroupSnapshot snapshot;
        group.Token = replacement;
        try { snapshot = await groupApi.GetAsync(group, groupLifetime.Token); }
        catch (GroupApiException ex) when (ex.Code == "unauthorized")
        {
            group.Token = old;
            snapshot = await groupApi.ActionAsync(group, "credential", new { operationId = group.PendingCredentialOperation ?? GroupMembership.NewId(), newToken = replacement }, groupLifetime.Token);
        }
        catch { group.Token = old; throw; }
        group.Token = replacement; group.PendingToken = null; group.PendingCredentialOperation = null;
        AcceptManagedSnapshot(group, snapshot);
    }
    private void StopAll(string reason)
    {
        StopGroupFollow(); automation.Stop(reason);
    }
    private void StopGroupFollow()
    {
        groupJoinRefresh?.Cancel(); groupJoinRefresh = null;
        if (groupFollow.Enabled && automation.Snapshot.State != RunState.Stopped) automation.Stop("Gruppenmodus beendet", AutomationStopCause.GroupUpdate);
        groupFollow.Stop(); groupSync?.Dispose(); groupSync = null; followedMembership = null; followedSettings = null;
        stableGroupDialog = 0; lastGroupObservation = 0; nextGroupRecovery = 0; groupDialogFocusRequested = false; automation.Observe(null);
        groupUiLoading = true; groupAuto.IsChecked = false; groupUiLoading = false;
        SaveCheckboxPreferences();
        if (groupStopButton != null) { UpdateGroupControls(); UpdateRunDisplay(automation.Snapshot); }
    }
    private void DisposeGroups()
    {
        groupLifetime.Cancel(); groupTimer.Stop(); StopGroupFollow(); groupApi.Dispose();
    }
    private string[]? GroupPrompt(string title, params (string Label, string Initial)[] values)
        => GroupPrompt(title, null, values);
    private string[]? GroupPrompt(string title, string? description, params (string Label, string Initial)[] values)
    {
        var dialog = new Window { Owner = this, Title = title, Width = 580, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize, Background = Background, Foreground = Foreground, FontFamily = FontFamily, FontSize = FontSize, MaxHeight = 600 };
        dialog.Resources.MergedDictionaries.Add(CreateTheme());
        var body = new StackPanel { Margin = new Thickness(24) }; var inputs = new System.Collections.Generic.List<TextBox>();
        if (description != null) body.Children.Add(new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap });
        foreach (var field in values)
        {
            body.Children.Add(new TextBlock { Text = field.Label, Margin = new Thickness(0, 8, 0, 4) });
            var box = new TextBox { Text = field.Initial, MinWidth = 480, Margin = new Thickness(0, 0, 0, 8) }; AutomationProperties.SetName(box, field.Label); inputs.Add(box); body.Children.Add(box);
        }
        var actions = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        var accept = Button("Bestätigen", () => dialog.DialogResult = true); accept.IsDefault = true; actions.Children.Add(accept);
        var cancel = Button("Abbrechen", () => dialog.DialogResult = false); cancel.IsCancel = true; actions.Children.Add(cancel);
        body.Children.Add(actions); dialog.Content = body;
        dialog.Loaded += (_, _) => { LocalizeInterface(dialog); inputs[0].Focus(); };
        return dialog.ShowDialog() == true ? inputs.Select(x => x.Text).ToArray() : null;
    }
}
