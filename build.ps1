param([switch]$Tests, [string]$OutputDirectory = 'dist', [long]$ReleaseBuild = 0)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$localSdk = Join-Path $projectRoot '.tools\dotnet\dotnet.exe'
$sdkCommand = if (Test-Path -LiteralPath $localSdk) { $localSdk } else { 'dotnet' }
if ($Tests) {
    foreach ($testProject in @('tests\DetectionChecks\DetectionChecks.csproj', 'tests\JoinedScreenChecks\JoinedScreenChecks.csproj', 'tests\AutomationChecks\AutomationChecks.csproj', 'tests\ConfigurationChecks\ConfigurationChecks.csproj', 'tests\UpdateChecks\UpdateChecks.csproj', 'tests\PlatformChecks\PlatformChecks.csproj')) {
        & $sdkCommand run --project (Join-Path $projectRoot $testProject) -c Release
        if ($LASTEXITCODE -ne 0) { throw "Prüfung fehlgeschlagen: $testProject" }
    }
}
$publishRoot = Join-Path $projectRoot $OutputDirectory
foreach ($variant in @('with-runtime', 'without-runtime')) {
    $selfContained = if ($variant -eq 'with-runtime') { 'true' } else { 'false' }
    $publishDirectory = Join-Path $publishRoot $variant
    & $sdkCommand publish (Join-Path $projectRoot 'src\WardogsTeamselector\WardogsTeamselector.csproj') -c Release -r win-x64 --self-contained $selfContained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false "-p:ReleaseBuild=$ReleaseBuild" "-p:UpdateVariant=$variant" -o $publishDirectory
    if ($LASTEXITCODE -ne 0) { throw "Veröffentlichung fehlgeschlagen: $variant" }
    $asset = Join-Path $publishRoot "WardogsTeamselector-win-x64-$variant.exe"
    Copy-Item -LiteralPath (Join-Path $publishDirectory 'WardogsTeamselector.exe') -Destination $asset -Force
    Write-Output $asset
}
