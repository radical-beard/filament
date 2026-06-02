$ErrorActionPreference = "Stop"

$Root = Resolve-Path (Join-Path $PSScriptRoot "..")

function Assert-LastExit([string]$Command) {
    if ($LASTEXITCODE -ne 0) {
        throw "$Command failed with exit code $LASTEXITCODE"
    }
}

function Test-Python311([string]$Python) {
    & $Python -c "import sys; raise SystemExit(0 if sys.version_info >= (3, 11) else 1)" *> $null
    return $LASTEXITCODE -eq 0
}

function Resolve-Python {
    foreach ($Name in @("python", "python3")) {
        $Command = Get-Command $Name -ErrorAction SilentlyContinue
        if ($Command -and (Test-Python311 $Command.Source)) {
            return $Command.Source
        }
    }

    throw "Python 3.11+ is required for playtest agent checks"
}

$Python = Resolve-Python
$env:PYTHONPYCACHEPREFIX = Join-Path ([IO.Path]::GetTempPath()) ("filament-pycache-" + [Guid]::NewGuid().ToString("N"))
& uv --version *> $null
Assert-LastExit "uv --version"

& $Python -m json.tool (Join-Path $Root ".mcp.json") *> $null
Assert-LastExit "python -m json.tool .mcp.json"
& $Python -m json.tool (Join-Path $Root "global.json") *> $null
Assert-LastExit "python -m json.tool global.json"
& $Python -m py_compile `
    (Join-Path (Join-Path $Root "playtest") "preview.py") `
    (Join-Path (Join-Path $Root "playtest") "selftest.py") `
    (Join-Path (Join-Path $Root "playtest") "server.py") `
    (Join-Path (Join-Path $Root "playtest") "survey.py")
Assert-LastExit "python -m py_compile playtest scripts"
& $Python (Join-Path (Join-Path $Root "playtest") "preview.py") *> $null
Assert-LastExit "python playtest/preview.py"

$McpJson = Get-Content (Join-Path $Root ".mcp.json") -Raw
if ($McpJson -notmatch '"filament-playtest"') { throw ".mcp.json does not register filament-playtest" }
if ($McpJson -notmatch '"playtest/server.py"') { throw ".mcp.json does not use the repo-relative server path" }
