# plugins — Health

**Module:** `plugins`

## Overview
27-plugin monorepo of MoonBark Studio Godot C# plugins. Last full audit: 2026-04-22.

## Metrics
| Metric | Value |
|--------|-------|
| C# Files | 1,718 |
| Total Lines | ~229,382 |
| ECS Violations (total) | 58 |
| IDisposable Lifetime Violations | 22 |
| Structure Violations | 681 |
| Changed Files (last audit) | 35 |

## Per-Plugin Quality Scores (100 = perfect)

Scored across 4 dimensions. Weighted: Boundary 30% | Implementation Quality 30% | Testability 20% | Test Suite 20%.

| Plugin | Score | Tier | Bdry | ImplQ | Tstbl | TS |
|--------|-------|------|------|-------|-------|----|
| GridPlacement | **47** | CRITICAL | 3 | 1 | 3 | 3 |
| ItemDrops | **55** | LOW | 6 | 2 | 3 | 2 |
| Economy | **63** | LOW | 8 | 4 | 2 | 1 |
| EntityTargetingSystem | **63** | LOW | 8 | 4 | 2 | 1 |
| NetworkSync | **63** | LOW | 8 | 4 | 2 | 1 |
| PrototypeUI | **63** | LOW | 8 | 4 | 2 | 1 |
| RenderingOptimizations | **63** | LOW | 8 | 4 | 2 | 1 |
| StatusEffects | **63** | LOW | 8 | 4 | 2 | 1 |
| TaskDistribution | **63** | LOW | 8 | 4 | 2 | 1 |
| WorldState | **63** | LOW | 8 | 4 | 2 | 1 |
| GridAgents | **64** | LOW | 10 | 5 | 1 | 0 |
| Sensors | **69** | LOW | 10 | 4 | 2 | 1 |
| Telemetry | **69** | LOW | 10 | 4 | 2 | 1 |
| WorldGen2D | **69** | LOW | 10 | 4 | 2 | 1 |
| GridPathfinding | **70** | LOW | 7 | 4 | 3 | 2 |
| Abilities | **72** | MEDIUM | 6 | 3 | 4 | 3 |
| ItemVault | **73** | MEDIUM | 8 | 4 | 3 | 2 |
| WorldTime | **73** | MEDIUM | 10 | 3 | 3 | 2 |
| AI | **75** | MEDIUM | 10 | 5 | 2 | 1 |
| Upgrades | **75** | MEDIUM | 10 | 5 | 2 | 1 |
| AudioSystem | **79** | MEDIUM | 10 | 4 | 3 | 2 |
| MapLoader | **79** | MEDIUM | 10 | 4 | 3 | 2 |
| Minimap | **79** | MEDIUM | 10 | 4 | 3 | 2 |
| Attributes | **85** | HIGH | 10 | 5 | 3 | 2 |
| EcsPhysics2D | **85** | HIGH | 10 | 5 | 3 | 2 |
| Quest | **85** | HIGH | 10 | 5 | 3 | 2 |
| Crafting | **96** | HIGH | 10 | 5 | 4 | 3 |

Scoring key: Bdry=ECS boundary compliance (0=worst,10=perfect), ImplQ=Implementation quality, Tstbl=Testability, TS=Test suite comprehensiveness.

## Critical Debt — HIGH Priority

### GridPlacement (47/100 — CRITICAL)
- **35 ECS boundary violations** — ECS types in Core/ instead of ECS/
- **646 structure violations** — `cs/` wrapper + 349 double-nested paths + committed build artifacts
- **16 catch-all Exception handlers**
- **75 magic numbers (4+ digits)**
- 7 async void methods
- 5 DeepInheritance chains in EventBus files

### ItemDrops (55/100)
- **7 ECS boundary violations** — ECS types in Core/Conditions/
- **HardcodedSerializationKey** [HIGH] in 5 files
- **NullConditionalInHotPath** [HIGH] in 14 files
- 65 structure violations

### Abilities (72/100)
- **7 ECS boundary violations** — ECS types in Core/Execution/
- NullConditionalInHotPath [HIGH] in 4 files
- 3 structure violations (cs/, godot/, non-standard plugin.cfg)

### WorldTime (73/100)
- **6 NullConditionalInHotPath** [HIGH] — `?.`/`?[]` in large files (3000-11000+ chars)
- **IDisposable lifetime violations** — `TimeContextNode.cs`, `WorldTimeDemo.cs` missing `_ExitTree()`
- 78 structure violations (legacy gdscript/, archived/, .godot/temp/)

### GridPathfinding (70/100)
- **3 ECS boundary violations** — ECS types in Core/

## IDisposable Lifetime Violations (22 total)
22 Godot Node files subscribe to events without `_ExitTree()` — delegates keep Nodes alive after tree removal. Key offenders:
- `AudioSystem/Godot/Systems/GodotAudioManager.cs`
- `GridPlacement/cs/Godot/addons/grid_placement/placement/PlacementContext.cs`
- `ItemDrops` (7 files across cs/Godot/ and Godot/ addons)
- `ItemVault/Godot/addons/item_vault/UI/` (4 files)
- `Minimap/Godot/addons/Minimap/` (3 files)
- `Quest/Godot/` (2 files)

## ECS Boundary Violations (58 total)
| Plugin | Count |
|--------|-------|
| GridPlacement | 35 |
| ItemDrops | 7 |
| Abilities | 7 |
| GridPathfinding | 3 |
| Others (Economy, EntityTargetingSystem, etc.) | 1-2 each |

Top files needing ECS refactor:
- `Abilities/Core/Execution/AbilityCommandHandler.cs`
- `AI/src/EventWeaver/EventWeaverCoordinator.cs`
- `AI/src/Shared/AISystemCoordinator.cs`
- `AI/src/Shared/EntityExtensions.cs`
- `AI/src/EventWeaver/Interfaces/IDelegatePatternGoalMapper.cs`

## Structure Violations (681 total)
- **23 unexpected directories** at plugin root (feature folders exist at root level instead of only Core/ECS/Godot/Tests/)
- **638 double-nested paths** (build artifacts, .godot/mono/temp/, legacy cs/ wrappers, deeply nested demo/test dirs)
- **14 Godot addons at non-canonical depth** (should be `Godot/addons/plugins/<name>`)
- Worst offender: GridPlacement (646 violations)

## Folder Structure Compliance
**Expected:** `Core/`, `ECS/`, `Godot/`, `Tests/` at plugin root — no `cs/` wrapper, no double-nested subdirs.
**Actual:** 681 violations.

## Framework Contracts
Bridge classes requiring review: `TimeProviderGameTimeAdapter`, `IPlacementInputBridge`, `PlacementSyncBridge`, `PlacementInputBridge`, `PlacementSceneAdapter`.

## License
- ✅ License compliant

## Last Audit
- **2026-04-22** by golden_trio_cron v4
