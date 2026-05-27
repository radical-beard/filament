namespace Filament.Tests;

using System.Collections.Generic;
using Filament;
using MoonSharp.Interpreter;

[TestFixture]
public class MarshalTests
{
    private static Script NewScript() => LuaSandbox.Create();

    [Test]
    public void RoundTrips_AllShapes()
    {
        var s = NewScript();
        var original = new AttackPattern(
            "jab", 3, 0.5f, Phase.Frenzied,
            new List<string> { "a", "b" }, Option.Some(2.5f));

        var table = ScriptableMarshal.ToLua(original, s);

        // snake_case keys, enum-as-string, 1-based list.
        Assert.That(table.Get("id").String, Is.EqualTo("jab"));
        Assert.That(table.Get("reps").Number, Is.EqualTo(3));
        Assert.That(table.Get("phase").String, Is.EqualTo("Frenzied"));
        Assert.That(table.Get("tags").Table.Get(1).String, Is.EqualTo("a"));
        Assert.That(table.Get("tags").Table.Get(2).String, Is.EqualTo("b"));
        Assert.That(table.Get("weight").Number, Is.EqualTo(2.5));

        var back = ScriptableMarshal.FromLua<AttackPattern>(DynValue.NewTable(table));
        Assert.That(back.IsOk, Is.True);
        back.TryGet(out var v);
        Assert.That(v.Id, Is.EqualTo("jab"));
        Assert.That(v.Reps, Is.EqualTo(3));
        Assert.That(v.Cooldown, Is.EqualTo(0.5f));
        Assert.That(v.Phase, Is.EqualTo(Phase.Frenzied));
        Assert.That(v.Tags, Is.EqualTo(new[] { "a", "b" }));
        Assert.That(v.Weight.IsSome, Is.True);
        v.Weight.TryGet(out var w);
        Assert.That(w, Is.EqualTo(2.5f));
    }

    [Test]
    public void Option_AbsentKey_IsNone_AndOmittedOnWrite()
    {
        var s = NewScript();
        var original = new AttackPattern("x", 1, 0f, Phase.Stalking, new List<string>(), Option<float>.None);

        var table = ScriptableMarshal.ToLua(original, s);
        Assert.That(ScriptableMarshal.IsAbsent(table.Get("weight")), Is.True, "None must omit the key");

        var back = ScriptableMarshal.FromLua<AttackPattern>(DynValue.NewTable(table));
        back.TryGet(out var v);
        Assert.That(v.Weight.IsNone, Is.True);
    }

    [Test]
    public void Nested_Scriptable_RoundTrips()
    {
        var s = NewScript();
        var inner = new AttackPattern("k", 2, 1f, Phase.Stalking, new List<string> { "t" }, Option.Some(1f));
        var original = new Wrapper("boss", inner);

        var table = ScriptableMarshal.ToLua(original, s);
        Assert.That(table.Get("inner").Table.Get("id").String, Is.EqualTo("k"));

        var back = ScriptableMarshal.FromLua<Wrapper>(DynValue.NewTable(table));
        Assert.That(back.IsOk, Is.True);
        back.TryGet(out var v);
        Assert.That(v.Inner.Id, Is.EqualTo("k"));
    }

    [Test]
    public void FromLua_NonTable_IsTypeMismatch_NotThrow()
    {
        var r = ScriptableMarshal.FromLua<AttackPattern>(DynValue.NewNumber(5));
        Assert.That(r.IsErr, Is.True);
        r.TryGetError(out var e);
        Assert.That(e.Kind, Is.EqualTo(LuaErrorKind.TypeMismatch));
    }

    [Test]
    public void FromLua_MissingRequiredField_IsErr()
    {
        var s = NewScript();
        var t = new Table(s);
        t.Set("reps", DynValue.NewNumber(1)); // no "id"
        var r = ScriptableMarshal.FromLua<AttackPattern>(DynValue.NewTable(t));
        Assert.That(r.IsErr, Is.True);
        r.TryGetError(out var e);
        Assert.That(e.Kind, Is.EqualTo(LuaErrorKind.MissingRequiredField));
        Assert.That(e.Member, Is.EqualTo("id"));
    }

    [Test]
    public void FromLua_WrongFieldType_IsFieldCoercion()
    {
        var s = NewScript();
        var t = new Table(s);
        t.Set("id", DynValue.NewNumber(7)); // should be string
        var r = ScriptableMarshal.FromLua<AttackPattern>(DynValue.NewTable(t));
        Assert.That(r.IsErr, Is.True);
        r.TryGetError(out var e);
        Assert.That(e.Kind, Is.EqualTo(LuaErrorKind.FieldCoercion));
        Assert.That(e.Member, Is.EqualTo("id"));
    }

    [Test]
    public void FromLua_BadEnum_IsFieldCoercion()
    {
        var s = NewScript();
        var t = MinimalAttackTable(s);
        t.Set("phase", DynValue.NewString("Nope"));
        var r = ScriptableMarshal.FromLua<AttackPattern>(DynValue.NewTable(t));
        Assert.That(r.IsErr, Is.True);
        r.TryGetError(out var e);
        Assert.That(e.Kind, Is.EqualTo(LuaErrorKind.FieldCoercion));
    }

    [Test]
    public void FromLua_OutOfRangeLong_IsFieldCoercion()
    {
        var s = NewScript();
        var t = new Table(s);
        t.Set("big", DynValue.NewNumber(1e20)); // beyond exact-integer range
        var r = ScriptableMarshal.FromLua<LongHolder>(DynValue.NewTable(t));
        Assert.That(r.IsErr, Is.True);
        r.TryGetError(out var e);
        Assert.That(e.Kind, Is.EqualTo(LuaErrorKind.FieldCoercion));
    }

    private static Table MinimalAttackTable(Script s)
    {
        var t = new Table(s);
        t.Set("id", DynValue.NewString("x"));
        t.Set("reps", DynValue.NewNumber(1));
        t.Set("cooldown", DynValue.NewNumber(0));
        t.Set("phase", DynValue.NewString("Stalking"));
        t.Set("tags", DynValue.NewTable(new Table(s)));
        return t;
    }
}
