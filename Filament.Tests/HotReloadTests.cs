namespace Filament.Tests;

using System;
using System.IO;
using System.Threading;
using Filament;
using MoonSharp.Interpreter;

[TestFixture]
public class HotReloadTests
{
    private static string Module(int value) =>
        $"local M = {{}}\nfunction M.v(p) return {value} end\nreturn M";

    [Test]
    public void Replace_SwapsLiveChunk()
    {
        var s1 = LuaSandbox.Create();
        LuaSandbox.LoadModule(s1, Module(1), "beh").TryGet(out var t1);
        var m = new LuaModule("beh", s1, t1);

        m.Call<int, AddArgs>("v", new AddArgs(0, 0)).TryGet(out var before);
        Assert.That(before, Is.EqualTo(1));

        var s2 = LuaSandbox.Create();
        LuaSandbox.LoadModule(s2, Module(2), "beh").TryGet(out var t2);
        m.Replace(s2, t2);

        m.Call<int, AddArgs>("v", new AddArgs(0, 0)).TryGet(out var after);
        Assert.That(after, Is.EqualTo(2));
    }

    [Test]
    public void Registry_LiveReload_PicksUpEdits()
    {
        var dir = TempDir();
        try
        {
            var file = Path.Combine(dir, "beh.lua");
            File.WriteAllText(file, Module(1));

            using var reg = new ScriptRegistry(dir);
            Assert.That(reg.Initialize(liveReload: true).IsOk, Is.True);

            var mo = reg.GetModule("beh");
            Assert.That(mo.IsSome, Is.True);
            mo.TryGet(out var mod);
            mod.Call<int, AddArgs>("v", new AddArgs(0, 0)).TryGet(out var first);
            Assert.That(first, Is.EqualTo(1));

            File.WriteAllText(file, Module(2));

            int latest = first;
            for (int i = 0; i < 80 && latest != 2; i++)
            {
                Thread.Sleep(50);
                reg.Pump(1.0); // large delta so the debounce elapses once the event lands
                mod.Call<int, AddArgs>("v", new AddArgs(0, 0)).TryGet(out latest);
            }
            Assert.That(latest, Is.EqualTo(2), "edit should hot-reload into the live module");
        }
        finally { Cleanup(dir); }
    }

    [Test]
    public void Registry_ParseError_KeepsLastGood()
    {
        var dir = TempDir();
        try
        {
            var file = Path.Combine(dir, "beh.lua");
            File.WriteAllText(file, Module(2));

            using var reg = new ScriptRegistry(dir);
            reg.Initialize(liveReload: true);
            var sawFailed = false;
            reg.StatusChanged += st => { if (st.Kind == ScriptStatusKind.Failed) sawFailed = true; };

            reg.GetModule("beh").TryGet(out var mod);

            File.WriteAllText(file, "this is not valid lua {{{ ");
            for (int i = 0; i < 40; i++)
            {
                Thread.Sleep(50);
                reg.Pump(1.0);
                if (sawFailed) break;
            }

            mod.Call<int, AddArgs>("v", new AddArgs(0, 0)).TryGet(out var stillGood);
            Assert.That(stillGood, Is.EqualTo(2), "bad edit must keep the last-good chunk live");
            Assert.That(sawFailed, Is.True, "a Failed status should be raised");
        }
        finally { Cleanup(dir); }
    }

    private static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "filament_hr_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void Cleanup(string dir)
    {
        try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
    }
}
