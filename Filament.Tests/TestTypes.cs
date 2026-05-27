namespace Filament.Tests;

using System.Collections.Generic;
using Filament;

public enum Phase { Stalking, Frenzied }

/// <summary>Exercises every marshalling shape: primitives, enum, list, Option.</summary>
[Scriptable]
public partial record AttackPattern(
    string Id,
    int Reps,
    float Cooldown,
    Phase Phase,
    IReadOnlyList<string> Tags,
    Option<float> Weight);

/// <summary>Nested [Scriptable] member.</summary>
[Scriptable]
public partial record Wrapper(string Name, AttackPattern Inner);

/// <summary>Params record for dispatch tests.</summary>
[Scriptable]
public partial record AddArgs(int A, int B);

/// <summary>For the long-precision coercion test.</summary>
[Scriptable]
public partial record LongHolder(long Big);
