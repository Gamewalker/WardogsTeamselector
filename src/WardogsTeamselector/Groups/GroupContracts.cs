using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WardogsTeamselector.Groups;

public sealed record GroupMember(string Id, string DisplayName, string Status, string Role, long RequestedAt)
{
    [JsonIgnore] public string Label => $"{DisplayName} · {Id[..Math.Min(6, Id.Length)]} · {StatusLabel(Status)}";
    public static string StatusLabel(string status) => status switch { "Approved" => "Bestätigt", "Pending" => "Wartet auf Bestätigung", "Rejected" => "Abgelehnt", "Removed" => "Entfernt", _ => "Unbekannt" };
}
public sealed record GroupSnapshot(int ProtocolVersion, string GroupId, string Name, long Revision, long SelectionVersion, string? Team, string YourStatus, string YourRole, string MemberId, List<GroupMember> Members)
{
    public void Validate(GroupMembership membership)
    {
        if (ProtocolVersion != 1 || GroupId != membership.GroupId || MemberId != membership.MemberId || Revision < 1 || SelectionVersion < 0 || Team is not (null or "Blue" or "Red" or "Green") || YourStatus is not ("Approved" or "Pending" or "Rejected" or "Removed") || YourRole is not ("Owner" or "Member") || string.IsNullOrWhiteSpace(Name) || Name.Length > 48 || Members == null || Members.Count > 200)
            throw new GroupApiException("Ungültige Antwort des Gruppendienstes.", "invalid_response");
    }
}
public sealed class GroupMembership
{
    public string ServiceUrl { get; set; } = "";
    public string GroupId { get; set; } = "";
    public string MemberId { get; set; } = "";
    public string Token { get; set; } = "";
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "Member";
    public string Status { get; set; } = "Pending";
    public string? InviteToken { get; set; }
    public bool RegistrationPending { get; set; }
    public string? PendingToken { get; set; }
    public string? PendingCredentialOperation { get; set; }
    [JsonIgnore] public string Label => $"{Name} · {GroupMember.StatusLabel(Status)}{(Role == "Owner" ? " · Ersteller" : "")}";
    [JsonIgnore] public string InvitationLink => $"{ServiceUrl}/invite/{GroupId}#{InviteToken}";
    public void Apply(GroupSnapshot snapshot) { snapshot.Validate(this); Name = snapshot.Name; Role = snapshot.YourRole; Status = snapshot.YourStatus; }
    public static string NewId() => Guid.NewGuid().ToString("N");
    public static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public void Validate()
    {
        ServiceUrl = GroupServiceAddress.Normalize(ServiceUrl);
        if (!ValidId(GroupId) || !ValidId(MemberId) || !ValidToken(Token) || (InviteToken != null && !ValidToken(InviteToken)) || (PendingToken != null && !ValidToken(PendingToken)) || (PendingCredentialOperation != null && !ValidId(PendingCredentialOperation)) || string.IsNullOrWhiteSpace(Name) || Name.Length > 48 || string.IsNullOrWhiteSpace(DisplayName) || DisplayName.Length > 48 || Role is not ("Owner" or "Member") || Status is not ("Approved" or "Pending" or "Rejected" or "Removed")) throw new ArgumentException("Ungültige gespeicherte Gruppenmitgliedschaft.");
    }
    public static bool ValidId(string value) => value.Length == 32 && System.Text.RegularExpressions.Regex.IsMatch(value, "\\A[a-f0-9]{32}\\z");
    public static bool ValidToken(string value) => value.Length == 43 && System.Text.RegularExpressions.Regex.IsMatch(value, "\\A[A-Za-z0-9_-]{43}\\z");
}
public sealed class GroupProfile
{
    public int Version { get; set; } = 1;
    public string ServiceUrl { get; set; } = "";
    public List<GroupMembership> Groups { get; set; } = new();
}
public static class GroupJson
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
}
public static class GroupServiceAddress
{
    public static string Normalize(string value)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo) || uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback))) throw new ArgumentException("Gruppendienst: eine HTTPS-Adresse ohne Pfad verwenden.");
        return uri.GetLeftPart(UriPartial.Authority);
    }
    public static (string ServiceUrl, string GroupId, string InviteToken) ParseInvitation(string link)
    {
        if (!Uri.TryCreate(link.Trim(), UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query)) throw new ArgumentException("Ungültiger Einladungslink.");
        var service = Normalize(uri.GetLeftPart(UriPartial.Authority));
        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var invite = uri.Fragment.TrimStart('#');
        if (parts.Length != 2 || parts[0] != "invite" || !GroupMembership.ValidId(parts[1]) || !GroupMembership.ValidToken(invite)) throw new ArgumentException("Der vollständige Einladungslink mit Zugangscode wird benötigt.");
        return (service, parts[1], invite);
    }
}
public sealed class GroupApiException : Exception
{
    public string Code { get; }
    public bool AccessRevoked => Code is "membership_revoked" or "group_deleted" or "request_expired" or "unauthorized";
    public GroupApiException(string message, string code) : base(message) { Code = code; }
}
