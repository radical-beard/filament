#!/usr/bin/env bash
set -euo pipefail
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG="$(mktemp "${TMPDIR:-/tmp}/filament-demo.XXXXXX")"

dotnet run --project "$ROOT/demo/Filament.Demo.csproj" -- --ticks 3 >"$LOG" 2>&1
cat "$LOG"

grep -Eq "Loaded [0-9]+ module\\(s\\)" "$LOG"
grep -q "describe(hp=" "$LOG"
! grep -q "lua root does not exist" "$LOG"
