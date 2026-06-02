---
name: filament
description: Work on the Filament .NET/Godot Lua scripting framework with release-grade verification, package checks, and playtest MCP routing.
---

# Filament Skill

Use this skill when working in the Filament repository or when the user asks to
prepare, verify, package, or agent-test Filament.

## Workflow

1. Inspect the relevant project surface before editing.
2. Keep `Filament.Abstractions` dependency-light: no MoonSharp or Godot.
3. Keep `Filament.Core` engine-free: no Godot.
4. If demo copy behavior changes, run `scripts/demo-smoke.sh`.
5. If package or source-generator behavior changes, run `scripts/package-smoke.sh`.
6. If playtest or MCP setup changes, run `scripts/agent-smoke.sh`.
7. Before claiming distribution readiness, run `scripts/verify.sh`.

## Playtest MCP

Use the `filament-playtest` MCP server when a change needs human feedback about
feel, timing, input responsiveness, or discoverability. Always pass a briefing
with what changed and controls to try.
