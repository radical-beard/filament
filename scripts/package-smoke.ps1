$ErrorActionPreference = "Stop"
$env:DOTNET_NOLOGO = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

$Root = Resolve-Path (Join-Path $PSScriptRoot "..")
$Version = if ($env:FILAMENT_VERSION) { $env:FILAMENT_VERSION } else { "0.1.0" }
$Feed = New-Item -ItemType Directory -Path (Join-Path ([IO.Path]::GetTempPath()) ("filament-pack-" + [Guid]::NewGuid().ToString("N")))

function Assert-LastExit([string]$Command) {
    if ($LASTEXITCODE -ne 0) {
        throw "$Command failed with exit code $LASTEXITCODE"
    }
}

function Get-ZipEntryNames([string]$PackagePath) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $Zip = [IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        return @($Zip.Entries | ForEach-Object { $_.FullName })
    }
    finally {
        $Zip.Dispose()
    }
}

& dotnet pack (Join-Path $Root "filament.slnx") -c Release -o $Feed.FullName
Assert-LastExit "dotnet pack"

$AbstractionsPkg = Join-Path $Feed.FullName "Filament.Abstractions.$Version.nupkg"
$ConfigPkg = Join-Path $Feed.FullName "Filament.Config.$Version.nupkg"
$CorePkg = Join-Path $Feed.FullName "Filament.Core.$Version.nupkg"
$GodotPkg = Join-Path $Feed.FullName "Filament.Godot.$Version.nupkg"
$DemoPkg = Join-Path $Feed.FullName "Filament.Demo.$Version.nupkg"

foreach ($Package in @($AbstractionsPkg, $ConfigPkg, $CorePkg, $GodotPkg)) {
    if (-not (Test-Path $Package)) { throw "missing package: $Package" }
}
if (Test-Path $DemoPkg) { throw "demo package should not be produced: $DemoPkg" }

$CoreEntries = Get-ZipEntryNames $CorePkg
$GodotEntries = Get-ZipEntryNames $GodotPkg
if ($CoreEntries -notcontains "analyzers/dotnet/cs/Filament.SourceGen.dll") {
    throw "Filament.Core package is missing the source generator analyzer asset"
}
if ($GodotEntries -contains "analyzers/dotnet/cs/Filament.SourceGen.dll") {
    throw "Filament.Godot package should not duplicate the source generator analyzer asset"
}

$CoreConsumer = New-Item -ItemType Directory -Path (Join-Path ([IO.Path]::GetTempPath()) ("filament-core-consumer-" + [Guid]::NewGuid().ToString("N")))
& dotnet new console -o (Join-Path $CoreConsumer.FullName "app") *> $null
Assert-LastExit "dotnet new console"

$CoreApp = Join-Path $CoreConsumer.FullName "app"
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$($Feed.FullName)" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content (Join-Path $CoreApp "nuget.config")
@"
using Filament;
using MoonSharp.Interpreter;

var s = LuaSandbox.Create();
Console.WriteLine(ScriptableMarshal.TryGet<Inp>(out _));
var table = ScriptableMarshal.ToLua(new Inp(0.5f), s);
Console.WriteLine(table.Get("hp_fraction").Number);

[Scriptable]
public partial record Inp(float HpFraction);
"@ | Set-Content (Join-Path $CoreApp "Program.cs")

Push-Location $CoreApp
try {
    $env:NUGET_PACKAGES = Join-Path $CoreConsumer.FullName "packages"
    & dotnet add package Filament.Core --version $Version *> $null
    Assert-LastExit "dotnet add package Filament.Core"
    $CoreOut = & dotnet run
    Assert-LastExit "dotnet run core consumer"
    $CoreOut | ForEach-Object { Write-Host $_ }
    if ($CoreOut[0] -ne "True") { throw "core consumer did not register generated converter" }
    if ($CoreOut[1] -ne "0.5") { throw "core consumer did not marshal hp_fraction" }
}
finally {
    Pop-Location
}

$GodotConsumer = New-Item -ItemType Directory -Path (Join-Path ([IO.Path]::GetTempPath()) ("filament-godot-consumer-" + [Guid]::NewGuid().ToString("N")))
& dotnet new console -o (Join-Path $GodotConsumer.FullName "app") *> $null
Assert-LastExit "dotnet new console"

$GodotApp = Join-Path $GodotConsumer.FullName "app"
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$($Feed.FullName)" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content (Join-Path $GodotApp "nuget.config")
@"
using Filament;
using Godot;
using MoonSharp.Interpreter;

var script = LuaSandbox.Create();
Console.WriteLine(ScriptableMarshal.TryGet<Spatial>(out _));
var table = ScriptableMarshal.ToLua(new Spatial(new Vector3(1, 2, 3), new Color(1, 0, 0)), script);
Console.WriteLine(table.Get("position").Table.Get("z").Number);

[Scriptable]
public partial record Spatial(Vector3 Position, Color Tint);
"@ | Set-Content (Join-Path $GodotApp "Program.cs")

Push-Location $GodotApp
try {
    $env:NUGET_PACKAGES = Join-Path $GodotConsumer.FullName "packages"
    & dotnet add package Filament.Godot --version $Version *> $null
    Assert-LastExit "dotnet add package Filament.Godot"
    & dotnet build -warnaserror
    Assert-LastExit "dotnet build Godot consumer"
    $GodotOut = & dotnet run
    Assert-LastExit "dotnet run Godot consumer"
    $GodotOut | ForEach-Object { Write-Host $_ }
    if ($GodotOut[0] -ne "True") { throw "Godot consumer did not register generated converter" }
    if ($GodotOut[1] -ne "3") { throw "Godot consumer did not marshal Vector3" }
}
finally {
    Pop-Location
}

Write-Host "package smoke passed: $($Feed.FullName)"
