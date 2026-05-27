namespace Filament.Tests;

using Filament;
using MoonSharp.Interpreter;

[TestFixture]
public class DispatchTests
{
    private const string Module = """
        local M = {}
        function M.add(p) return p.a + p.b end
        function M.boom(p) error('kaboom') end
        function M.wrong(p) return 'not a number' end
        function M.sandbox_clear(p) return io == nil and os == nil end
        return M
        """;

    private static LuaModule Load()
    {
        var s = LuaSandbox.Create();
        var loaded = LuaSandbox.LoadModule(s, Module, "dispatch");
        Assert.That(loaded.IsOk, Is.True);
        loaded.TryGet(out var table);
        return new LuaModule("dispatch", s, table);
    }

    [Test]
    public void Call_Success_ReturnsValue()
    {
        var r = Load().Call<int, AddArgs>("add", new AddArgs(2, 3));
        Assert.That(r.IsOk, Is.True);
        r.TryGet(out var v);
        Assert.That(v, Is.EqualTo(5));
    }

    [Test]
    public void Call_MissingMethod_IsMethodMissing()
    {
        var r = Load().Call<int, AddArgs>("nope", new AddArgs(0, 0));
        Assert.That(r.IsErr, Is.True);
        r.TryGetError(out var e);
        Assert.That(e.Kind, Is.EqualTo(LuaErrorKind.MethodMissing));
    }

    [Test]
    public void Call_RuntimeError_IsRuntimeError_NotThrow()
    {
        var r = Load().Call<int, AddArgs>("boom", new AddArgs(0, 0));
        Assert.That(r.IsErr, Is.True);
        r.TryGetError(out var e);
        Assert.That(e.Kind, Is.EqualTo(LuaErrorKind.RuntimeError));
        Assert.That(e.Message, Does.Contain("kaboom"));
    }

    [Test]
    public void Call_UncoercibleReturn_IsReturnCoercion()
    {
        var r = Load().Call<int, AddArgs>("wrong", new AddArgs(0, 0));
        Assert.That(r.IsErr, Is.True);
        r.TryGetError(out var e);
        Assert.That(e.Kind, Is.EqualTo(LuaErrorKind.ReturnCoercion));
    }

    [Test]
    public void Sandbox_HasNoIoOrOs()
    {
        var r = Load().Call<bool, AddArgs>("sandbox_clear", new AddArgs(0, 0));
        Assert.That(r.IsOk, Is.True);
        r.TryGet(out var ok);
        Assert.That(ok, Is.True);
    }
}
