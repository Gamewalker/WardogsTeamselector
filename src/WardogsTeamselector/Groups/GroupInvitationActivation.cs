using System;

namespace WardogsTeamselector.Groups;

public static class GroupInvitationActivation
{
    public const string Scheme = "wardogs";
    public static string Parse(string value)
    {
        if (value.Length > 4096 || !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme != Scheme || uri.Host != "join" || uri.AbsolutePath != "/" ||
            uri.Port != -1 || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length <= 1)
            throw new ArgumentException("Ungültiger Einladungslink.");
        var invitation = Uri.UnescapeDataString(uri.Fragment[1..]);
        var parsed = GroupServiceAddress.ParseInvitation(invitation);
        return $"{parsed.ServiceUrl}/invite/{parsed.GroupId}#{parsed.InviteToken}";
    }
}
