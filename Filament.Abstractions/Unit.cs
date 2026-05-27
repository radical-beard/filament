namespace Filament;

/// <summary>The "no meaningful value" type — for <c>Result&lt;Unit, E&gt;</c> on
/// calls whose success carries nothing (void-style dispatch).</summary>
public readonly struct Unit
{
    public static readonly Unit Value = default;
}
