using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace WardogsTeamselector.Groups;

public static class GroupRecoveryCodec
{
    public static string ExportAdmin(GroupMembership group)
    {
        ValidateAdmin(group);
        return "WDGADMIN1:" + Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(group, GroupJson.Options));
    }
    public static GroupMembership ImportAdmin(string code)
    {
        if (!code.StartsWith("WDGADMIN1:", StringComparison.Ordinal) || code.Length > 16000) throw new ArgumentException("Ungültiger Admin-Übertragungscode.");
        var group = JsonSerializer.Deserialize<GroupMembership>(Convert.FromBase64String(code[10..]), GroupJson.Options) ?? throw new InvalidDataException("Leerer Admin-Übertragungscode.");
        ValidateAdmin(group); return group;
    }
    private static void ValidateAdmin(GroupMembership group)
    {
        group.Validate();
        if (group.Role != "Owner" || group.Status != "Approved" || group.RegistrationPending || group.PendingToken != null || group.PendingCredentialOperation != null)
            throw new ArgumentException("Nur einen bestätigten und aktuellen Adminzugang übertragen.");
    }
    public static string Export(GroupProfile profile)
    {
        Validate(profile);
        return "WDG1:" + Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(profile, GroupJson.Options));
    }
    public static GroupProfile Import(string code)
    {
        if (!code.StartsWith("WDG1:", StringComparison.Ordinal) || code.Length > 200000) throw new ArgumentException("Ungültiger Wiederherstellungscode.");
        var profile = JsonSerializer.Deserialize<GroupProfile>(Convert.FromBase64String(code[5..]), GroupJson.Options) ?? throw new InvalidDataException("Leerer Wiederherstellungscode.");
        Validate(profile); return profile;
    }
    public static void Validate(GroupProfile profile)
    {
        if (profile.Version != 1 || profile.Groups == null || profile.Groups.Count > 100) throw new ArgumentException("Nicht unterstützte Gruppendatei.");
        if (!string.IsNullOrWhiteSpace(profile.ServiceUrl)) profile.ServiceUrl = GroupServiceAddress.Normalize(profile.ServiceUrl);
        foreach (var group in profile.Groups) group.Validate();
        if (profile.Groups.Select(x => (x.ServiceUrl, x.GroupId)).Distinct().Count() != profile.Groups.Count) throw new ArgumentException("Gruppenmitgliedschaft doppelt gespeichert.");
        if (profile.ActiveGroupKey != null && !profile.Groups.Any(g => g.Key == profile.ActiveGroupKey)) profile.ActiveGroupKey = null;
    }
}
