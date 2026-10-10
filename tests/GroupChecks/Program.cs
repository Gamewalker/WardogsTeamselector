using System.Diagnostics;
using System.Net;
using System.Text.Json;
using WardogsTeamselector.Groups;

var checks = 0;
void Check(bool condition, string label) { if (!condition) throw new Exception(label); checks++; }
void Reject(Action action, string label) { bool rejected = false; try { action(); } catch (Exception) { rejected = true; } Check(rejected, label); }
GroupMembership Membership() => new() { ServiceUrl = "https://groups.example", GroupId = GroupMembership.NewId(), MemberId = GroupMembership.NewId(), Token = GroupMembership.NewToken(), Name = "Freunde", DisplayName = "Spieler", Status = "Approved" };
GroupSnapshot Snapshot(GroupMembership group, long revision = 1, long selection = 1, string? team = "Blue", string status = "Approved") => new(1, group.GroupId, group.Name, revision, selection, team, status, "Member", group.MemberId, new());

Check(new GroupProfile().ServiceUrl == "https://wardogs-groups.niels-82f.workers.dev", "New profiles use the default group service");
Check(JsonSerializer.Deserialize<GroupProfile>("{\"serviceUrl\":\"https://custom.example\"}", GroupJson.Options)!.ServiceUrl == "https://custom.example", "Stored custom service survives the new default");

var group = Membership(); var follower = new GroupFollowCoordinator(); var now = Stopwatch.GetTimestamp();
var invitationLink = group.ServiceUrl + "/invite/" + group.GroupId + "#" + GroupMembership.NewToken();
var activationLink = "wardogs://join/#" + Uri.EscapeDataString(invitationLink);
Check(GroupInvitationActivation.Parse(activationLink) == invitationLink, "App activation preserves service, group and secret");
var localInvitation = invitationLink.Replace(group.ServiceUrl, "http://localhost:8787");
Check(GroupInvitationActivation.Parse("wardogs://join/#" + Uri.EscapeDataString(localInvitation)) == localInvitation, "Local development service activation");
foreach (var invalid in new[] { "https://join/#" + Uri.EscapeDataString(invitationLink), "wardogs://other/#" + Uri.EscapeDataString(invitationLink), "wardogs://join:42/#" + Uri.EscapeDataString(invitationLink), "wardogs://user@join/#" + Uri.EscapeDataString(invitationLink), "wardogs://join/path#" + Uri.EscapeDataString(invitationLink), "wardogs://join/?link=x#" + Uri.EscapeDataString(invitationLink), "wardogs://join/#" + Uri.EscapeDataString(invitationLink.Replace("https:", "http:")), "wardogs://join/#" + Uri.EscapeDataString(invitationLink.Split('#')[0]), "wardogs://join/#" + new string('a', 4096) })
    Reject(() => GroupInvitationActivation.Parse(invalid), "Reject malformed or unsafe activation");
var generation = follower.Begin(group, true);
Check(!follower.CanRun(generation, now), "Cannot start before authorized state");
Check(follower.Apply(generation, Snapshot(group), now), "First team publication changes selection");
Check(follower.CanRun(generation, now), "Current approved selection can start");
Check(!follower.Apply(generation, Snapshot(group), now), "Duplicate state does not restart");
Check(!follower.Apply(generation, Snapshot(group, 2), now), "Membership revision does not restart selection");
Check(follower.Apply(generation, Snapshot(group, 3, 2, "Red"), now), "New selection replaces team");
Check(follower.Team == "Red", "Latest team retained");
Check(!follower.Apply(generation, Snapshot(group, 1), now), "Older revision ignored");
Check(follower.Team == "Red", "Old message cannot replace new team");
follower.Disconnected(generation); Check(!follower.CanRun(generation, now), "Disconnected state cannot start");
follower.Apply(generation, Snapshot(group, 3, 2, "Red"), now); Check(follower.CanRun(generation, now), "Fresh snapshot restores online gate");
Check(!follower.CanRun(generation, now + Stopwatch.Frequency * 76), "Stale online lease prevents clicks");
follower.Stop(); Check(!follower.Apply(generation, Snapshot(group, 4, 3, "Green"), now), "ESC generation rejects delayed messages");
Check(!follower.CanRun(generation, now), "ESC cannot auto-rearm");
var other = Membership(); var newer = follower.Begin(other, true);
Check(!follower.Apply(generation, Snapshot(group, 5, 4), now), "Group switch rejects old connection");
Check(!follower.Apply(newer, Snapshot(group, 5, 4), now), "Other group ID rejected even with current generation");
follower.Apply(newer, Snapshot(other, 1, 0, null), now); Check(follower.Enabled && !follower.CanRun(newer, now), "Auto may wait for no selection");
follower.Apply(newer, Snapshot(other, 2, 1, "Blue", "Removed"), now); Check(!follower.CanRun(newer, now), "Removal revokes click gate");

// A fresh server read is required for each newly detected selection dialog.
var repeated = new GroupFollowCoordinator();
var repeatGeneration = repeated.Begin(group, true);
repeated.Apply(repeatGeneration, Snapshot(group), Stopwatch.GetTimestamp());
var pendingRead = new TaskCompletionSource<GroupSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
var confirmation = repeated.RefreshForJoinAsync(repeatGeneration, _ => pendingRead.Task, CancellationToken.None);
Check(repeated.JoinRefreshPending && !confirmation.IsCompleted, "New dialog waits for server confirmation");
var duplicateRead = false;
Check(await repeated.RefreshForJoinAsync(repeatGeneration, _ => { duplicateRead = true; return Task.FromResult(Snapshot(group)); }, CancellationToken.None) == null && !duplicateRead, "Pending confirmation cannot send duplicate calls");
pendingRead.SetResult(Snapshot(group, 2, 2, "Red"));
var confirmed = await confirmation;
Check(confirmed?.Team == "Red", "Fresh dialog confirmation uses the leader's new team");
repeated.Apply(repeatGeneration, confirmed!, Stopwatch.GetTimestamp());
Check(repeated.Enabled && repeated.Auto && repeated.Team == "Red", "Auto remains enabled for subsequent dialogs");
var secondConfirmation = await repeated.RefreshForJoinAsync(repeatGeneration, _ => Task.FromResult(Snapshot(group, 3, 3, "Green")), CancellationToken.None);
Check(secondConfirmation?.Team == "Green", "Subsequent dialog performs another server read");

pendingRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
confirmation = repeated.RefreshForJoinAsync(repeatGeneration, _ => pendingRead.Task, CancellationToken.None);
repeated.Stop();
var nextGeneration = repeated.Begin(other, true);
var nextRead = new TaskCompletionSource<GroupSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
var nextConfirmation = repeated.RefreshForJoinAsync(nextGeneration, _ => nextRead.Task, CancellationToken.None);
pendingRead.SetResult(Snapshot(group, 4, 4));
Check(await confirmation == null && repeated.JoinRefreshPending, "Stopped generation cannot start input or clear another group's pending read");
nextRead.SetResult(Snapshot(other)); await nextConfirmation;
Check(!repeated.JoinRefreshPending, "Current confirmation releases its own pending guard");

repeatGeneration = repeated.Begin(group, true);
repeated.Apply(repeatGeneration, Snapshot(group, 5, 5), Stopwatch.GetTimestamp());
Check(await repeated.RefreshForJoinAsync(repeatGeneration, _ => Task.FromResult(Snapshot(group, 4, 4, "Red")), CancellationToken.None) == null,
    "Delayed HTTP state cannot override a newer pushed selection");
repeatGeneration = repeated.Begin(group, true);
repeated.Apply(repeatGeneration, Snapshot(group), Stopwatch.GetTimestamp());
try { await repeated.RefreshForJoinAsync(repeatGeneration, _ => Task.FromException<GroupSnapshot>(new HttpRequestException()), CancellationToken.None); Check(false, "Offline confirmation must fail"); }
catch (HttpRequestException) { Check(repeated.Enabled && repeated.Auto && !repeated.JoinRefreshPending, "Failed confirmation leaves Auto waiting without starting input"); }
var retriedRead = false;
Check(await repeated.RefreshForJoinAsync(repeatGeneration, _ => { retriedRead = true; return Task.FromResult(Snapshot(group)); }, CancellationToken.None) == null && !retriedRead,
    "Failed confirmation backs off instead of exhausting the call budget");
repeatGeneration = repeated.Begin(group, true);
var noSelection = await repeated.RefreshForJoinAsync(repeatGeneration, _ => Task.FromResult(Snapshot(group, 1, 0, null)), CancellationToken.None);
repeated.Apply(repeatGeneration, noSelection!, Stopwatch.GetTimestamp());
Check(repeated.HasCurrentState(repeatGeneration, Stopwatch.GetTimestamp()) && !repeated.CanRun(repeatGeneration, Stopwatch.GetTimestamp()),
    "Auto can observe a dialog while the leader has not published a team");
repeated.Apply(repeatGeneration, Snapshot(group, 2, 1, "Red"), Stopwatch.GetTimestamp());
var publicationRead = false;
Check((await repeated.RefreshForJoinAsync(repeatGeneration, _ => { publicationRead = true; return Task.FromResult(Snapshot(group, 2, 1, "Red")); }, CancellationToken.None))?.Team == "Red" && publicationRead,
    "Publishing a team releases the no-selection wait without a 15-second delay");

repeatGeneration = repeated.Begin(group, true);
repeated.Apply(repeatGeneration, Snapshot(group), Stopwatch.GetTimestamp());
pendingRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
confirmation = repeated.RefreshForJoinAsync(repeatGeneration, _ => pendingRead.Task, CancellationToken.None);
repeated.Apply(repeatGeneration, Snapshot(group, 2, 2, "Green"), Stopwatch.GetTimestamp());
pendingRead.SetResult(Snapshot(group));
Check((await confirmation)?.Team == "Green", "Publication during HTTP confirmation uses the newer pushed team immediately");

repeatGeneration = repeated.Begin(group, true);
repeated.Apply(repeatGeneration, Snapshot(group), Stopwatch.GetTimestamp());
pendingRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
confirmation = repeated.RefreshForJoinAsync(repeatGeneration, _ => pendingRead.Task, CancellationToken.None);
repeated.Apply(repeatGeneration, Snapshot(group, 2, 2, "Green"), Stopwatch.GetTimestamp());
repeated.Disconnected(repeatGeneration);
pendingRead.SetResult(Snapshot(group));
Check(await confirmation == null, "Disconnected pushed state cannot replace an older HTTP confirmation");

var invite = GroupMembership.NewToken();
var parsed = GroupServiceAddress.ParseInvitation($"https://groups.example/invite/{group.GroupId}#{invite}");
Check(parsed.ServiceUrl == group.ServiceUrl && parsed.GroupId == group.GroupId && parsed.InviteToken == invite, "Invitation retains secret fragment");
Reject(() => GroupServiceAddress.ParseInvitation($"http://groups.example/invite/{group.GroupId}#{invite}"), "Nonlocal HTTP rejected");
Reject(() => GroupServiceAddress.ParseInvitation($"https://user:secret@groups.example/invite/{group.GroupId}#{invite}"), "URL credentials rejected");
Reject(() => GroupServiceAddress.ParseInvitation($"https://groups.example/invite/{group.GroupId}?token={invite}"), "Query-string credentials rejected");
Reject(() => GroupServiceAddress.Normalize("https://groups.example/path"), "Service URL path rejected");
Check(GroupServiceAddress.Normalize("http://localhost:8787") == "http://localhost:8787", "Local development service accepted");
Reject(() => (Snapshot(group) with { GroupId = other.GroupId }).Validate(group), "Cross-group snapshot rejected");
Reject(() => (Snapshot(group) with { ProtocolVersion = 2 }).Validate(group), "Unsupported protocol rejected");
Reject(() => (Snapshot(group) with { Team = "Orange" }).Validate(group), "Unknown team rejected");

var profile = new GroupProfile { ServiceUrl = group.ServiceUrl, Groups = new() { group } };
group.PendingToken = GroupMembership.NewToken(); group.PendingCredentialOperation = GroupMembership.NewId();
var recovered = GroupRecoveryCodec.Import(GroupRecoveryCodec.Export(profile));
Check(recovered.Groups[0].Token == group.Token && recovered.Groups[0].PendingToken == group.PendingToken, "Recovery includes interrupted credential replacement");
Check(recovered.Groups[0].MemberId == group.MemberId, "Recovery retains identity");
Reject(() => GroupRecoveryCodec.Import("WDG2:invalid"), "Unsupported recovery code rejected");
Reject(() => GroupRecoveryCodec.Export(new GroupProfile { Groups = new() { group, group } }), "Duplicate memberships rejected");
var oldName = group.DisplayName; group.DisplayName = "Name\nwith newline";
Reject(() => GroupRecoveryCodec.Export(profile), "Control characters in names rejected before saving credentials");
group.DisplayName = oldName;
var admin = Membership(); admin.Role = "Owner"; admin.Status = "Approved"; admin.InviteToken = GroupMembership.NewToken();
var adminCopy = GroupRecoveryCodec.ImportAdmin(GroupRecoveryCodec.ExportAdmin(admin));
Check(adminCopy.Key == admin.Key && adminCopy.Token == admin.Token && adminCopy.InviteToken == admin.InviteToken, "Single-group admin transfer retains management and invitation rights");
Check(adminCopy != admin, "Imported admin identity is independent of source instance");
Reject(() => GroupRecoveryCodec.ExportAdmin(group), "Member identity cannot be exported as administrator");
Reject(() => GroupRecoveryCodec.ImportAdmin(GroupRecoveryCodec.Export(profile)), "Whole-profile backup cannot be mistaken for single-group admin transfer");
admin.PendingToken = GroupMembership.NewToken();
Reject(() => GroupRecoveryCodec.ExportAdmin(admin), "Interrupted admin rotation must be resolved before transfer");
admin.PendingToken = null; admin.Status = "Removed";
Reject(() => GroupRecoveryCodec.ExportAdmin(admin), "Revoked admin identity cannot be transferred");
profile.ActiveGroupKey = group.Key;
Check(GroupRecoveryCodec.Import(GroupRecoveryCodec.Export(profile)).ActiveGroupKey == group.Key, "Active group survives profile recovery");

var handler = new FakeHandler(request =>
{
    Check(request.Headers.Authorization?.Parameter == group.Token, "Authentication stays in request header");
    Check(!request.RequestUri!.ToString().Contains(group.Token), "No token in request URL");
    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(Snapshot(group), GroupJson.Options)) };
});
using (var api = new GroupApiClient(handler)) Check((await api.GetAsync(group)).Team == "Blue", "Authorized API state parsed");
using (var api = new GroupApiClient(new FakeHandler(_ => new(HttpStatusCode.Forbidden) { Content = new StringContent("{\"code\":\"membership_revoked\",\"message\":\"secret provider text\"}") })))
{
    try { await api.GetAsync(group); Check(false, "Revocation expected"); }
    catch (GroupApiException ex) { Check(ex.AccessRevoked && !ex.Message.Contains("secret"), "Provider text cannot leak into diagnostics"); }
}
using (var api = new GroupApiClient(new FakeHandler(_ => new(HttpStatusCode.Found) { Headers = { Location = new Uri("https://other.example") }, Content = new StringContent("") })))
{
    try { await api.GetAsync(group); Check(false, "Redirect must fail"); } catch (GroupApiException) { Check(true, "Redirect does not forward credentials"); }
}
using (var api = new GroupApiClient(new FakeHandler(_ => new(HttpStatusCode.OK) { Content = new StringContent(new string('x', 65537)) })))
{
    try { await api.GetAsync(group); Check(false, "Large response must fail"); } catch (GroupApiException ex) { Check(ex.Code == "invalid_response", "Response size bounded"); }
}
using (var api = new GroupApiClient(new FakeHandler(_ => new(HttpStatusCode.Forbidden) { Content = new StringContent("[]") })))
{
    try { await api.GetAsync(group); Check(false, "Forbidden expected"); } catch (GroupApiException ex) { Check(ex.AccessRevoked, "Unexpected JSON error shape retains authorization failure"); }
}
Console.WriteLine($"GroupChecks: {checks} checks passed.");

if (args.Length == 2 && args[0] == "--live")
{
    var owner = Membership(); owner.ServiceUrl = GroupServiceAddress.Normalize(args[1]); owner.Role = "Owner"; owner.InviteToken = GroupMembership.NewToken();
    var member = Membership(); member.ServiceUrl = owner.ServiceUrl; member.GroupId = owner.GroupId; member.Status = "Pending";
    using var realApi = new GroupApiClient();
    await realApi.CreateAsync(owner);
    try
    {
        await realApi.JoinAsync(member, owner.InviteToken);
        await realApi.ActionAsync(owner, "approve", new { operationId = GroupMembership.NewId(), memberId = member.MemberId });
        using var sync = new GroupSyncClient(realApi, member);
        var updates = new System.Collections.Concurrent.ConcurrentQueue<(GroupSnapshot State, long Time)>();
        var connectionReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var revoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sync.Updated += value => updates.Enqueue((value, Stopwatch.GetTimestamp()));
        sync.ConnectionChanged += (online, _) => { if (online) connectionReady.TrySetResult(); };
        sync.AccessRevoked += _ => revoked.TrySetResult();
        sync.Start(); await connectionReady.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await realApi.ActionAsync(owner, "publish", new { operationId = GroupMembership.NewId(), team = "Red" });
        var waiting = Stopwatch.StartNew();
        while (!updates.Any(x => x.State.Team == "Red") && waiting.Elapsed.TotalSeconds < 10) await Task.Delay(20);
        Check(updates.Any(x => x.State.Team == "Red"), "Real .NET WebSocket receives immediate publication");
        Console.WriteLine("Live client: connected and Red received; checking the scheduled 15-second sync.");
        var heartbeatStart = Stopwatch.GetTimestamp();
        waiting.Restart();
        while (!updates.Any(x => x.State.Team == "Red" && Stopwatch.GetElapsedTime(heartbeatStart, x.Time).TotalSeconds >= 12) && waiting.Elapsed.TotalSeconds < 25) await Task.Delay(100);
        Check(updates.Any(x => x.State.Team == "Red" && Stopwatch.GetElapsedTime(heartbeatStart, x.Time).TotalSeconds >= 12), "Real 15-second sync refreshes unchanged authorized state");
        await realApi.ActionAsync(owner, "remove", new { operationId = GroupMembership.NewId(), memberId = member.MemberId });
        await revoked.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Check(true, "Real removal revokes .NET WebSocket membership");
        var transferred = GroupRecoveryCodec.ImportAdmin(GroupRecoveryCodec.ExportAdmin(owner));
        Check((await realApi.GetAsync(transferred)).YourRole == "Owner", "Copied administrative token authorizes another instance");
        var replacement = GroupMembership.NewToken();
        await realApi.ActionAsync(transferred, "credential", new { operationId = GroupMembership.NewId(), newToken = replacement });
        var oldAdmin = GroupRecoveryCodec.ImportAdmin(GroupRecoveryCodec.ExportAdmin(owner));
        owner.Token = replacement; transferred.Token = replacement;
        try { await realApi.GetAsync(oldAdmin); Check(false, "Previous admin access must be revoked after exclusive takeover"); }
        catch (GroupApiException ex) { Check(ex.AccessRevoked, "Exclusive admin takeover revokes the source instance"); }
        Check((await realApi.GetAsync(transferred)).YourRole == "Owner", "Recipient retains administration after exclusive takeover");
        Console.WriteLine("Live client: 15-second sync and removal passed.");
    }
    finally { await realApi.ActionAsync(owner, "delete", new { operationId = GroupMembership.NewId() }); }
}

sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
}
