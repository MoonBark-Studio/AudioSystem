# plugins

**Module:** `plugins`

## Key Types
## Key Types (1745 files, ~186920 lines)
AICommandCoordinator, AIMode, AITests, AbilitiesEcsPlugin, AbilitiesEcsPluginTests, AbilitiesModule, AbilitiesPlugin, AbilityBookComponent, AbilityCommandHandler, AbilityCommandTypesTests, AbilityComponent, AbilityComponentTests, AbilityCooldownAdapter, AbilityCooldownComponent, AbilityCooldownSystem, AbilityDefinition, AbilityDefinitionTests, AbilityDemo, AbilityKind, AbilityLearningComponent
## Namespaces
- `MoonBark.AI.Conditions`
- `MoonBark.AI.ECS.Components`
- `MoonBark.AI.ECS.EventWeaver`
- `MoonBark.AI.ECS.GOAP.Systems`
- `MoonBark.AI.ECS.Hybrid.Systems`
- `MoonBark.AI.ECS.Shared`
- `MoonBark.AI.Shared`
- `MoonBark.AI.Tests`
- `MoonBark.Abilities.Core`
- `MoonBark.Abilities.Core.Effects`
- `MoonBark.Abilities.Core.Execution`
- `MoonBark.Abilities.ECS`
- `MoonBark.Abilities.Godot`
- `MoonBark.Abilities.Godot.Examples`
- `MoonBark.Abilities.Tests`
- `MoonBark.WorldState`
## ECS Architecture (v2)
- ECS subdirectories: Abilities/ECS, AI/ECS, Attributes/ECS, Economy/ECS, EcsPhysics2D/ECS, GridPathfinding/ECS, GridPlacement/ECS, ItemVault/ECS, Minimap/ECS, Quest/ECS, StatusEffects/ECS, Upgrades/ECS, WorldTime/ECS, Crafting/cs/ECS, GridPathfinding/Tests/ECS, GridPlacement/Tests/ECS, GridPlacement/Tests/Core/ECS, ItemDrops/Tests/ECS, ItemVault/Tests/ECS, Sensors/cs/ECS, WorldTime/ECS/tests/ECS
- ECS files outside subdirectories: 27
- Flat structure: Core/, ECS/, Godot/ (cs/ prefix not required)
## Overview
27-plugin monorepo of MoonBark Studio Godot C# plugins, organized under a hub-and-spoke Git submodule architecture. Each plugin is a standalone, game-agnostic system intended for reuse across MoonBark titles.

## Architecture

### Folder Convention
Each plugin follows a flat 4-folder convention:
```
<PluginName>/
  Core/       — Pure interfaces, no Godot or Friflo dependencies
  ECS/        — Friflo ECS types (components, systems). Depends on Core.
  Godot/      — Godot Node/Resource types. Depends on Core and ECS.
  Tests/      — Unit/integration tests
```
No `cs/` wrapper, no double-nested subdirectories. Build artifacts (bin/, obj/) should not be committed.

### ECS Architecture
- **Core/** — Interfaces only. Zero engine or ECS dependencies. Can be referenced by anything.
- **ECS/** — Friflo.Engine.ECS types only. Can depend on Core.
- **Godot/** — Godot types (Node, Resource, RefCounted). Can depend on Core and ECS.
- **Rule:** No Friflo types outside ECS/. No Godot types in Core/.

## Plugins

| Plugin | Role | ECS | Tests |
|--------|------|-----|-------|
| Abilities | Agent ability system (cast, cooldown, learning) | Yes | xUnit |
| AI | GOAP, UtilityAI, EventWeaver, Hybrid AI systems | Yes | Minimal |
| Attributes | Entity attribute/modifier system | Yes | Minimal |
| AudioSystem | Godot audio bus management | No | Minimal |
| Crafting | Recipe-based crafting with grid layout | Yes | xUnit (15 tests) |
| Economy | Shared plant/animal ECS: growth, production, gathering | Yes | Minimal |
| EcsPhysics2D | Box2D physics bridged to ECS | Yes | xUnit |
| EntityTargetingSystem | Target acquisition and tracking | No | xUnit |
| GridAgents | Grid-based agent movement/pathfinding | Yes | None |
| GridPathfinding | A* pathfinding on grid data | Yes | Minimal |
| GridPlacement | Snap-to-grid placement with validation rules | Yes | Godot test runner |
| ItemDrops | Drop table and pickup system | Yes | Godot test runner |
| ItemVault | Inventory container and transaction system | Yes | xUnit + Godot tests |
| MapLoader | Tiled map loading via YAT library | No | Minimal |
| Minimap | 2D rendered minimap | Yes | Godot tests |
| NetworkSync | Snapshot interpolation for networked game state | No | Stress + E2E tests |
| PrototypeUI | Reusable UI panel/component system | No | None |
| Quest | Quest tracking with objectives and conditions | Yes | Minimal |
| RenderingOptimizations | Visibility culling and render budget system | No | None |
| Sensors | Raycast/area-based entity sensing | Yes | None |
| StatusEffects | Buff/debuff effect system | Yes | None |
| TaskDistribution | Work-task assignment to agents | No | None |
| Telemetry | Event/metric logging to local files | No | None |
| Upgrades | Per-entity stat upgrade system | Yes | None |
| WorldGen2D | Procedural 2D world generation with biomes | Yes | None |
| WorldState | Global game state registry | No | None |
| WorldTime | Game calendar, day/night cycle, time scaling | Yes | Godot test runner |

## Key Namespaces
```
MoonBark.Abilities.Core / .ECS
MoonBark.AI.Conditions / .EventWeaver / .GOAP / .UtilityAI / .Shared
MoonBark.Attributes.Core / .ECS
MoonBark.Crafting.Core
MoonBark.Economy.ECS
MoonBark.GridAgents.Core
MoonBark.GridPathfinding.Core / .ECS
MoonBark.GridPlacement.Core / .ECS / .Godot
MoonBark.ItemDrops.Core / .ECS
MoonBark.ItemVault.Core / .ECS / .Godot
MoonBark.Quest.Core / .ECS / .Godot
MoonBark.StatusEffects.Core / .ECS
MoonBark.WorldGen2D.Core / .Godot
MoonBark.WorldTime.Core / .ECS / .Godot
```

## Dependencies
Each plugin is self-contained. Cross-plugin dependencies go through Core/ interfaces (framework contracts). No plugin should directly reference another plugin's ECS/ or Godot/ types.

## Status
- ✅ Audited: 2026-05-06
- Changed files this run: 156
- File count: 1745 C# files (~186920 lines)