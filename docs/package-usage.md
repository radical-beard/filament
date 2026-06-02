# Package Usage

Use `Filament.Core` for engine-free Lua behavior scripting:

```sh
dotnet add package Filament.Core --version 0.1.0
```

Then define scriptable records in your game/tool assembly:

```csharp
using Filament;

[Scriptable]
public partial record PolicyInput(float HpFraction, float Distance);
```

The `Filament.Core` package includes the analyzer/source generator that registers
the converter for `PolicyInput` at module-load time. You do not need to reference
`Filament.SourceGen` directly when using packages.

Use `Filament.Config` for TOML world files:

```sh
dotnet add package Filament.Config --version 0.1.0
```

Use `Filament.Godot` for Godot 4.6 .NET projects:

```sh
dotnet add package Filament.Godot --version 0.1.0
```

`Filament.Godot` depends on `Filament.Core`, so the source generator arrives
through the package graph once. The package smoke test verifies this to avoid
duplicate generator execution.
