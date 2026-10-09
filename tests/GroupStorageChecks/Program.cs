using WardogsTeamselector.Groups;

var folder = WardogsTeamselector.SettingsStore.Folder;
try
{
    if (GroupMembershipStore.Load().Groups.Count != 0) throw new Exception("Missing store must start empty");
    var group = new GroupMembership { ServiceUrl = "https://groups.example", GroupId = GroupMembership.NewId(), MemberId = GroupMembership.NewId(), Token = GroupMembership.NewToken(), InviteToken = GroupMembership.NewToken(), Role = "Owner", Status = "Approved", Name = "Zehnergruppe", DisplayName = "Ersteller" };
    var profile = new GroupProfile { Groups = new() { group } };
    GroupMembershipStore.Save(profile);
    var stored = File.ReadAllBytes(GroupMembershipStore.FilePath);
    var text = System.Text.Encoding.UTF8.GetString(stored);
    if (text.Contains(group.Token) || text.Contains(group.InviteToken)) throw new Exception("Secrets stored as plaintext");
    var loaded = GroupMembershipStore.Load();
    if (loaded.Groups[0].Token != group.Token || loaded.Groups[0].InviteToken != group.InviteToken) throw new Exception("DPAPI roundtrip lost credentials");
    var recovery = GroupMembershipStore.Import(GroupMembershipStore.Export(profile));
    if (recovery.Groups[0].Role != "Owner" || recovery.Groups[0].MemberId != group.MemberId) throw new Exception("Recovery lost ownership");
    File.WriteAllBytes(GroupMembershipStore.FilePath, new byte[] { 1, 2, 3 });
    bool rejected = false; try { GroupMembershipStore.Load(); } catch { rejected = true; }
    if (!rejected) throw new Exception("Corrupt store silently accepted");
    Console.WriteLine("GroupStorageChecks: DPAPI, private recovery and corrupt-store rejection passed. No user profile changed.");
}
finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }

namespace WardogsTeamselector
{
    // Isolate the real store implementation from the user's AppData directory.
    public static class SettingsStore
    {
        public static string Folder { get; } = Path.Combine(Path.GetTempPath(), "WardogsGroupChecks-" + Guid.NewGuid().ToString("N"));
    }
}
