#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PYCACHE="$(mktemp -d "${TMPDIR:-/tmp}/filament-pycache.XXXXXX")"

python3 -c 'import sys; raise SystemExit(0 if sys.version_info >= (3, 11) else "Python 3.11+ is required for playtest agent checks")'
python3 -m json.tool "$ROOT/.mcp.json" >/dev/null
python3 -m json.tool "$ROOT/global.json" >/dev/null
PYTHONPYCACHEPREFIX="$PYCACHE" python3 -m py_compile \
  "$ROOT/playtest/preview.py" \
  "$ROOT/playtest/selftest.py" \
  "$ROOT/playtest/server.py" \
  "$ROOT/playtest/survey.py"
PYTHONPYCACHEPREFIX="$PYCACHE" python3 "$ROOT/playtest/preview.py" >/dev/null
uv --version >/dev/null

grep -q '"filament-playtest"' "$ROOT/.mcp.json"
grep -q '"playtest/server.py"' "$ROOT/.mcp.json"
