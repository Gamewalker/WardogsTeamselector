using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using WardogsTeamselector.Groups;

namespace WardogsTeamselector.Platform;

// One receiver per user/session, regardless of which portable EXE Windows starts.
public sealed class InvitationActivationHost : IDisposable
{
    private readonly string pipeName;
    private readonly Mutex mutex;
    private readonly CancellationTokenSource lifetime = new();
    private bool disposed;
    public bool IsPrimary { get; }

    public InvitationActivationHost(string channel = "WardogsInvitation")
    {
        pipeName = channel + "-" + WindowsIdentity.GetCurrent().User!.Value + "-" + System.Diagnostics.Process.GetCurrentProcess().SessionId;
        mutex = new Mutex(false, "Local\\" + pipeName);
        try { IsPrimary = mutex.WaitOne(0); }
        catch (AbandonedMutexException) { IsPrimary = true; }
    }

    public static void RegisterProtocol()
    {
        var executable = Environment.ProcessPath;
        // dotnet run points at the SDK host, which must never become the handler.
        if (executable == null || !Path.GetFileNameWithoutExtension(executable).Equals("WardogsTeamselector", StringComparison.OrdinalIgnoreCase)) return;
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\wardogs");
        key.SetValue("", "URL:Wardogs group invitation");
        key.SetValue("URL Protocol", "");
        using var command = key.CreateSubKey(@"shell\open\command");
        command.SetValue("", $"\"{executable}\" --join \"%1\"");
    }

    public async Task ForwardAsync(string invitation)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        await pipe.ConnectAsync(timeout.Token);
        await pipe.WriteAsync(Encoding.UTF8.GetBytes(invitation), timeout.Token);
        await pipe.FlushAsync(timeout.Token);
    }

    public async Task ListenAsync(Action<string> receive)
    {
        while (!lifetime.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(lifetime.Token);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                var buffer = new byte[4097];
                var count = 0;
                int read;
                while (count < buffer.Length && (read = await pipe.ReadAsync(buffer.AsMemory(count), timeout.Token)) != 0) count += read;
                if (count == 0) receive(""); // Ordinary second launch brings the window forward.
                else if (count <= 4096)
                {
                    var value = Encoding.UTF8.GetString(buffer, 0, count);
                    GroupInvitationActivation.Parse(value);
                    receive(value);
                }
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException or ArgumentException) { }
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        lifetime.Cancel();
        if (IsPrimary) mutex.ReleaseMutex();
        mutex.Dispose();
    }
}
