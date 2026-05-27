using Filament;
using Filament.Demo;

// A tiny non-Godot host that proves the whole loop: load a Lua behavior module,
// call into it with a [Scriptable] params record, and hot-reload edits live.
// Run it, then edit demo/lua/policy.lua (in bin/.../lua) and watch the output.

var luaDir = Path.Combine(AppContext.BaseDirectory, "lua");
using var registry = new ScriptRegistry(luaDir);
registry.StatusChanged += s =>
    Console.WriteLine($"[registry] {s.Kind}: {s.Path}{(s.Message is null ? "" : " — " + s.Message)}");

var init = registry.Initialize(liveReload: true);
if (init.TryGetError(out var initErr))
{
    Console.WriteLine($"init failed: {initErr}");
    return;
}

Console.WriteLine($"Loaded {init.Match(n => n.ToString(), _ => "?")} module(s). "
    + "Edit lua/policy.lua and watch the output change. Ctrl+C to quit.\n");

var rng = new Random();
while (true)
{
    registry.Pump(0.1);

    if (registry.GetModule("policy").TryGet(out var module))
    {
        var input = new PolicyInput(
            HpFraction: (float)rng.NextDouble(),
            Distance: MathF.Round((float)(rng.NextDouble() * 10), 1));

        var result = module.Call<string, PolicyInput>("describe", input);
        Console.WriteLine(result.Match(
            s => $"describe(hp={input.HpFraction:0.00}, d={input.Distance}) -> {s}",
            err => $"ERR {err}"));
    }

    Thread.Sleep(1000);
}
