# MoonBark Telemetry

Real-time ECS performance monitoring for MoonBark games. Tracks per-system tick times, entity/component counts, and in-game stat summaries — with both in-game and Godot editor views.

## Structure

```
plugins/moonbark-telemetry/
├── Core/                        ← engine-agnostic: data model + config
│   ├── IPerformanceData.cs      ← ECS perf contract (in Framework/Core)
│   ├── TelemetryConfiguration.cs
│   └── GameStatDefinition.cs
├── Godot/
│   ├── InGame/                  ← runtime panel for players and devs
│   │   ├── TelemetryPanel.cs
│   │   └── TelemetryPanel.tscn
│   └── Editor/                  ← Godot editor tooling
│       ├── TelemetryEditorPlugin.cs
│       └── TelemetryEditorPanel.cs
└── Tests/
    └── Core/                    ← Core layer unit tests
        ├── TelemetryConfigurationTests.cs
        └── GameStatDefinitionTests.cs
```

## Core Concepts

**Configuration** vs **Settings** — Configuration lives in `Core/` and is loaded from YAML or another backend. Settings live in `Godot/` and are `[Export]` Resource properties for the inspector.

**In-Game view** — shown during play, toggled with a key (default F3). Displays live ECS system timings and optional game stat summaries.

**Editor view** — Godot editor plugin tooling for deep inspection during game development.

## Quick Start

```csharp
// Bootstrap: wire IPerformanceData to Telemetry
_telemetry = PerformanceMonitor.Initialize(harness);

// In your Godot scene:
var panel = TelemetryPanel.New();
panel.Initialize(_telemetry, config);
AddChild(panel);
```

## Configuration

Loaded from YAML. See `Config/telemetry.yaml`:

```yaml
toggle_key: "F3"
history_size: 60
critical_threshold_ms: 4.0
auto_show_on_critical: true
panel_offset_x: 10
panel_offset_y: 10
panel_width: 400
stats:
  - id: frames_per_second
    display_name: "FPS"
    category: performance
    show_in_panel: true
  - id: economy_balance
    display_name: "Coins/Tick"
    category: economy
    show_in_panel: true
```

## Requirements

- MoonBark.Framework (stable SDK surface)
- .NET 8 / Godot 4.x
- Core layer has zero Friflo.ECS dependencies

## Status

Early development — Core interfaces and tests scaffolded. Godot panels in progress.
