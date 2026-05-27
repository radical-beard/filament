namespace Filament.Godot.Tests;

using System.Collections.Generic;
using Filament;
using global::Godot;
using MoonSharp.Interpreter;

[Scriptable]
public partial record Spatial(
    Vector3 Position,
    Vector2 Uv,
    Color Tint,
    IReadOnlyList<Vector3> Path);

[TestFixture]
public class GodotMarshalTests
{
    [Test]
    public void Vectors_And_Color_RoundTrip()
    {
        var s = LuaSandbox.Create();
        var original = new Spatial(
            new Vector3(1, 2, 3),
            new Vector2(0.5f, 0.25f),
            new Color(0.1f, 0.2f, 0.3f, 0.4f),
            new List<Vector3> { new(4, 5, 6) });

        var table = ScriptableMarshal.ToLua(original, s);

        // plain { x, y, z } / { r, g, b, a } tables; lists 1-based.
        Assert.That(table.Get("position").Table.Get("x").Number, Is.EqualTo(1));
        Assert.That(table.Get("position").Table.Get("z").Number, Is.EqualTo(3));
        Assert.That(table.Get("path").Table.Get(1).Table.Get("y").Number, Is.EqualTo(5));

        var back = ScriptableMarshal.FromLua<Spatial>(DynValue.NewTable(table));
        Assert.That(back.IsOk, Is.True);
        back.TryGet(out var v);

        // float -> double -> float is lossless, so equality is exact.
        Assert.That(v.Position, Is.EqualTo(new Vector3(1, 2, 3)));
        Assert.That(v.Uv, Is.EqualTo(new Vector2(0.5f, 0.25f)));
        Assert.That(v.Tint, Is.EqualTo(new Color(0.1f, 0.2f, 0.3f, 0.4f)));
        Assert.That(v.Path[0], Is.EqualTo(new Vector3(4, 5, 6)));
    }

    [Test]
    public void Color_DefaultsAlphaToOpaque()
    {
        var s = LuaSandbox.Create();
        var t = new Table(s);
        var c = new Table(s);
        c.Set("r", DynValue.NewNumber(1));
        c.Set("g", DynValue.NewNumber(0));
        c.Set("b", DynValue.NewNumber(0)); // no alpha
        var r = GodotMarshal.AsColor(DynValue.NewTable(c), "T", "tint");
        Assert.That(r.IsOk, Is.True);
        r.TryGet(out var color);
        Assert.That(color.A, Is.EqualTo(1f));
    }

    [Test]
    public void IncompleteVector_IsFieldCoercion()
    {
        var s = LuaSandbox.Create();
        var pos = new Table(s);
        pos.Set("x", DynValue.NewNumber(1));
        pos.Set("y", DynValue.NewNumber(2)); // missing z
        var r = GodotMarshal.AsVector3(DynValue.NewTable(pos), "T", "position");
        Assert.That(r.IsErr, Is.True);
        r.TryGetError(out var e);
        Assert.That(e.Kind, Is.EqualTo(LuaErrorKind.FieldCoercion));
    }
}
