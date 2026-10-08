using System;
using System.Diagnostics;
using System.IO;

namespace WardogsTeamselector.Updates;

public static class UpdateInstaller
{
    public static ProcessStartInfo CreateStartInfo(string script, string job)
    {
        var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe")) {
            UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden
        };
        // A .NET process launched from pwsh inherits PS7 module paths. Windows
        // PowerShell must rebuild its own paths to load Get-FileHash correctly.
        start.Environment.Remove("PSModulePath");
        foreach (string argument in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", script, "-Job", job }) start.ArgumentList.Add(argument);
        return start;
    }
}
