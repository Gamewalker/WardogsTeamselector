param([switch]$Tests, [string]$OutputDirectory = 'dist')
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$localSdk = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
$sdkCommand = if (Test-Path -LiteralPath $localSdk) { $localSdk } else { 'dotnet' }
if ($Tests) {
    foreach ($testProject in @('tests\DetectionChecks\DetectionChecks.csproj', 'tests\JoinedScreenChecks\JoinedScreenChecks.csproj', 'tests\AutomationChecks\AutomationChecks.csproj', 'tests\ConfigurationChecks\ConfigurationChecks.csproj', 'tests\PlatformChecks\PlatformChecks.csproj')) {
        & $sdkCommand run --project (Join-Path $projectRoot $testProject) -c Release
        if ($LASTEXITCODE -ne 0) { throw "Prüfung fehlgeschlagen: $testProject" }
    }
}
$publishDirectory = Join-Path $projectRoot $OutputDirectory
& $sdkCommand publish (Join-Path $projectRoot 'src\WardogsTeamselector\WardogsTeamselector.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o $publishDirectory
if ($LASTEXITCODE -ne 0) { throw 'Veröffentlichung fehlgeschlagen.' }
Write-Output (Join-Path $publishDirectory 'WardogsTeamselector.exe')
