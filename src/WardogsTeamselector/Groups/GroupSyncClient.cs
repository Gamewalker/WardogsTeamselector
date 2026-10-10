using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WardogsTeamselector.Groups;

public sealed class GroupSyncClient : IDisposable
{
    private readonly GroupApiClient api;
    private readonly CancellationTokenSource lifetime = new();
    private readonly GroupMembership group;
    private Task? worker;
    public event Action<GroupSnapshot>? Updated;
    public event Action<bool, string>? ConnectionChanged;
    public event Action<GroupApiException>? AccessRevoked;
    public GroupSyncClient(GroupApiClient api, GroupMembership group) { this.api = api; this.group = group; }
    public void Start() { if (worker != null) throw new InvalidOperationException("Sync läuft bereits."); worker = Task.Run(RunAsync); }
    private async Task RunAsync()
    {
        int failures = 0;
        while (!lifetime.IsCancellationRequested)
        {
            try
            {
                // HTTP authorization distinguishes permanent revocation from transient socket failures.
                var initial = await api.GetAsync(group, lifetime.Token).ConfigureAwait(false);
                Updated?.Invoke(initial);
                using var socket = new ClientWebSocket();
                socket.Options.SetRequestHeader("Authorization", "Bearer " + group.Token);
                socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(25);
                var address = new UriBuilder(group.ServiceUrl) { Scheme = group.ServiceUrl.StartsWith("https:", StringComparison.Ordinal) ? "wss" : "ws", Path = $"/v1/groups/{group.GroupId}/socket" };
                using (var connect = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token))
                { connect.CancelAfter(TimeSpan.FromSeconds(10)); await socket.ConnectAsync(address.Uri, connect.Token).ConfigureAwait(false); }
                // A connected socket alone is not authorization; its first snapshot is required.
                failures = 0;
                using var connection = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                var syncTask = SyncAsync(socket, connection.Token);
                try { await ReceiveAsync(socket, connection.Token).ConfigureAwait(false); }
                finally { connection.Cancel(); socket.Abort(); try { await syncTask.ConfigureAwait(false); } catch (OperationCanceledException) { } catch (WebSocketException) { } }
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { break; }
            catch (GroupApiException ex) when (ex.AccessRevoked) { ConnectionChanged?.Invoke(false, ex.Message); AccessRevoked?.Invoke(ex); break; }
            catch (Exception) when (!lifetime.IsCancellationRequested) { }
            if (lifetime.IsCancellationRequested) break;
            ConnectionChanged?.Invoke(false, "Verbindung unterbrochen · Gruppenlauf pausiert");
            failures++;
            var delay = Math.Min(60, Math.Pow(2, Math.Min(failures, 6))) + Random.Shared.NextDouble();
            try { await Task.Delay(TimeSpan.FromSeconds(delay), lifetime.Token).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
        }
    }
    private static async Task SyncAsync(ClientWebSocket socket, CancellationToken cancellation)
    {
        var message = Encoding.UTF8.GetBytes("{\"type\":\"sync\"}");
        while (!cancellation.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), cancellation).ConfigureAwait(false);
            await socket.SendAsync(message, WebSocketMessageType.Text, true, cancellation).ConfigureAwait(false);
        }
    }
    private async Task ReceiveAsync(ClientWebSocket socket, CancellationToken cancellation)
    {
        var chunk = new byte[4096];
        while (!cancellation.IsCancellationRequested)
        {
            using var message = new MemoryStream();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            // 15s until the next sync + 10s response timeout.
            deadline.CancelAfter(TimeSpan.FromSeconds(25));
            WebSocketReceiveResult received;
            do
            {
                received = await socket.ReceiveAsync(new ArraySegment<byte>(chunk), deadline.Token).ConfigureAwait(false);
                if (received.MessageType == WebSocketMessageType.Close)
                {
                    if (received.CloseStatus == (WebSocketCloseStatus)4003) throw new GroupApiException("Mitgliedschaft beendet.", "membership_revoked");
                    ConnectionChanged?.Invoke(false, "Verbindung unterbrochen"); return;
                }
                if (received.MessageType != WebSocketMessageType.Text || message.Length + received.Count > 65536) throw new GroupApiException("Ungültige Gruppennachricht.", "invalid_response");
                message.Write(chunk, 0, received.Count);
            } while (!received.EndOfMessage);
            var snapshot = JsonSerializer.Deserialize<GroupSnapshot>(message.ToArray(), GroupJson.Options) ?? throw new GroupApiException("Leere Gruppennachricht.", "invalid_response");
            snapshot.Validate(group); Updated?.Invoke(snapshot with { ReceivedAt = System.Diagnostics.Stopwatch.GetTimestamp() }); ConnectionChanged?.Invoke(true, "Verbunden · Zustandsprüfung alle 15 Sekunden");
        }
    }
    public void Dispose() { lifetime.Cancel(); /* Do not block WPF waiting for callbacks. */ }
}
