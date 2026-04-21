# MoonBark Telemetry — Roadmap

**Priority:** P1
**Status:** Phase 0 — scaffolding
**Created:** 2026-04-18

## Vision

Real-time ECS performance monitoring for MoonBark games. The primary goal is to **extend the Godot Editor** with a dedicated ECS profiler dock, allowing developers to inspect system tick times, entity counts, and bottlenecks at runtime. This shares the same Core data model and visual components as the in-game HUD.

## Architecture

```
MoonBark.Framework/Core/
└── IPerformanceData.cs          ← ECS perf contract (Framework owns this)

plugins/moonbark-telemetry/
├── Core/                        ← game-agnostic data model + YAML config
│   ├── TelemetryConfiguration.cs
│   └── GameStatDefinition.cs
├── Godot/
│   ├── InGame/                  ← runtime: shown during play
│   └── Editor/                  ← editor: Godot editor tooling
└── Tests/Core/                  ← Core layer unit tests
```

## Plan

### Phase 0 — Scaffold ✅

- [x] Directory structure created
- [x] `TelemetryConfiguration.cs` in Core/
- [x] `GameStatDefinition.cs` in Core/
- [x] `TelemetrySettings.cs` Godot wrapper (converts Resource → Core config)
- [x] `TelemetryConfigurationTests.cs`
- [x] `GameStatDefinitionTests.cs`
- [x] README, HEALTH, ROADMAP docs

### Phase 1 — ECS Perf Integration (this week)

- [ ] `MoonBark.Framework/Core/IPerformanceData.cs` — interface (Framework owns this)
- [ ] `MoonBark.Framework/Core/SystemMetrics.cs` — record struct
- [ ] `IdleSimulationHarness` implements `IPerformanceData` (wraps `_profiler`, `_store`)
- [ ] Thistletide harness implements `IPerformanceData` the same way
- [ ] `SystemRunner` tracked metrics readable through the interface

### Phase 2 — In-Game Panel (this week)

- [ ] `Godot/InGame/TelemetryPanel.cs` — `Control` node
  - Per-system bar chart (latest ms, color-coded)
  - Entity / component count labels
  - Sparkline for top 3 systems (last N ticks)
  - Reads `IPerformanceData` + `TelemetryConfiguration`
- [ ] `Godot/InGame/TelemetryPanel.tscn` — scene file
- [ ] `Config/telemetry.yaml` — default configuration
- [ ] Wire into `SimulationBootstrap` (moonbark-idle) as proof-of-concept
- [ ] Verify: panel renders without affecting simulation performance

### Phase 3 — Game Stats (this week)

- [ ] YAML loading for `TelemetryConfiguration` and `GameStatDefinition` list
- [ ] `GameStatDefinition.Category` filtering in panel (performance, economy, simulation tabs)
- [ ] `DevOnly` flag: hide stats from player panel in release builds
- [ ] moonbark-idle wires first game stats (coins/tick, food, hired agents)

### Phase 4 — Editor Profiler Dock (next week)

- [ ] **TelemetryEditorPlugin**: Formal Godot Editor extension registration.
- [ ] **ECS Profiler Dock**: A dedicated editor dock (bottom panel or right dock) for deep inspection.
- [ ] **Runtime Bridge**: Connect the Editor Dock to the running game instance via `IPerformanceData` signals.
- [ ] **Demo Project Integration**: Use `demo/project.godot` to verify the plugin loads correctly and displays mock ECS data.
- [ ] **Engine-Integrated Tests**: Run GoDotTest/GDUnit4 suites within the `demo/` project to validate visual node rendering.
- [ ] **System Breakdown UI**: Detailed list of all ECS systems, their current `ms` cost, and history sparklines.

### Phase 5 — Polish & Advanced Features (week 2–3)

- [ ] **Live Hot-Swapping**: Enable/Disable specific ECS systems directly from the Editor Profiler Dock.
- [ ] **Threshold Alerts**: Flash panel red when a system exceeds threshold.
- [ ] **Comparison Mode**: Delta vs previous second/minute or baseline.
- [ ] **Allocation Tracking**: Per-system memory allocation monitoring (if using custom allocators).
- [ ] **Friflo.Engine Diagnostics**: Native EntityStore profiling integration.
- [ ] Third-party integration guide in README.

## Dependencies

- `MoonBark.Framework/Core/IPerformanceData.cs` — Phase 1
- moonbark-idle harness wiring — Phase 2
- Thistletide harness wiring — Phase 1

## Changelog

### 2026-04-18

- Scaffold created — Core/, Godot/, Tests/ in place
- Configuration and stat definition types written
- Unit tests for Core types
- Structure confirmed: Core/ = YAML config, Godot/ = [Export] Settings + panels
