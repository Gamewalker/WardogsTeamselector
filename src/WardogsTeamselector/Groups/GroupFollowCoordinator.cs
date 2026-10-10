using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WardogsTeamselector.Groups;

/// <summary>UI-thread state machine. Network generations and online lease gate every group start.</summary>
public sealed class GroupFollowCoordinator
{
    public long Generation { get; private set; }
    public bool Enabled { get; private set; }
    public bool Auto { get; private set; }
    public bool Online { get; private set; }
    public string? GroupId { get; private set; }
    public string? ServiceUrl { get; private set; }
    public string? Team { get; private set; }
    public long Revision { get; private set; }
    public long SelectionVersion { get; private set; }
    private long lastReceipt;
    private GroupSnapshot? currentSnapshot;
    private bool waitingForSelection;
    private long joinRefreshAttempt, nextJoinRefresh;
    public bool JoinRefreshPending { get; private set; }
    public long OnlineLeaseDeadline => lastReceipt + System.Diagnostics.Stopwatch.Frequency * 75;
    public long Begin(GroupMembership group, bool auto)
    {
        Stop(); GroupId = group.GroupId; ServiceUrl = group.ServiceUrl; Auto = auto; Enabled = true;
        return Generation;
    }
    public void Stop() { Generation++; joinRefreshAttempt++; JoinRefreshPending = false; nextJoinRefresh = 0; waitingForSelection = false; currentSnapshot = null; lastReceipt = 0; Enabled = false; Auto = false; Online = false; Team = null; Revision = 0; SelectionVersion = 0; }
    public void Disconnected(long generation) { if (Generation == generation) Online = false; }
    public bool Apply(long generation, GroupSnapshot snapshot, long timestamp)
    {
        if (!Enabled || generation != Generation || snapshot.GroupId != GroupId || snapshot.Revision < Revision || (snapshot.Revision == Revision && timestamp < lastReceipt)) return false;
        if (snapshot.YourStatus != "Approved") { Online = false; Team = null; return true; }
        var changed = snapshot.SelectionVersion != SelectionVersion || snapshot.Team != Team;
        Revision = snapshot.Revision; SelectionVersion = snapshot.SelectionVersion; Team = snapshot.Team;
        Online = true; lastReceipt = timestamp; currentSnapshot = snapshot;
        // A publication ends the wait for a missing selection immediately. Keep
        // transport-error backoff intact so it cannot exhaust the call budget.
        if (waitingForSelection && Team != null) { waitingForSelection = false; nextJoinRefresh = 0; }
        return changed;
    }
    public bool HasCurrentState(long generation, long timestamp) => Enabled && Generation == generation && Online && Stopwatch.GetElapsedTime(lastReceipt, timestamp).TotalSeconds <= 75;
    public bool CanRun(long generation, long timestamp) => Team != null && HasCurrentState(generation, timestamp);

    /// <summary>Every newly detected dialog needs its own server confirmation. Never use a cached selection if this fails.</summary>
    public async Task<GroupSnapshot?> RefreshForJoinAsync(long generation, Func<CancellationToken, Task<GroupSnapshot>> refresh, CancellationToken cancellation)
    {
        if (!Enabled || generation != Generation || JoinRefreshPending || Stopwatch.GetTimestamp() < nextJoinRefresh) return null;
        var attempt = ++joinRefreshAttempt;
        var requestedAt = Stopwatch.GetTimestamp();
        JoinRefreshPending = true;
        try
        {
            var snapshot = await refresh(cancellation);
            cancellation.ThrowIfCancellationRequested();
            if (!Enabled || generation != Generation || snapshot.GroupId != GroupId) return null;
            // A pushed update received during this request is a newer server
            // confirmation. An older HTTP response must not delay that team.
            if (snapshot.Revision < Revision)
            {
                if (Online && lastReceipt >= requestedAt && currentSnapshot != null) snapshot = currentSnapshot;
                else { nextJoinRefresh = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 15; return null; }
            }
            waitingForSelection = snapshot.Team == null;
            nextJoinRefresh = waitingForSelection ? Stopwatch.GetTimestamp() + Stopwatch.Frequency * 15 : 0;
            return snapshot;
        }
        catch
        {
            // Leave room for the 15-second background sync within the ten-call budget.
            if (attempt == joinRefreshAttempt) { waitingForSelection = false; nextJoinRefresh = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 15; }
            throw;
        }
        finally { if (attempt == joinRefreshAttempt) JoinRefreshPending = false; }
    }
}
