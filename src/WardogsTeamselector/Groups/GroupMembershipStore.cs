using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WardogsTeamselector.Groups;

public static class GroupMembershipStore
{
    public static string FilePath => Path.Combine(SettingsStore.Folder, "groups.dat");
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("WardogsTeamselector.Groups.v1");
    public static GroupProfile Load()
    {
        if (!File.Exists(FilePath)) return new();
        var plain = ProtectedData.Unprotect(File.ReadAllBytes(FilePath), Entropy, DataProtectionScope.CurrentUser);
        try { var profile = JsonSerializer.Deserialize<GroupProfile>(plain, GroupJson.Options) ?? throw new InvalidDataException("Leere Gruppendatei."); GroupRecoveryCodec.Validate(profile); return profile; }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }
    public static void Save(GroupProfile profile)
    {
        GroupRecoveryCodec.Validate(profile);
        var plain = JsonSerializer.SerializeToUtf8Bytes(profile, GroupJson.Options);
        byte[] encrypted;
        try { encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser); }
        finally { CryptographicOperations.ZeroMemory(plain); }
        Directory.CreateDirectory(SettingsStore.Folder);
        var temp = FilePath + ".tmp"; File.WriteAllBytes(temp, encrypted); File.Move(temp, FilePath, true);
    }
    public static string Export(GroupProfile profile) => GroupRecoveryCodec.Export(profile);
    public static GroupProfile Import(string code) => GroupRecoveryCodec.Import(code);
}
