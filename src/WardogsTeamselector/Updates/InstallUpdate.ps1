param([Parameter(Mandatory)][string]$Job)
$ErrorActionPreference = 'Stop'
$jobData = Get-Content -LiteralPath $Job -Raw -Encoding UTF8 | ConvertFrom-Json
try {
    $parent = Get-Process -Id $jobData.ProcessId -ErrorAction SilentlyContinue
    if ($parent) { $parent.WaitForExit() }
    $hasher = [Security.Cryptography.SHA256]::Create()
    $mutexKey = [BitConverter]::ToString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($jobData.Target.ToLowerInvariant()))).Replace('-', '')
    $hasher.Dispose()
    $mutex = [Threading.Mutex]::new($false, 'Local\WardogsUpdate-' + $mutexKey)
    if (!$mutex.WaitOne(30000)) { throw 'Another update is already running.' }
    $locked = $true
    if ((Get-FileHash -LiteralPath $jobData.Target -Algorithm SHA256).Hash -ne $jobData.TargetSha256) { throw 'The EXE has already changed. Update not installed.' }
    if ((Get-FileHash -LiteralPath $jobData.Source -Algorithm SHA256).Hash -ne $jobData.Sha256) { throw 'Update checksum mismatch' }
    # Stage on the target volume; File.Replace preserves the original on failure.
    $candidate = $jobData.Target + '.' + [Guid]::NewGuid().ToString('N') + '.update'
    Copy-Item -LiteralPath $jobData.Source -Destination $candidate
    if ((Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash -ne $jobData.Sha256) { throw 'Staged update checksum mismatch' }
    $installed = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        try {
            [IO.File]::Replace($candidate, $jobData.Target, $jobData.Backup, $true)
            $installed = $true
            break
        } catch { Start-Sleep -Milliseconds 1000 }
    }
    if (!$installed) { throw 'EXE locked or directory not writable. Update not installed.' }
    Set-Content -LiteralPath $jobData.Result -Value 'Update erfolgreich installiert.' -Encoding UTF8
} catch {
    Set-Content -LiteralPath $jobData.Result -Value ('Update fehlgeschlagen: ' + $_.Exception.Message) -Encoding UTF8
} finally {
    if (Test-Path -LiteralPath $jobData.Source) { Remove-Item -LiteralPath $jobData.Source }
    if ($candidate -and (Test-Path -LiteralPath $candidate)) { Remove-Item -LiteralPath $candidate }
    Remove-Item -LiteralPath $Job
    Remove-Item -LiteralPath $jobData.Script
    if ($locked) { $mutex.ReleaseMutex() }
    if ($mutex) { $mutex.Dispose() }
}
