# Distribution

This document defines the minimum checks before handing Filament to another
developer.

## Required commands

Run these from the repository root:

Any platform with .NET:

```sh
dotnet run --project tools/Filament.Verify/Filament.Verify.csproj
```

macOS/Linux:

```sh
scripts/verify.sh
```

Windows/PowerShell:

```powershell
./scripts/verify.ps1
```

The verifier runs restore, warnings-as-errors build, formatting verification,
tests, demo smoke, agent smoke, package packing, and fresh package consumers.

## Clean checkout check

Use an archived checkout so generated local files cannot mask missing inputs:

```sh
tmp=$(mktemp -d /tmp/filament-dist.XXXXXX)
git archive HEAD | tar -x -C "$tmp"
dotnet test "$tmp/filament.slnx"
dotnet run --project "$tmp/demo/Filament.Demo.csproj" -- --ticks 3
```

The demo should print loaded modules and repeated `describe(...)` lines. It
should not print `lua root does not exist`.

## Package check

Pack into a temporary local feed:

macOS/Linux:

```sh
scripts/package-smoke.sh
```

Windows/PowerShell:

```powershell
./scripts/package-smoke.ps1
```

The script verifies package contents, confirms `Filament.Demo` is not packaged,
and builds fresh `Filament.Core` and `Filament.Godot` package consumers with an
isolated NuGet cache.

The core consumer check is equivalent to:

```sh
tmp=$(mktemp -d /tmp/filament-consumer.XXXXXX)
dotnet new console -o "$tmp/app"
cat > "$tmp/app/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$feed" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF
cd "$tmp/app"
NUGET_PACKAGES="$tmp/packages" dotnet add package Filament.Core --version 0.1.0
cat > Program.cs <<'EOF'
using Filament;

Console.WriteLine(ScriptableMarshal.TryGet<Inp>(out _));

[Scriptable]
public partial record Inp(float HpFraction);
EOF
NUGET_PACKAGES="$tmp/packages" dotnet run
```

Expected output:

```text
True
```

## Public release gaps

Before publishing to a public NuGet feed, review:

- Package IDs and ownership.
- Versioning policy.
- Changelog/release notes.
- Whether the current all-rights-reserved license file should be replaced with
  an open-source license.
