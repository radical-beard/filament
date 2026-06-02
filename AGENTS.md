# Filament Agent Guide

This repository is a source-first .NET/Godot framework. Keep changes small,
tested, and distribution-aware.

## Project Shape

- `Filament.Abstractions` is dependency-light shared vocabulary.
- `Filament.Core` is engine-free Lua runtime and hot reload.
- `Filament.SourceGen` is the analyzer/source generator for `[Scriptable]`.
- `Filament.Config` is engine-free TOML world config support.
- `Filament.Godot` is the Godot 4.6 .NET adapter.
- `demo/` is the first-run smoke test.
- `playtest/` is the MCP human-feedback harness.

## Rules

- Do not add dependencies unless the task explicitly requires them.
- Do not commit generated caches, browser traces, `bin/`, or `obj/`.
- Keep `Filament.Core` free of Godot references.
- Keep `Filament.Abstractions` free of MoonSharp and Godot references.
- If `[Scriptable]` packaging changes, verify a temporary package consumer.
- If demo copy behavior changes, verify a clean archived checkout can run it.

## Verification

Use the smallest meaningful subset while iterating, then finish with:

```sh
dotnet build filament.slnx -warnaserror
dotnet format filament.slnx --verify-no-changes --verbosity minimal
dotnet test filament.slnx
```

For release/distribution work, also run the checks in `docs/distribution.md`.
