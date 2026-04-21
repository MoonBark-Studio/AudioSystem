namespace MoonBark.Telemetry.Core;

/// <summary>
/// Configuration for the Telemetry plugin — loaded from YAML or another backend.
/// Lives in Core so it is engine-agnostic and reusable across all games.
/// </summary>
public sealed class TelemetryConfiguration
{
    /// <summary>Key to toggle the in-game panel. Default: F3.</summary>
    public string ToggleKey { get; set; } = "F3";

    /// <summary>Number of historical ticks to keep for sparklines.</summary>
    public int HistorySize { get; set; } = 60;

    /// <summary>Minimum ms per tick before a system bar turns red.</summary>
    public double CriticalThresholdMs { get; set; } = 4.0;

    /// <summary>Automatically show the panel when CriticalThresholdMs is exceeded.</summary>
    public bool AutoShowOnCritical { get; set; } = true;

    /// <summary>Panel horizontal offset from the top-left corner in pixels.</summary>
    public int PanelOffsetX { get; set; } = 10;

    /// <summary>Panel vertical offset from the top-left corner in pixels.</summary>
    public int PanelOffsetY { get; set; } = 10;

    /// <summary>Width of the panel in pixels.</summary>
    public int PanelWidth { get; set; } = 400;

    /// <summary>Stats to surface in the panel — loaded from telemetry_stats.yaml.</summary>
    public List<GameStatDefinition> Stats { get; set; } = new();
}
