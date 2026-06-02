$ErrorActionPreference = "Stop"
$env:DOTNET_NOLOGO = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
$Project = Join-Path (Join-Path $Root "demo") "Filament.Demo.csproj"
$Log = New-TemporaryFile

& dotnet run --project $Project -- --ticks 3 *> $Log
if ($LASTEXITCODE -ne 0) {
    Get-Content $Log
    throw "demo smoke failed with exit code $LASTEXITCODE"
}

$Text = Get-Content $Log -Raw
Write-Host $Text

if ($Text -notmatch "Loaded \d+ module\(s\)") { throw "demo did not report loaded modules" }
if ($Text -notmatch "describe\(hp=") { throw "demo did not call policy.describe" }
if ($Text -match "lua root does not exist") { throw "demo reported missing Lua root" }
