using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WardogsTeamselector.Groups;

public sealed class GroupApiClient : IDisposable
{
    private readonly HttpClient http;
    public GroupApiClient(HttpMessageHandler? handler = null)
    {
        http = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(10) };
    }
    public async Task<GroupSnapshot> CreateAsync(GroupMembership group, CancellationToken cancellation = default) => await SendAsync(group, "", new { group.GroupId, group.MemberId, group.Token, group.InviteToken, group.Name, group.DisplayName }, false, cancellation);
    public async Task<GroupSnapshot> JoinAsync(GroupMembership group, string inviteToken, CancellationToken cancellation = default) => await SendAsync(group, "join", new { group.MemberId, group.Token, inviteToken, group.DisplayName }, false, cancellation);
    public Task<GroupSnapshot> GetAsync(GroupMembership group, CancellationToken cancellation = default) => SendAsync(group, "state", null, true, cancellation);
    public Task<GroupSnapshot> ActionAsync(GroupMembership group, string action, object data, CancellationToken cancellation = default) => SendAsync(group, action, data, true, cancellation);
    private async Task<GroupSnapshot> SendAsync(GroupMembership group, string action, object? data, bool authenticated, CancellationToken cancellation)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(TimeSpan.FromSeconds(10)); cancellation = deadline.Token;
        var url = GroupServiceAddress.Normalize(group.ServiceUrl) + (action.Length == 0 ? "/v1/groups" : $"/v1/groups/{group.GroupId}/{action}");
        using var request = new HttpRequestMessage(data == null ? HttpMethod.Get : HttpMethod.Post, url);
        if (authenticated) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", group.Token);
        if (data != null) request.Content = new StringContent(JsonSerializer.Serialize(data, GroupJson.Options), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[4096]; int count;
        while ((count = await stream.ReadAsync(chunk, cancellation).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + count > 65536) throw new GroupApiException("Antwort des Gruppendienstes zu groß.", "invalid_response");
            buffer.Write(chunk, 0, count);
        }
        if (!response.IsSuccessStatusCode)
        {
            string code = response.StatusCode switch { HttpStatusCode.Unauthorized => "unauthorized", HttpStatusCode.Forbidden => "membership_revoked", HttpStatusCode.Gone => "group_deleted", HttpStatusCode.TooManyRequests => "rate_limit", _ => "service_error" };
            try { using var error = JsonDocument.Parse(buffer.ToArray()); if (error.RootElement.ValueKind == JsonValueKind.Object && error.RootElement.TryGetProperty("code", out var value) && value.ValueKind == JsonValueKind.String) code = value.GetString() ?? code; } catch (JsonException) { }
            // Display only known local messages, never provider errors containing request URLs/tokens.
            throw new GroupApiException(ErrorMessage(code), code);
        }
        try
        {
            var snapshot = JsonSerializer.Deserialize<GroupSnapshot>(buffer.ToArray(), GroupJson.Options) ?? throw new JsonException();
            snapshot.Validate(group); return snapshot with { ReceivedAt = System.Diagnostics.Stopwatch.GetTimestamp() };
        }
        catch (JsonException) { throw new GroupApiException("Ungültige Antwort des Gruppendienstes.", "invalid_response"); }
    }
    public static string ErrorMessage(string code) => code switch
    {
        "unauthorized" => "Zugangscode ungültig. Mitgliedschaft wiederherstellen oder erneut anfragen.",
        "membership_revoked" => "Die Mitgliedschaft wurde entfernt oder abgelehnt.",
        "group_deleted" => "Die Gruppe wurde gelöscht.",
        "request_expired" => "Die Beitrittsanfrage ist abgelaufen.",
        "owner_required" => "Diese Aktion ist nur für den Ersteller verfügbar.",
        "invalid_invitation" => "Der Einladungslink ist ungültig oder widerrufen.",
        "group_full" => "Die Gruppe hat bereits 50 bestätigte Mitglieder.",
        "too_many_requests" or "rate_limit" => "Zu viele Anfragen oder kostenloses Kontingent erreicht. Bitte später erneut versuchen.",
        "operation_conflict" => "Diese Anfragekennung wurde bereits verwendet.",
        "invalid_status" => "Diese Anfrage wurde bereits bearbeitet.",
        _ => "Der Gruppendienst ist momentan nicht erreichbar. Bitte später erneut versuchen."
    };
    public void Dispose() => http.Dispose();
}
