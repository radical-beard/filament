namespace Filament.Tests;

using Filament.Config;

[TestFixture]
public class ConfigParseTests
{
    private const string World = """
        schema_version = 1
        description = "test arena"

        [entities.boss]
        type = "EnemyEntity"
        position = [0, 1, -10]
        max_health = 320
        select_pattern = "lua:marionette_aggressive"
        patterns = [ { id = "jab", telegraph = 0.5 }, { id = "swing", telegraph = 0.9 } ]

        [entities.player]
        type = "StormCorsair"
        parent = "root"
        position = [0, 1, 22]
        """;

    [Test]
    public void Parses_World_Entities_And_NestedProps()
    {
        var r = TomlWorldParser.Parse(World);
        Assert.That(r.IsOk, Is.True, r.Match(_ => "", e => e.ToString()));
        r.TryGet(out var world);

        Assert.That(world.SchemaVersion, Is.EqualTo(1));
        Assert.That(world.Description, Is.EqualTo("test arena"));
        Assert.That(world.Entities.Keys, Is.EquivalentTo(new[] { "boss", "player" }));

        var boss = world.Entities["boss"];
        Assert.That(boss.Type, Is.EqualTo("EnemyEntity"));
        Assert.That(boss.Parent, Is.Null);
        Assert.That(world.Entities["player"].Parent, Is.EqualTo("root"));

        var p = new PropReader(boss.Props);
        Assert.That(p.GetVec3("position"), Is.EqualTo((0f, 1f, -10f)));
        Assert.That(p.GetInt("max_health"), Is.EqualTo(320));
        Assert.That(p.TryGetLuaRef("select_pattern", out var script), Is.True);
        Assert.That(script, Is.EqualTo("marionette_aggressive"));

        var patterns = p.GetArray("patterns");
        Assert.That(patterns, Is.Not.Null);
        Assert.That(patterns!.Count, Is.EqualTo(2));
        Assert.That(patterns[0], Is.InstanceOf<System.Collections.Generic.IReadOnlyDictionary<string, object?>>());
    }

    [Test]
    public void MissingType_IsError_WithEntityId()
    {
        var r = TomlWorldParser.Parse("schema_version = 1\n[entities.x]\nposition = [1,2,3]");
        Assert.That(r.IsErr, Is.True);
        r.TryGetError(out var e);
        Assert.That(e.EntityId, Is.EqualTo("x"));
    }

    [Test]
    public void MissingSchemaVersion_IsError()
    {
        var r = TomlWorldParser.Parse("[entities.x]\ntype = \"Foo\"");
        Assert.That(r.IsErr, Is.True);
    }
}

[TestFixture]
public class WorldDiffTests
{
    private static WorldConfig Parse(string toml)
    {
        TomlWorldParser.Parse(toml).TryGet(out var w);
        return w;
    }

    [Test]
    public void Computes_Added_Removed_Changed()
    {
        var old = Parse("""
            schema_version = 1
            [entities.boss]
            type = "EnemyEntity"
            max_health = 320
            [entities.player]
            type = "StormCorsair"
            """);
        var @new = Parse("""
            schema_version = 1
            [entities.boss]
            type = "EnemyEntity"
            max_health = 200
            [entities.add_a]
            type = "Marionette"
            """);

        var diff = WorldDiffComputer.Compute(old, @new);

        Assert.That(diff.Added.Select(e => e.Id), Is.EquivalentTo(new[] { "add_a" }));
        Assert.That(diff.Removed, Is.EquivalentTo(new[] { "player" }));
        Assert.That(diff.Changed.Count, Is.EqualTo(1));
        Assert.That(diff.Changed[0].Id, Is.EqualTo("boss"));
        Assert.That(diff.Changed[0].TypeChanged, Is.False);
        Assert.That(diff.Changed[0].ChangedKeys, Is.EquivalentTo(new[] { "max_health" }));
    }

    [Test]
    public void TypeChange_IsFlagged()
    {
        var old = Parse("schema_version = 1\n[entities.e]\ntype = \"A\"");
        var @new = Parse("schema_version = 1\n[entities.e]\ntype = \"B\"");
        var diff = WorldDiffComputer.Compute(old, @new);
        Assert.That(diff.Changed.Count, Is.EqualTo(1));
        Assert.That(diff.Changed[0].TypeChanged, Is.True);
    }

    [Test]
    public void IdenticalConfigs_AreEmptyDiff()
    {
        var toml = "schema_version = 1\n[entities.e]\ntype = \"A\"\nvals = [1, 2, 3]";
        var diff = WorldDiffComputer.Compute(Parse(toml), Parse(toml));
        Assert.That(diff.IsEmpty, Is.True);
    }
}
