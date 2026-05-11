---
name: moonbark-plugin-architecture
description: "Review or implement MoonBark Idle plugin integrations. Use when tracing WorldTime or GridPlacement wiring, debugging HUD/bootstrap startup path issues, enforcing GameBootstrap-owned runtime composition, keeping scene-path SSOT in ScenePaths, auditing GodotSignalBus migration, wiring GameProvider-owned signal buses, replacing TimeContextNode with WorldTimeProvider plus TimeSignalBus, or checking plugin Core versus Godot boundary violations in the projects workspace."
user-invocable: true
---

# MoonBark Plugin Architecture

Use this skill for the `projects/` workspace, especially `games/moonbark-idle`, `plugins/WorldTime`, and `plugins/GridPlacement`.

## Verified Architecture Rules

- Preserve the game/plugin/framework split. MoonBark Idle composes plugin runtimes; plugin Core code should stay free of Godot dependencies, and plugin Godot layers should host scene wiring, signals, and engine adapters.
- Prefer the dual bus pattern for plugin communication:
  - Core `*EventBus` for C# domain events.
  - Godot `*SignalBus` for Godot signals and GDScript-facing consumers.
- **Autoloads are banned** - use Provider pattern instead. Plugin entry points use `{PluginName}Bootstrap` as RefCounted.
- The supported MoonBark Idle gameplay entry path is `Main` → `MainMenu` → `ScenePaths.Level` → `Level_SunhatchGlade.tscn` → `GameBootstrap`. Do not bypass the gameplay shell by loading raw level-content scenes directly when investigating HUD or startup regressions.
- In MoonBark Idle, `GameBootstrap` is the wiring point. It creates a `GameProvider` with typed plugin signal buses as exports, initializes them from the game-owned runtime, and injects those buses directly into HUD, controllers, and panels.
- `GameBootstrap` owns runtime composition. It should instantiate or wire the harness, level loader, HUD, debug overlay, and plugin buses; level-content scenes should provide authored content, not duplicate bootstrap logic.
- Do not add scene nodes solely to store signal buses or rebuild singleton-style service lookup paths. Wiring lives in `GameProvider` exports, not the scene tree.
- `GameProvider` is a root-owned resource context (`.tres`). Scene roots export the `.tres`, root-level owners consume it, and subtree owners distribute narrow typed dependencies downward.
- Do not path-load `game_provider.tres` from arbitrary UI children or service classes when a root-owned context already exists.
- Prefer provider wrappers around the authoritative runtime. `WorldTimeProvider.Initialize(IWorldTimeRuntime)` is the preferred path because it wraps the game-owned runtime.
- Treat `TimeContextNode` as deprecated legacy. It creates its own `WorldTimeEcsRuntime`, so it can diverge from the game-owned runtime and should not be used for new production integrations.
- Treat `GodotSignalBus` as migration scaffolding. `SpeedAdjusted`, `PlaceableSelected`, `CategoryChanged`, and `TerrainSelected` are migration debt unless a compatibility note proves the old bus is still temporarily required.
- Prefer event-driven updates over polling when a plugin bus already exposes the change. In MoonBark Idle, time-of-day is already bridged into `TimeSignalBus`; new integrations should subscribe instead of polling `_Process`.
- Use explicit names that distinguish bus layers: `EventBus` for Core buses, `SignalBus` for Godot bridges, and provider names for runtime wrappers.
- Keep scene/resource path literals in `ScenePaths` or equivalent SSOT holders. Menu transitions, HUD/bootstrap references, and tests should consume the shared constant instead of copying `res://...` strings.

## MoonBark Idle Flow Notes

- Startup flow today:
  - `Main` loads `tests/TestRunner.tscn` in test mode.
  - Otherwise `Main` loads `scenes/MainMenu.tscn`.
  - `MainMenu` pre-warms `HarnessCache` and transitions with `ScenePaths.Level`.
  - `Level_SunhatchGlade.tscn` is the gameplay shell and runs `GameBootstrap`.
- Time flow today:
  - `GameBootstrap.OnTick`
  - `IdleSimulationHarness.AdvanceSeconds`
  - `IdleSimulationDirector.AdvanceSingleTick`
  - `IdleWorldTimeBridge.Advance`
  - `WorldTimeEcsRuntime.Update`
  - `WorldTimeAdvanceSystem.Update`
  - `TimeEventBus`
  - `TimeSignalBus`
- The current light update is bootstrap-mediated, not self-subscribed in the node: `GameBootstrap` subscribes to `TimeSignalBus.TimeOfDayChangedSignal` and calls `TimeOfDayLightNode.UpdateFromHour(...)`.
- `GameProvider` holds `TimeSignalBus` and `PlacementSignalBus` as `[Export]` properties for explicit injection into consumers such as `HudManager`, `IdleHotbar`, `PlacementPanel`, and `PlacementController`.
- `HudManager` is the HUD root distributor. Bootstrap injects typed buses into `HudManager`, `HudManager` fans them out to immediate children, and composite widgets like `IdleHotbar` fan them out again to owned descendants like `TimeSection`.
- `MainMenu` and `HudRoot` own `GameProvider` via exported scene-root references instead of path-loading the `.tres` from code.
- For startup/HUD regressions, validate the actual authored entry path first. Prefer headless Godot test execution for runtime startup coverage when VSTest host behavior is noisy or environment-sensitive.

## Audit Checklist

- Does the startup chain go through `MainMenu` and the gameplay shell scene, rather than bypassing `GameBootstrap`?
- Are scene switches and scene-instantiation paths using `ScenePaths` constants instead of duplicated `res://...` strings?
- Does Core code avoid `using Godot;` and other Godot types?
- Does the game inject plugin buses through GameProvider exports instead of reaching through singletons/autoloads?
- Does the scene root own `GameProvider` and avoid path-loading the `.tres` from children?
- Do root UI owners distribute typed dependencies to direct children instead of every leaf resolving the full context object?
- Are UI publishers and subscribers using the owning plugin bus instead of `GodotSignalBus`?
- Are `[Obsolete]` types such as `TimeContextNode` still referenced in production code?
- Are hot ECS systems allocating or recreating queries every tick?
- In this workspace's Friflo ECS code, are queried marker tags staying as empty `IComponent` or record-struct markers instead of being moved to `ITag`?

## Related Files To Check

- `games/moonbark-idle/godot/Main.cs`
- `games/moonbark-idle/godot/MainMenu.cs`
- `games/moonbark-idle/godot/ScenePaths.cs`
- `games/moonbark-idle/godot/GameBootstrap.cs`
- `games/moonbark-idle/godot/Provider/GameProvider.cs`
- `games/moonbark-idle/godot/Services/GodotSignalBus.cs`
- `games/moonbark-idle/godot/Services/HudManager.cs`
- `games/moonbark-idle/godot/PlacementPanel.cs`
- `games/moonbark-idle/godot/PlacementController.cs`
- `games/moonbark-idle/godot/UI/TimeSection.cs`
- `plugins/WorldTime/Godot/addons/WorldTime/WorldTimeProvider.cs`
- `plugins/WorldTime/Godot/addons/WorldTime/TimeSignalBus.cs`
- `plugins/WorldTime/Godot/addons/WorldTime/WorldTimeBootstrap.cs`
- `plugins/WorldTime/Godot/addons/WorldTime/TimeContextNode.cs`
- `plugins/GridPlacement/Godot/Services/Placement/PlacementSignalBus.cs`
- `games/moonbark-idle/ROADMAP.md`
- `games/moonbark-idle/HEALTH.md`