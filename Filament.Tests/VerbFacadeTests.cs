namespace Filament.Tests;

using System.Collections.Generic;
using Filament;
using MoonSharp.Interpreter;

[TestFixture]
public class VerbFacadeTests
{
    [TearDown]
    public void Reset() => SandboxVerbs.Clear();

    [Test]
    public void RegisteredVerb_IsCallableFromLua()
    {
        SandboxVerbs.Register("math", "double_it",
            (ctx, args) => DynValue.NewNumber(args[0].Number * 2));

        var s = LuaSandbox.Create();
        LuaSandbox.LoadModule(s, "local M = {}\nfunction M.go(p) return filament.math.double_it(21) end\nreturn M", "verbs")
            .TryGet(out var t);
        var mod = new LuaModule("verbs", s, t);

        var r = mod.Call<int, AddArgs>("go", new AddArgs(0, 0));
        Assert.That(r.IsOk, Is.True);
        r.TryGet(out var v);
        Assert.That(v, Is.EqualTo(42));
    }

    [Test]
    public void Verbs_DoNotOpenTheSandbox()
    {
        SandboxVerbs.Register("noop", "ping", (ctx, args) => DynValue.NewBoolean(true));
        var s = LuaSandbox.Create();
        LuaSandbox.LoadModule(s, "local M = {}\nfunction M.go(p) return io == nil and os == nil end\nreturn M", "verbs")
            .TryGet(out var t);
        var mod = new LuaModule("verbs", s, t);

        mod.Call<bool, AddArgs>("go", new AddArgs(0, 0)).TryGet(out var clean);
        Assert.That(clean, Is.True);
    }
}

[TestFixture]
public class RoleClipMapTests
{
    [Test]
    public void Resolve_ReturnsSomeForKnownRole_NoneOtherwise()
    {
        var map = new RoleClipMap(new Dictionary<string, string>
        {
            ["idle"] = "Combat_Stance",
            ["dodge"] = "Roll_Dodge",
        });

        Assert.That(map.Resolve("idle").IsSome, Is.True);
        map.Resolve("idle").TryGet(out var clip);
        Assert.That(clip, Is.EqualTo("Combat_Stance"));

        // A missing role is a cosmetic gap (None), never a throw — the soft-lock fix.
        Assert.That(map.Resolve("nonexistent").IsNone, Is.True);
    }
}

[TestFixture]
public class LuaRefTests
{
    [Test]
    public void Parses_LuaPrefixedRefs()
    {
        Assert.That(LuaRef.TryParse("lua:enemy/marionette", out var path), Is.True);
        Assert.That(path, Is.EqualTo("enemy/marionette"));
    }

    [Test]
    public void RejectsNonRefs()
    {
        Assert.That(LuaRef.TryParse("Wraith", out _), Is.False);
        Assert.That(LuaRef.TryParse("lua:", out _), Is.False);
        Assert.That(LuaRef.TryParse(null, out _), Is.False);
    }
}
