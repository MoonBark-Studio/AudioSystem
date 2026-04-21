namespace MoonBark.Telemetry.Core;

/// <summary>
/// Defines a single stat that Telemetry can display.
/// Loaded from telemetry_stats.yaml alongside TelemetryConfiguration.
/// </summary>
public sealed class GameStatDefinition
{
    /// <summary>Unique identifier, e.g. "frames_per_second", "economy_balance".</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable name shown in the panel.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Category for grouping: performance, economy, simulation, debug.</summary>
    public string Category { get; set; } = "general";

    /// <summary>Whether to show this stat in the player-facing panel.</summary>
    public bool ShowInPanel { get; set; } = true;

    /// <summary>Only visible in dev builds when true.</summary>
    public bool DevOnly { get; set; } = false;

    /// <summary>Format string for the value, e.g. "{0:F1} ms", "${0:N0}".</summary>
    public string ValueFormat { get; set; } = "{0}";
}
