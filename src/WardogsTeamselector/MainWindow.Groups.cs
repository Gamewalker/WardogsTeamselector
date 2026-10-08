using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using WardogsTeamselector.Core;
using WardogsTeamselector.Groups;

namespace WardogsTeamselector;

public sealed partial class MainWindow
{
    private readonly TabControl operationTabs = new();
    private readonly ComboBox groupPicker = new() { DisplayMemberPath = "Label", MinWidth = 260 };
    private readonly ComboBox shareGroupPicker = new() { DisplayMemberPath = "Label", MinWidth = 240 };
    private readonly ComboBox sharedTeamPicker = new() { MinWidth = 100 };
    private readonly ListBox groupMembers = new() { DisplayMemberPath = "Label", MinHeight = 100, MaxHeight = 220 };
    private readonly ListBox groupRequests = new() { DisplayMemberPath = "Label", MinHeight = 70, MaxHeight = 160 };
    private readonly TextBlock groupStatus = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 8) };
    private readonly CheckBox groupAuto = new() { Content = "Auto folgen · beim nächsten Teamauswahlbildschirm" };
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
    private Button groupStopButton = null!;
    private Button groupJoinButton = null!, shareButton = null!;
    private readonly System.Collections.Generic.List<Button> groupOwnerButtons = new();
    private GroupMembership? SelectedGroup => groupPicker.SelectedItem as GroupMembership;

    private UIElement BuildGroupMode()
    {
        var body = new StackPanel();
        body.Children.Add(Heading("Gemeinsam einem Team beitreten"));
        body.Children.Add(Hint("Der Ersteller teilt ein Team. Bestätigte Mitglieder können einmal beitreten oder der Auswahl automatisch folgen."));
        body.Children.Add(groupPicker);
        AutomationProperties.SetName(groupPicker, "Gespeicherte Gruppen");
        groupPicker.SelectionChanged += async (_, _) =>
        {
            if (groupUiLoading) return;
            StopGroupFollow(); selectedGroupSnapshot = null; UpdateGroupControls();
            if (!smokeMode) await RefreshSelectedGroupAsync();
        };
        body.Children.Add(groupStatus);
        var actions = new WrapPanel();
        groupJoinButton = GroupButton("Einmal beitreten", () => BeginGroupFollowAsync(false)); actions.Children.Add(groupJoinButton);
        actions.Children.Add(GroupButton("Aktualisieren", RefreshSelectedGroupAsync));
        body.Children.Add(actions); body.Children.Add(groupAuto);
        groupAuto.Checked += async (_, _) => { if (!groupUiLoading) await RunGroupUiAction(() => BeginGroupFollowAsync(true)); };
        groupAuto.Unchecked += (_, _) => { if (!groupUiLoading) StopGroupFollow(); };
        var manage = new WrapPanel { Margin = new Thickness(0, 12, 0, 8) };
        manage.Children.Add(GroupButton("Gruppe erstellen", CreateGroupAsync));
        manage.Children.Add(GroupButton("Gruppe beitreten", JoinGroupAsync));
        manage.Children.Add(GroupButton("Gruppe verlassen", LeaveGroupAsync));
        manage.Children.Add(Button("Aus Liste entfernen", RemoveLocalGroup));
        body.Children.Add(manage);
        var owner = new StackPanel();
        owner.Children.Add(Hint("Gruppenverwaltung · für Ersteller. Namen sind Anzeigenamen; die kurze Kennung unterscheidet gleichnamige Personen."));
        owner.Children.Add(new TextBlock { Text = "Offene Beitrittsanfragen", Margin = new Thickness(0, 8, 0, 4) });
        owner.Children.Add(groupRequests);
        owner.Children.Add(new TextBlock { Text = "Bestätigte Mitglieder", Margin = new Thickness(0, 8, 0, 4) });
        owner.Children.Add(groupMembers);
        groupRequests.SelectionChanged += (_, _) => { if (groupRequests.SelectedItem != null) groupMembers.SelectedItem = null; };
        groupMembers.SelectionChanged += (_, _) => { if (groupMembers.SelectedItem != null) groupRequests.SelectedItem = null; };
        var memberActions = new WrapPanel();
        foreach (var (label, action) in new[] { ("Bestätigen", "approve"), ("Ablehnen", "reject"), ("Mitglied entfernen", "remove") })
        {
            var button = GroupButton(label, () => MemberActionAsync(action)); memberActions.Children.Add(button); groupOwnerButtons.Add(button);
        }
        owner.Children.Add(memberActions);
        var inviteActions = new WrapPanel();
        var copy = Button("Einladungslink kopieren", CopyGroupInvitation); inviteActions.Children.Add(copy); groupOwnerButtons.Add(copy);
        foreach (var (label, action) in new[] { ("Neuen Einladungslink erzeugen", "invite"), ("Auswahl zurücknehmen", "clear"), ("Gruppe löschen", "delete") })
        {
            var button = GroupButton(label, () => OwnerActionAsync(action)); inviteActions.Children.Add(button); groupOwnerButtons.Add(button);
        }
        owner.Children.Add(inviteActions);
        body.Children.Add(new Expander { Header = "Gruppe verwalten", Content = owner, IsExpanded = true });
        var recovery = new WrapPanel();
        recovery.Children.Add(Button("Wiederherstellungscode exportieren", ExportGroups));
        recovery.Children.Add(Button("Wiederherstellungscode importieren", ImportGroups));
        recovery.Children.Add(GroupButton("Eigenen Zugangscode ersetzen", RotateGroupCredentialAsync));
        body.Children.Add(new Expander { Header = "PC-Wechsel und Wiederherstellung", Content = recovery });
        var service = new StackPanel();
        service.Children.Add(Hint("Adresse des Gruppendienstes. Einladungslinks enthalten die passende Adresse bereits. Eine eigene Bereitstellung benötigt ausschließlich Cloudflare Workers Free."));
        service.Children.Add(groupService); AutomationProperties.SetName(groupService, "Gruppendienst-Adresse");
        service.Children.Add(Button("Dienstadresse speichern", SaveGroupService));
        body.Children.Add(new Expander { Header = "Gruppendienst einrichten", Content = service });
        return Scroll(body);
    }
    private void BuildManualGroupActions(StackPanel body)
    {
        var section = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
        section.Children.Add(Hint("Als Ersteller: Team mit einer eigenen Gruppe teilen und anschließend lokal beitreten."));
        section.Children.Add(shareGroupPicker);
        foreach (var team in Enum.GetValues<Team>()) sharedTeamPicker.Items.Add(new ComboBoxItem { Content = TeamName(team), Tag = team });
        sharedTeamPicker.SelectedIndex = 0;
        AutomationProperties.SetName(shareGroupPicker, "Gruppe für geteilte Auswahl");
        AutomationProperties.SetName(sharedTeamPicker, "Team zum Teilen");
        var row = new WrapPanel(); row.Children.Add(sharedTeamPicker);
        shareButton = GroupButton("Auswahl mit Gruppe teilen & beitreten", ShareAndJoinAsync); row.Children.Add(shareButton);
        section.Children.Add(row); body.Children.Add(section);
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
        groupService.Text = groupProfile.ServiceUrl;
        ReloadGroupPickers();
        groupTimer.Tick += (_, _) => TickGroupFollow(); groupTimer.Start();
    }
    private void SaveGroups()
    {
        if (smokeMode) return;
        if (groupStorageFailed) throw new InvalidOperationException("Gruppenspeicher nicht verfügbar.");
        GroupMembershipStore.Save(groupProfile);
    }
    private void ReloadGroupPickers()
    {
        var selected = SelectedGroup; var shared = shareGroupPicker.SelectedItem as GroupMembership;
        groupUiLoading = true;
        try
        {
            groupPicker.ItemsSource = null; groupPicker.ItemsSource = groupProfile.Groups;
            groupPicker.SelectedItem = selected != null && groupProfile.Groups.Contains(selected) ? selected : groupProfile.Groups.FirstOrDefault();
            var owned = groupProfile.Groups.Where(x => x.Role == "Owner" && x.Status == "Approved" && !x.RegistrationPending).ToList();
            shareGroupPicker.ItemsSource = null; shareGroupPicker.ItemsSource = owned;
            shareGroupPicker.SelectedItem = shared != null && owned.Contains(shared) ? shared : owned.FirstOrDefault();
        }
        finally { groupUiLoading = false; }
        UpdateGroupControls();
    }
    private void UpdateGroupControls()
    {
        if (groupJoinButton == null) return;
        var group = SelectedGroup; bool available = !groupBusy && !groupStorageFailed;
        groupJoinButton.IsEnabled = available && group?.Status == "Approved" && selectedGroupSnapshot?.Team != null;
        groupAuto.IsEnabled = available && group?.Status == "Approved";
        shareButton.IsEnabled = available && shareGroupPicker.Items.Count > 0;
        foreach (var button in groupOwnerButtons) button.IsEnabled = available && group?.Role == "Owner" && group.Status == "Approved";
        if (!ReferenceEquals(displayedGroupSnapshot, selectedGroupSnapshot))
        {
            displayedGroupSnapshot = selectedGroupSnapshot;
            groupMembers.ItemsSource = selectedGroupSnapshot?.Members.Where(x => x.Status == "Approved").ToList();
            groupRequests.ItemsSource = selectedGroupSnapshot?.Members.Where(x => x.Status == "Pending").ToList();
        }
        groupStopButton.IsEnabled = groupFollow.Enabled;
        groupStopButton.Visibility = groupFollow.Enabled ? Visibility.Visible : Visibility.Collapsed;
        if (group == null && !groupStorageFailed) groupStatus.Text = "Noch keine Gruppe gespeichert. Gruppe erstellen oder Einladung einfügen.";
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
    private async Task JoinGroupAsync()
    {
        var input = GroupPrompt("Gruppe beitreten", ("Vollständiger Einladungslink", ""), ("Dein Name", "")); if (input == null) return;
        var invitation = GroupServiceAddress.ParseInvitation(input[0]);
        if (groupProfile.Groups.Any(x => x.ServiceUrl == invitation.ServiceUrl && x.GroupId == invitation.GroupId)) throw new ArgumentException("Diese Gruppe ist bereits gespeichert. Aktualisieren oder die alte Mitgliedschaft aus der Liste entfernen.");
        var group = new GroupMembership { ServiceUrl = invitation.ServiceUrl, GroupId = invitation.GroupId, MemberId = GroupMembership.NewId(), Token = GroupMembership.NewToken(), InviteToken = invitation.InviteToken, Name = "Neue Gruppenanfrage", DisplayName = input[1].Trim(), RegistrationPending = true };
        group.Validate(); groupProfile.Groups.Add(group);
        try { SaveGroups(); } catch { groupProfile.Groups.Remove(group); throw; }
        ReloadGroupPickers(); groupPicker.SelectedItem = group;
        await CompleteRegistrationAsync(group);
        groupStatus.Text = "Anfrage gesendet · wartet auf Bestätigung. „Aktualisieren“ prüft die Freigabe.";
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
        UpdateGroupControls();
        try
        {
            var snapshot = await groupApi.GetAsync(group, groupLifetime.Token);
            if (!groupFollow.Enabled || generation != groupFollow.Generation || closing) return;
            if (snapshot.YourStatus != "Approved") throw new GroupApiException("Mitgliedschaft nicht bestätigt.", "membership_revoked");
            ApplyFollowedGroup(generation, group, snapshot);
            if (!auto && snapshot.Team == null) { StopGroupFollow(); groupStatus.Text = "Noch keine Auswahl veröffentlicht."; return; }
            groupSync = new(groupApi, group);
            groupSync.AccessRevoked += ex => Dispatcher.BeginInvoke(() =>
            {
                if (closing || generation != groupFollow.Generation) return;
                group.Status = "Removed"; StopAll("Mitgliedschaft beendet"); SaveGroups(); ReloadGroupPickers(); groupStatus.Text = ex.Message;
            });
            groupSync.Updated += update => Dispatcher.BeginInvoke(() => { if (!closing) ApplyFollowedGroup(generation, group, update); });
            groupSync.ConnectionChanged += (online, reason) => Dispatcher.BeginInvoke(() =>
            {
                if (closing || generation != groupFollow.Generation || !groupFollow.Enabled) return;
                if (!online) { groupFollow.Disconnected(generation); automation.Observe(null); automation.Stop("Gruppenverbindung pausiert", AutomationStopCause.GroupUpdate); stableGroupDialog = 0; }
                groupStatus.Text = reason; UpdateGroupControls();
            });
            groupSync.Start();
            if (!smokeMode && config.FocusGameOnTeamActivation) screen.TryBringGameToForeground(config);
        }
        catch { if (generation == groupFollow.Generation) StopAll("Gruppenbeitritt fehlgeschlagen"); throw; }
    }
    private void ApplyFollowedGroup(long generation, GroupMembership group, GroupSnapshot snapshot)
    {
        if (generation != groupFollow.Generation || !groupFollow.Enabled || closing) return;
        if (snapshot.Revision < groupFollow.Revision) return;
        var changed = groupFollow.Apply(generation, snapshot, snapshot.ReceivedAt == 0 ? Stopwatch.GetTimestamp() : snapshot.ReceivedAt);
        group.Apply(snapshot);
        try { SaveGroups(); } catch { StopAll("Gruppenspeicher nicht verfügbar"); groupStatus.Text = "Gruppenspeicher nicht verfügbar · Auto wurde gestoppt."; return; }
        selectedGroupSnapshot = snapshot;
        groupStatus.Text = DescribeGroup(snapshot) + "\nOnline · Zustandsprüfung mindestens jede Minute";
        if (snapshot.YourStatus != "Approved") { StopAll("Mitgliedschaft beendet"); ReloadGroupPickers(); return; }
        if (changed) { automation.Stop("Gruppenauswahl aktualisiert", AutomationStopCause.GroupUpdate); stableGroupDialog = 0; lastGroupObservation = 0; }
        automation.RenewOnlineLease(groupFollow.OnlineLeaseDeadline);
        if (followedSettings != null) automation.Observe(snapshot.Team == null ? null : followedSettings);
        UpdateGroupControls();
    }
    private void TickGroupFollow()
    {
        if (!groupFollow.Enabled || followedSettings == null || closing) return;
        var current = automation.Snapshot;
        if (current.State == RunState.Stopped && current.StopCause is AutomationStopCause.Manual or AutomationStopCause.Safety) { StopGroupFollow(); return; }
        if (!groupFollow.CanRun(groupFollow.Generation, Stopwatch.GetTimestamp()))
        {
            if (!groupFollow.Online || groupFollow.Team != null) { automation.Observe(null); if (current.State != RunState.Stopped) automation.Stop("Gruppenstatus nicht aktuell", AutomationStopCause.GroupUpdate); }
            stableGroupDialog = 0; return;
        }
        if (current.State != RunState.Stopped) return;
        if (current.StopCause == AutomationStopCause.Joined && !groupFollow.Auto) { StopGroupFollow(); return; }
        if (current.Detection?.IsMatch != true || current.JoinedDetection?.IsMatch == true || current.Geometry?.IsForeground != true || (!followedSettings.DryRun && !current.Geometry.IsCalibrated)) { stableGroupDialog = 0; return; }
        // Distinct sparse observations, followed by the controller's own three-frame validation.
        var timestamp = Stopwatch.GetTimestamp();
        if (lastGroupObservation != 0 && Stopwatch.GetElapsedTime(lastGroupObservation, timestamp).TotalMilliseconds < 240) return;
        lastGroupObservation = timestamp;
        if (++stableGroupDialog < 3) return;
        stableGroupDialog = 0;
        if (groupFollow.CanRun(groupFollow.Generation, timestamp) && Enum.TryParse<Team>(groupFollow.Team, out var team)) automation.Start(team, followedSettings, groupFollow.OnlineLeaseDeadline);
    }
    private async Task ShareAndJoinAsync()
    {
        var group = shareGroupPicker.SelectedItem as GroupMembership ?? throw new ArgumentException("Eine eigene Gruppe wählen.");
        var team = (Team)((ComboBoxItem)sharedTeamPicker.SelectedItem).Tag;
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
        try { StopAll("Gruppe aus lokaler Liste entfernt"); groupProfile.Groups.Remove(group); SaveGroups(); selectedGroupSnapshot = null; ReloadGroupPickers(); }
        catch (Exception) { groupStatus.Text = "Lokale Gruppenliste konnte nicht gespeichert werden."; }
    }
    private void CopyGroupInvitation()
    {
        var group = SelectedGroup;
        if (group?.Role != "Owner" || group.InviteToken == null) return;
        try { Clipboard.SetText(group.InvitationLink); groupStatus.Text = "Einladungslink kopiert."; }
        catch (Exception) { groupStatus.Text = "Zwischenablage momentan nicht verfügbar."; }
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
        if (groupFollow.Enabled && automation.Snapshot.State != RunState.Stopped) automation.Stop("Gruppenmodus beendet", AutomationStopCause.GroupUpdate);
        groupFollow.Stop(); groupSync?.Dispose(); groupSync = null; followedMembership = null; followedSettings = null;
        stableGroupDialog = 0; lastGroupObservation = 0; automation.Observe(null);
        groupUiLoading = true; groupAuto.IsChecked = false; groupUiLoading = false;
        if (groupStopButton != null) UpdateGroupControls();
    }
    private void DisposeGroups()
    {
        groupLifetime.Cancel(); groupTimer.Stop(); StopGroupFollow(); groupApi.Dispose();
    }
    private string[]? GroupPrompt(string title, params (string Label, string Initial)[] values)
    {
        var dialog = new Window { Owner = this, Title = title, Width = 580, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize, Background = Background, Foreground = Foreground, FontFamily = FontFamily, FontSize = FontSize, MaxHeight = 600 };
        dialog.Resources.MergedDictionaries.Add(CreateTheme());
        var body = new StackPanel { Margin = new Thickness(24) }; var inputs = new System.Collections.Generic.List<TextBox>();
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
