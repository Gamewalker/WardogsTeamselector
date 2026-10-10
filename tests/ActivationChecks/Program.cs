using System.Diagnostics;
using WardogsTeamselector.Groups;
using WardogsTeamselector.Platform;

if (args.Length == 3 && args[0] == "--forward")
{
    using var secondary = new InvitationActivationHost(args[1]);
    if (secondary.IsPrimary) throw new Exception("Secondary process must use the existing receiver");
    await secondary.ForwardAsync(args[2]);
    return;
}

foreach (var filename in new[] { "WardogsTeamselector.exe", "WardogsTeamselector-win-x64-with-runtime.exe", "WardogsTeamselector-win-x64-without-runtime.exe", "My portable app.exe" })
{
    var executable = Path.Combine(@"C:\Portable Apps", filename);
    if (InvitationActivationHost.CreateProtocolCommand(executable) != $"\"{executable}\" --join \"%1\"")
        throw new Exception("Protocol command must support renamed release executables and quote paths");
}
foreach (var executable in new string?[] { null, "", @"C:\Program Files\dotnet\dotnet.exe", @"C:\SDK\DOTNET.EXE" })
    if (InvitationActivationHost.CreateProtocolCommand(executable) != null) throw new Exception("SDK host must not be registered");

var channel = "WardogsActivationTest-" + Guid.NewGuid().ToString("N");
// Keep mutex ownership on this thread; async continuations must not release it.
using var primary = new InvitationActivationHost(channel);
if (!primary.IsPrimary) throw new Exception("First process must own the receiver");
var received = new System.Collections.Concurrent.ConcurrentQueue<string>();
var listener = primary.ListenAsync(received.Enqueue);
void Forward(string value)
{
    var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true };
    start.ArgumentList.Add("--forward"); start.ArgumentList.Add(channel); start.ArgumentList.Add(value);
    using var child = Process.Start(start)!;
    if (!child.WaitForExit(10000) || child.ExitCode != 0) throw new Exception("Forwarding process failed");
}
void WaitForCount(int count)
{
    if (!SpinWait.SpinUntil(() => received.Count == count, 5000)) throw new Exception("Activation was not received");
}
var invitation = "https://custom.example:8443/invite/" + GroupMembership.NewId() + "#" + GroupMembership.NewToken();
var activation = "wardogs://join/#" + Uri.EscapeDataString(invitation);
Forward(activation); WaitForCount(1);
if (!received.TryDequeue(out var actual) || actual != activation) throw new Exception("Invitation changed in transit");
Forward("invalid activation");
Forward(activation); WaitForCount(1);
received.TryDequeue(out _);
Forward(""); WaitForCount(1);
if (!received.TryDequeue(out actual) || actual != "") throw new Exception("Ordinary activation changed");
primary.Dispose();
listener.GetAwaiter().GetResult();
Console.WriteLine("ActivationChecks: renamed release protocol commands, existing-process forwarding, invalid input recovery, ordinary launch and shutdown passed.");
