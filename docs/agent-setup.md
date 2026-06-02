# Agent Setup

Filament includes a playtest MCP server for agent-assisted iteration. The server
launches a build, waits for a human to play and close it, opens a browser survey,
then returns structured answers to the agent.

## Project MCP config

The repository-level `.mcp.json` registers:

```json
{
  "mcpServers": {
    "filament-playtest": {
      "command": "uv",
      "args": ["run", "--script", "playtest/server.py"]
    }
  }
}
```

This assumes the MCP client launches the command from the repository root. If
your client launches from another directory, replace `playtest/server.py` with an
absolute path to this checkout.

Validate the agent setup with:

macOS/Linux:

```sh
scripts/agent-smoke.sh
```

Windows/PowerShell:

```powershell
./scripts/agent-smoke.ps1
```

## Launch config

`filament.toml` defines the default launch command:

```toml
[launch]
command = "dotnet run --project demo/Filament.Demo.csproj"
```

Agents should call the `playtest` tool with `launch_dir` set to the repository
root and should pass a briefing. The briefing matters because the human needs to
know what changed and what controls or verbs to try.

## Good questions

Ask about things automation cannot judge:

- timing feel
- hit confirmation
- input latency
- whether a new verb is discoverable
- whether a tuning change feels too weak or too strong

Prefer questions with fixed choices and one optional detail field so the result
closes a concrete loop.

## Optional Codex skill

The repo includes a skill template at `.codex/skills/filament/SKILL.md`. Codex
loads installed skills from `~/.codex/skills`, so install it when you want
`$filament` available globally.

macOS/Linux symlink:

```sh
mkdir -p ~/.codex/skills
rm -rf ~/.codex/skills/filament
ln -s "$PWD/.codex/skills/filament" ~/.codex/skills/filament
```

Windows PowerShell copy:

```powershell
$SkillRoot = Join-Path $HOME ".codex\skills"
New-Item -ItemType Directory -Force -Path $SkillRoot | Out-Null
$Target = Join-Path $SkillRoot "filament"
if (Test-Path $Target) { Remove-Item -Recurse -Force $Target }
Copy-Item -Recurse ".codex\skills\filament" $Target
```

The symlink tracks repository changes automatically. The copy works on locked
down machines, but rerun it after changing `.codex/skills/filament`.

The skill routes agents toward the repo verification scripts and the playtest
MCP workflow.
