namespace Filament.Demo;

using Filament;

/// <summary>Params record marshalled into the Lua <c>describe</c> call.</summary>
[Scriptable]
public partial record PolicyInput(float HpFraction, float Distance);
