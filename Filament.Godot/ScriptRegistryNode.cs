namespace Filament.Godot;

using System;
using global::Godot;
using Filament;

/// <summary>
/// The thin Godot adapter over the engine-agnostic <see cref="ScriptRegistry"/>:
/// initializes on <c>_Ready</c>, drives reloads from <c>_Process</c>, and routes
/// status to the Godot log. Add it once near the root of a scene; resolve
/// behavior modules through <see cref="Module"/>.
/// </summary>
public sealed partial class ScriptRegistryNode : Node
{
    /// <summary>Godot path to the Lua root (res:// is globalized for the watcher).</summary>
    [Export] public string LuaRoot { get; set; } = "res://lua";
    [Export] public bool LiveReload { get; set; } = true;

    private ScriptRegistry? _registry;

    public override void _Ready()
    {
        var root = ProjectSettings.GlobalizePath(LuaRoot);
        _registry = new ScriptRegistry(root);
        _registry.StatusChanged += OnStatus;
        var init = _registry.Initialize(LiveReload);
        if (init.TryGetError(out var err)) GD.PushWarning($"[filament] init failed: {err}");
    }

    public override void _Process(double delta) => _registry?.Pump(delta);

    public override void _ExitTree()
    {
        _registry?.Dispose();
        _registry = null;
    }

    /// <summary>Resolve a loaded behavior module by logical path (e.g. "enemy/marionette").</summary>
    public Option<LuaModule> Module(string logicalPath)
        => _registry?.GetModule(logicalPath) ?? Option<LuaModule>.None;

    private static void OnStatus(ScriptStatus s)
    {
        var msg = $"[filament] {s.Kind}: {s.Path}{(s.Message is null ? "" : " — " + s.Message)}";
        if (s.Kind == ScriptStatusKind.Failed) GD.PushWarning(msg);
        else GD.Print(msg);
    }
}
