using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

var root = FindRepoRoot(AppContext.BaseDirectory);
Environment.SetEnvironmentVariable("DOTNET_NOLOGO", "1");
Environment.SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1");

await Dotnet("restore", P(root, "filament.slnx"));
await Dotnet("build", P(root, "filament.slnx"), "-warnaserror");
await Dotnet("build", P(root, "tools", "Filament.Verify", "Filament.Verify.csproj"), "-warnaserror");
await Dotnet("format", P(root, "filament.slnx"), "--verify-no-changes", "--verbosity", "minimal");
await Dotnet("format", P(root, "tools", "Filament.Verify", "Filament.Verify.csproj"), "--verify-no-changes", "--verbosity", "minimal");
await Dotnet("test", P(root, "filament.slnx"));
await DemoSmoke(root);
await AgentSmoke(root);
await PackageSmoke(root);

Console.WriteLine("all verification checks passed");

static async Task DemoSmoke(DirectoryInfo root)
{
    var output = await DotnetCapture("run", "--project", P(root, "demo", "Filament.Demo.csproj"), "--", "--ticks", "3");
    if (!output.Contains("Loaded 1 module(s)", StringComparison.Ordinal))
        throw new InvalidOperationException("demo did not report loaded modules");
    if (!output.Contains("describe(hp=", StringComparison.Ordinal))
        throw new InvalidOperationException("demo did not call policy.describe");
    if (output.Contains("lua root does not exist", StringComparison.Ordinal))
        throw new InvalidOperationException("demo reported missing Lua root");
}

static async Task AgentSmoke(DirectoryInfo root)
{
    JsonDocument.Parse(await File.ReadAllTextAsync(P(root, ".mcp.json"))).Dispose();
    JsonDocument.Parse(await File.ReadAllTextAsync(P(root, "global.json"))).Dispose();

    var mcp = await File.ReadAllTextAsync(P(root, ".mcp.json"));
    if (!mcp.Contains("\"filament-playtest\"", StringComparison.Ordinal))
        throw new InvalidOperationException(".mcp.json does not register filament-playtest");
    if (!mcp.Contains("\"playtest/server.py\"", StringComparison.Ordinal))
        throw new InvalidOperationException(".mcp.json does not use the repo-relative playtest path");

    var uv = ResolveCommand(OperatingSystem.IsWindows() ? new[] { "uv.exe", "uv" } : new[] { "uv" }, "uv is required for the playtest MCP server");
    await Run(uv, "--version");

    var python = ResolvePython();
    await RequirePython311(python);
    var pyEnv = new Dictionary<string, string>
    {
        ["PYTHONPYCACHEPREFIX"] = CreateTempDir("filament-pycache-").FullName,
    };
    await RunCaptureWith(python, new[]
    {
        "-m", "py_compile",
        P(root, "playtest", "preview.py"),
        P(root, "playtest", "selftest.py"),
        P(root, "playtest", "server.py"),
        P(root, "playtest", "survey.py"),
    }, null, pyEnv);
    await RunCaptureWith(python, new[] { P(root, "playtest", "preview.py") }, null, pyEnv);
}

static async Task PackageSmoke(DirectoryInfo root)
{
    const string version = "0.1.0";
    var feed = CreateTempDir("filament-pack-");

    await Dotnet("pack", P(root, "filament.slnx"), "-c", "Release", "-o", feed.FullName);

    var abstractions = P(feed, $"RadicalBeard.Filament.Abstractions.{version}.nupkg");
    var config = P(feed, $"RadicalBeard.Filament.Config.{version}.nupkg");
    var core = P(feed, $"RadicalBeard.Filament.Core.{version}.nupkg");
    var godot = P(feed, $"RadicalBeard.Filament.Godot.{version}.nupkg");
    var demo = P(feed, $"Filament.Demo.{version}.nupkg");

    RequireFile(abstractions);
    RequireFile(config);
    RequireFile(core);
    RequireFile(godot);
    if (File.Exists(demo)) throw new InvalidOperationException("Filament.Demo should not be packaged");

    var coreEntries = ZipEntries(core);
    var godotEntries = ZipEntries(godot);
    if (!coreEntries.Contains("analyzers/dotnet/cs/Filament.SourceGen.dll"))
        throw new InvalidOperationException("RadicalBeard.Filament.Core package is missing Filament.SourceGen analyzer asset");
    if (godotEntries.Contains("analyzers/dotnet/cs/Filament.SourceGen.dll"))
        throw new InvalidOperationException("RadicalBeard.Filament.Godot package should not duplicate Filament.SourceGen");

    await CorePackageConsumer(feed, version);
    await GodotPackageConsumer(feed, version);

    Console.WriteLine($"package smoke passed: {feed.FullName}");
}

static async Task CorePackageConsumer(DirectoryInfo feed, string version)
{
    var consumer = CreateTempDir("filament-core-consumer-");
    var app = new DirectoryInfo(P(consumer, "app"));
    await Dotnet("new", "console", "-o", app.FullName);
    WriteNuGetConfig(app, feed);
    await File.WriteAllTextAsync(P(app, "Program.cs"), """
        using Filament;
        using MoonSharp.Interpreter;

        var s = LuaSandbox.Create();
        Console.WriteLine(ScriptableMarshal.TryGet<Inp>(out _));
        var table = ScriptableMarshal.ToLua(new Inp(0.5f), s);
        Console.WriteLine(table.Get("hp_fraction").Number);

        [Scriptable]
        public partial record Inp(float HpFraction);
        """);

    var env = new Dictionary<string, string> { ["NUGET_PACKAGES"] = P(consumer, "packages") };
    await DotnetIn(app, env, "add", "package", "RadicalBeard.Filament.Core", "--version", version);
    var output = await DotnetCaptureIn(app, env, "run");
    var lines = SignificantLines(output);
    if (lines.ElementAtOrDefault(0) != "True")
        throw new InvalidOperationException("Core consumer did not register generated converter");
    if (lines.ElementAtOrDefault(1) != "0.5")
        throw new InvalidOperationException("Core consumer did not marshal hp_fraction");
}

static async Task GodotPackageConsumer(DirectoryInfo feed, string version)
{
    var consumer = CreateTempDir("filament-godot-consumer-");
    var app = new DirectoryInfo(P(consumer, "app"));
    await Dotnet("new", "console", "-o", app.FullName);
    WriteNuGetConfig(app, feed);
    await File.WriteAllTextAsync(P(app, "Program.cs"), """
        using Filament;
        using Godot;
        using MoonSharp.Interpreter;

        var script = LuaSandbox.Create();
        Console.WriteLine(ScriptableMarshal.TryGet<Spatial>(out _));
        var table = ScriptableMarshal.ToLua(new Spatial(new Vector3(1, 2, 3), new Color(1, 0, 0)), script);
        Console.WriteLine(table.Get("position").Table.Get("z").Number);

        [Scriptable]
        public partial record Spatial(Vector3 Position, Color Tint);
        """);

    var env = new Dictionary<string, string> { ["NUGET_PACKAGES"] = P(consumer, "packages") };
    await DotnetIn(app, env, "add", "package", "RadicalBeard.Filament.Godot", "--version", version);
    await DotnetIn(app, env, "build", "-warnaserror");
    var output = await DotnetCaptureIn(app, env, "run");
    var lines = SignificantLines(output);
    if (lines.ElementAtOrDefault(0) != "True")
        throw new InvalidOperationException("Godot consumer did not register generated converter");
    if (lines.ElementAtOrDefault(1) != "3")
        throw new InvalidOperationException("Godot consumer did not marshal Vector3");
}

static void WriteNuGetConfig(DirectoryInfo app, DirectoryInfo feed)
{
    File.WriteAllText(P(app, "nuget.config"), $$"""
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <packageSources>
            <clear />
            <add key="local" value="{{feed.FullName}}" />
            <add key="nuget" value="https://api.nuget.org/v3/index.json" />
          </packageSources>
        </configuration>
        """);
}

static IReadOnlyList<string> SignificantLines(string output)
    => output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.Trim())
        .Where(x => x.Length > 0)
        .ToArray();

static HashSet<string> ZipEntries(string package)
{
    using var zip = ZipFile.OpenRead(package);
    return zip.Entries.Select(x => x.FullName).ToHashSet(StringComparer.Ordinal);
}

static void RequireFile(string path)
{
    if (!File.Exists(path)) throw new FileNotFoundException($"missing package: {path}", path);
}

static string ResolvePython()
    => ResolveCommand(
        OperatingSystem.IsWindows()
            ? new[] { "python.exe", "python3.exe", "python", "python3" }
            : new[] { "python3", "python" },
        "Python 3.11+ is required for playtest agent checks");

static async Task RequirePython311(string python)
{
    await RunCaptureWith(python, new[]
    {
        "-c",
        "import sys; raise SystemExit(0 if sys.version_info >= (3, 11) else 'Python 3.11+ is required for playtest agent checks')",
    });
}

static string ResolveCommand(IEnumerable<string> names, string error)
{
    foreach (var name in names)
    {
        var candidate = FindOnPath(name);
        if (candidate is not null) return candidate;
    }
    throw new InvalidOperationException(error);
}

static string? FindOnPath(string name)
{
    var path = Environment.GetEnvironmentVariable("PATH");
    if (string.IsNullOrWhiteSpace(path)) return null;

    foreach (var dir in path.Split(Path.PathSeparator))
    {
        if (string.IsNullOrWhiteSpace(dir)) continue;
        var candidate = Path.Combine(dir, name);
        if (File.Exists(candidate)) return candidate;
    }
    return null;
}

static DirectoryInfo FindRepoRoot(string start)
{
    var dir = new DirectoryInfo(start);
    while (dir is not null)
    {
        if (File.Exists(P(dir, "filament.slnx"))) return dir;
        dir = dir.Parent;
    }
    throw new InvalidOperationException("could not find repository root");
}

static DirectoryInfo CreateTempDir(string prefix)
{
    var path = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
    return Directory.CreateDirectory(path);
}

static Task Dotnet(params string[] args) => Run("dotnet", args);
static Task DotnetIn(DirectoryInfo cwd, IReadOnlyDictionary<string, string> env, params string[] args)
    => RunWith("dotnet", args, cwd, env);
static Task<string> DotnetCapture(params string[] args) => RunCapture("dotnet", args);
static Task<string> DotnetCaptureIn(DirectoryInfo cwd, IReadOnlyDictionary<string, string> env, params string[] args)
    => RunCaptureWith("dotnet", args, cwd, env);

static async Task Run(string file, params string[] args)
{
    _ = await RunCapture(file, args);
}

static async Task RunWith(string file, string[] args, DirectoryInfo cwd, IReadOnlyDictionary<string, string> env)
{
    _ = await RunCaptureWith(file, args, cwd, env);
}

static async Task<string> RunCapture(string file, params string[] args)
    => await RunCaptureWith(file, args, null, null);

static async Task<string> RunCaptureWith(
    string file,
    string[] args,
    DirectoryInfo? cwd = null,
    IReadOnlyDictionary<string, string>? env = null)
{
    var psi = new ProcessStartInfo(file)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    if (cwd is not null) psi.WorkingDirectory = cwd.FullName;
    foreach (var arg in args) psi.ArgumentList.Add(arg);
    if (env is not null)
        foreach (var kv in env) psi.Environment[kv.Key] = kv.Value;

    using var process = Process.Start(psi) ?? throw new InvalidOperationException($"failed to start {file}");
    var stdoutTask = process.StandardOutput.ReadToEndAsync();
    var stderrTask = process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();
    var stdout = await stdoutTask;
    var stderr = await stderrTask;

    if (stdout.Length > 0) Console.Write(stdout);
    if (stderr.Length > 0) Console.Error.Write(stderr);

    if (process.ExitCode != 0)
        throw new InvalidOperationException($"{file} {string.Join(' ', args)} failed with exit code {process.ExitCode}");

    return stdout;
}

static string P(DirectoryInfo root, params string[] parts)
    => parts.Aggregate(root.FullName, Path.Combine);
