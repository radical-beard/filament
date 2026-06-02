$ErrorActionPreference = "Stop"
$env:DOTNET_NOLOGO = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
$Verifier = Join-Path (Join-Path (Join-Path $Root "tools") "Filament.Verify") "Filament.Verify.csproj"

& dotnet run --project $Verifier
if ($LASTEXITCODE -ne 0) { throw "verification failed" }
