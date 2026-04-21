using MoonBark.Framework.Core;

namespace MoonBark.Telemetry.Core;

/// <summary>
/// Bridges between YAML-defined stat manifests and live performance data sources.
/// Games provide an <see cref="IPerformanceData"/> instance and optionally an
/// <see cref="IReadOnlyWorldView"/> for game-level stats (coins, food, etc.).
/// </summary>
public sealed class GameStatDatabase
{
    private readonly List<GameStatDefinition> _definitions;
    private readonly IPerformanceData? _perfData;
    private readonly IReadOnlyWorldView? _worldView;
    private readonly Dictionary<string, Func<double>> _customStats = new();

    public GameStatDatabase(
        IEnumerable<GameStatDefinition> definitions,
        IPerformanceData? perfData = null,
        IReadOnlyWorldView? worldView = null)
    {
        _definitions = definitions.ToList();
        _perfData = perfData;
        _worldView = worldView;
    }

    /// <summary>Registers a custom stat with a value getter.</summary>
    public void RegisterCustomStat(string id, Func<double> getter)
    {
        _customStats[id] = getter;
    }

    /// <summary>Returns all stats that match the given filters.</summary>
    public IEnumerable<GameStatValue> GetStats(
        bool? showInPanel = null,
        bool? devOnly = null,
        string? category = null)
    {
        foreach (var def in _definitions)
        {
            if (showInPanel.HasValue && def.ShowInPanel != showInPanel.Value)
                continue;
            if (devOnly.HasValue && def.DevOnly != devOnly.Value)
                continue;
            if (category != null && def.Category != category)
                continue;

            yield return new GameStatValue(def, GetValue(def.Id));
        }
    }

    /// <summary>Returns all categories present in the loaded definitions.</summary>
    public IEnumerable<string> GetCategories()
        => _definitions.Select(d => d.Category).Distinct().OrderBy(c => c);

    private double GetValue(string statId)
    {
        // ECS performance stats
        if (statId == "entity_count")
            return _perfData?.EntityCount ?? 0;
        if (statId == "component_count")
            return _perfData?.ComponentCount ?? 0;
        if (statId == "gc_gen0")
            return _perfData?.GcGen0Collections ?? 0;

        // Game simulation stats
        if (statId == "coins")
            return _worldView?.Coins ?? 0;
        if (statId == "food")
            return _worldView?.Food ?? 0;
        if (statId == "hired_agents")
            return _worldView?.HiredAgents ?? 0;
        if (statId == "tick_count")
            return _worldView?.TickCount ?? 0;

        // Custom stats registered at runtime
        if (_customStats.TryGetValue(statId, out var getter))
            return getter();

        return 0;
    }
}

/// <summary>Runtime value for a stat definition.</summary>
public readonly record struct GameStatValue(
    GameStatDefinition Definition,
    double Value
);
