$ErrorActionPreference = 'Stop'
$localSdk = Join-Path $PSScriptRoot '.tools\dotnet\dotnet.exe'
$sdkCommand = if (Test-Path -LiteralPath $localSdk) { $localSdk } else { 'dotnet' }
$project = Join-Path $PSScriptRoot 'src\WardogsTeamselector\WardogsTeamselector.csproj'

& $sdkCommand run --project $project -c Debug
exit $LASTEXITCODE
