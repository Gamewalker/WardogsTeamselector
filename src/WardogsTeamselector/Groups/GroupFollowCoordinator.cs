using System;

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
    public long Begin(GroupMembership group, bool auto)
    {
        Stop(); GroupId = group.GroupId; ServiceUrl = group.ServiceUrl; Auto = auto; Enabled = true;
        return Generation;
    }
    public void Stop() { Generation++; Enabled = false; Auto = false; Online = false; Team = null; Revision = 0; SelectionVersion = 0; }
    public void Disconnected(long generation) { if (Generation == generation) Online = false; }
    public bool Apply(long generation, GroupSnapshot snapshot, long timestamp)
    {
        if (!Enabled || generation != Generation || snapshot.GroupId != GroupId || snapshot.Revision < Revision) return false;
        if (snapshot.YourStatus != "Approved") { Online = false; Team = null; return true; }
        var changed = snapshot.SelectionVersion != SelectionVersion || snapshot.Team != Team;
        Revision = snapshot.Revision; SelectionVersion = snapshot.SelectionVersion; Team = snapshot.Team;
        Online = true; lastReceipt = timestamp; return changed;
    }
    public bool CanRun(long generation, long timestamp) => Enabled && Generation == generation && Online && Team != null && System.Diagnostics.Stopwatch.GetElapsedTime(lastReceipt, timestamp).TotalSeconds <= 75;
}
