#!/usr/bin/env bash
set -euo pipefail
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${FILAMENT_VERSION:-0.1.0}"
FEED="$(mktemp -d "${TMPDIR:-/tmp}/filament-pack.XXXXXX")"

dotnet pack "$ROOT/filament.slnx" -c Release -o "$FEED"

CORE_PKG="$FEED/Filament.Core.$VERSION.nupkg"
GODOT_PKG="$FEED/Filament.Godot.$VERSION.nupkg"

test -f "$FEED/Filament.Abstractions.$VERSION.nupkg"
test -f "$FEED/Filament.Config.$VERSION.nupkg"
test -f "$CORE_PKG"
test -f "$GODOT_PKG"
test ! -f "$FEED/Filament.Demo.$VERSION.nupkg"

CORE_LIST="$(mktemp "${TMPDIR:-/tmp}/filament-core-package.XXXXXX")"
GODOT_LIST="$(mktemp "${TMPDIR:-/tmp}/filament-godot-package.XXXXXX")"
unzip -l "$CORE_PKG" >"$CORE_LIST"
unzip -l "$GODOT_PKG" >"$GODOT_LIST"
grep -q "analyzers/dotnet/cs/Filament.SourceGen.dll" "$CORE_LIST"
! grep -q "analyzers/dotnet/cs/Filament.SourceGen.dll" "$GODOT_LIST"

CORE_CONSUMER="$(mktemp -d "${TMPDIR:-/tmp}/filament-core-consumer.XXXXXX")"
dotnet new console -o "$CORE_CONSUMER/app" >/dev/null
cat > "$CORE_CONSUMER/app/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$FEED" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF
cat > "$CORE_CONSUMER/app/Program.cs" <<'EOF'
using Filament;
using MoonSharp.Interpreter;

var s = LuaSandbox.Create();
Console.WriteLine(ScriptableMarshal.TryGet<Inp>(out _));
var table = ScriptableMarshal.ToLua(new Inp(0.5f), s);
Console.WriteLine(table.Get("hp_fraction").Number);

[Scriptable]
public partial record Inp(float HpFraction);
EOF
(
  cd "$CORE_CONSUMER/app"
  NUGET_PACKAGES="$CORE_CONSUMER/packages" dotnet add package Filament.Core --version "$VERSION" >/dev/null
  OUT="$(NUGET_PACKAGES="$CORE_CONSUMER/packages" dotnet run)"
  printf '%s\n' "$OUT"
  grep -qx "True" <<<"$(printf '%s\n' "$OUT" | sed -n '1p')"
  grep -qx "0.5" <<<"$(printf '%s\n' "$OUT" | sed -n '2p')"
)

GODOT_CONSUMER="$(mktemp -d "${TMPDIR:-/tmp}/filament-godot-consumer.XXXXXX")"
dotnet new console -o "$GODOT_CONSUMER/app" >/dev/null
cat > "$GODOT_CONSUMER/app/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$FEED" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF
cat > "$GODOT_CONSUMER/app/Program.cs" <<'EOF'
using Filament;
using Godot;
using MoonSharp.Interpreter;

var script = LuaSandbox.Create();
Console.WriteLine(ScriptableMarshal.TryGet<Spatial>(out _));
var table = ScriptableMarshal.ToLua(new Spatial(new Vector3(1, 2, 3), new Color(1, 0, 0)), script);
Console.WriteLine(table.Get("position").Table.Get("z").Number);

[Scriptable]
public partial record Spatial(Vector3 Position, Color Tint);
EOF
(
  cd "$GODOT_CONSUMER/app"
  NUGET_PACKAGES="$GODOT_CONSUMER/packages" dotnet add package Filament.Godot --version "$VERSION" >/dev/null
  NUGET_PACKAGES="$GODOT_CONSUMER/packages" dotnet build -warnaserror
  OUT="$(NUGET_PACKAGES="$GODOT_CONSUMER/packages" dotnet run)"
  printf '%s\n' "$OUT"
  grep -qx "True" <<<"$(printf '%s\n' "$OUT" | sed -n '1p')"
  grep -qx "3" <<<"$(printf '%s\n' "$OUT" | sed -n '2p')"
)

echo "package smoke passed: $FEED"
