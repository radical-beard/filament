using Filament;
using Filament.Demo;

// A tiny non-Godot host that proves the whole loop: load a Lua behavior module,
// call into it with a [Scriptable] params record, and hot-reload edits live.
// Run it from a checkout, then edit demo/lua/policy.lua and watch the output.

var options = DemoOptions.Parse(args);
var luaDir = Path.GetFullPath(options.LuaRoot ?? FindDefaultLuaRoot());
using var registry = new ScriptRegistry(luaDir);
registry.StatusChanged += s =>
    Console.WriteLine($"[registry] {s.Kind}: {s.Path}{(s.Message is null ? "" : " — " + s.Message)}");

var init = registry.Initialize(liveReload: options.LiveReload);
if (init.TryGetError(out var initErr))
{
    Console.WriteLine($"init failed: {initErr}");
    Environment.ExitCode = 1;
    return;
}

var mode = options.Ticks is null
    ? "Edit files under the Lua root and watch the output change. Ctrl+C to quit."
    : $"Running {options.Ticks.Value} tick(s).";
Console.WriteLine($"Loaded {init.Match(n => n.ToString(), _ => "?")} module(s).");
Console.WriteLine($"Lua root: {luaDir}");
Console.WriteLine($"{mode}\n");

var rng = new Random();
var remaining = options.Ticks;
while (remaining is null || remaining.Value > 0)
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

    if (remaining is not null) remaining--;
    if (remaining is null || remaining.Value > 0) Thread.Sleep(options.IntervalMs);
}

static string FindDefaultLuaRoot()
{
    var sourceLuaRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "lua"));
    if (Directory.Exists(sourceLuaRoot)) return sourceLuaRoot;

    return Path.Combine(AppContext.BaseDirectory, "lua");
}

internal sealed record DemoOptions(string? LuaRoot, int? Ticks, bool LiveReload, int IntervalMs)
{
    public static DemoOptions Parse(string[] args)
    {
        string? luaRoot = null;
        int? ticks = null;
        var liveReload = true;
        var intervalMs = 1000;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--lua-root":
                    luaRoot = RequireValue(args, ref i, "--lua-root");
                    break;
                case "--ticks":
                    ticks = ParsePositiveInt(RequireValue(args, ref i, "--ticks"), "--ticks");
                    break;
                case "--once":
                    ticks = 1;
                    break;
                case "--no-live-reload":
                    liveReload = false;
                    break;
                case "--interval-ms":
                    intervalMs = ParsePositiveInt(RequireValue(args, ref i, "--interval-ms"), "--interval-ms");
                    break;
                case "--help":
                case "-h":
                    PrintUsage();
                    Environment.Exit(0);
                    break;
                default:
                    Console.Error.WriteLine($"unknown argument: {args[i]}");
                    PrintUsage();
                    Environment.Exit(2);
                    break;
            }
        }

        return new DemoOptions(luaRoot, ticks, liveReload, intervalMs);
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            Console.Error.WriteLine($"{option} requires a value");
            Environment.Exit(2);
        }
        index++;
        return args[index];
    }

    private static int ParsePositiveInt(string value, string option)
    {
        if (int.TryParse(value, out var parsed) && parsed > 0) return parsed;
        Console.Error.WriteLine($"{option} requires a positive integer");
        Environment.Exit(2);
        return 1;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
        Filament demo

        Usage:
          dotnet run --project demo/Filament.Demo.csproj -- [options]

        Options:
          --ticks N          Run N ticks and exit.
          --once             Run one tick and exit.
          --lua-root PATH    Load Lua modules from PATH instead of the build output.
          --no-live-reload   Disable file watching.
          --interval-ms N    Delay between ticks when running more than one tick.
        """);
    }
}
