#!/usr/bin/env bash
set -euo pipefail
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

dotnet run --project "$ROOT/tools/Filament.Verify/Filament.Verify.csproj"
