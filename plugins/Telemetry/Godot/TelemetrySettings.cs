using Godot;
using MoonBark.Telemetry.Core;

namespace MoonBark.Telemetry.Godot;

/// <summary>
/// Godot-level settings wrapper around TelemetryConfiguration.
/// Exposes [Export] properties so designers can configure in the inspector.
/// Loaded from a .tres Resource file at runtime, or can be created inline.
/// </summary>
[GlobalClass]
public partial class TelemetrySettings : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public string ToggleKey { get; set; } = "F3";
    [Export] public int HistorySize { get; set; } = 60;
    [Export] public double CriticalThresholdMs { get; set; } = 4.0;
    [Export] public bool AutoShowOnCritical { get; set; } = true;
    [Export] public int PanelOffsetX { get; set; } = 10;
    [Export] public int PanelOffsetY { get; set; } = 10;
    [Export] public int PanelWidth { get; set; } = 400;

    /// <summary>Converts Godot-level settings to a Core configuration object.</summary>
    public TelemetryConfiguration ToConfiguration()
    {
        return new TelemetryConfiguration
        {
            ToggleKey = ToggleKey,
            HistorySize = HistorySize,
            CriticalThresholdMs = CriticalThresholdMs,
            AutoShowOnCritical = AutoShowOnCritical,
            PanelOffsetX = PanelOffsetX,
            PanelOffsetY = PanelOffsetY,
            PanelWidth = PanelWidth,
        };
    }
}
