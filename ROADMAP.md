# Roadmap

## Current Structure

### cores/ (1 spoke)
- `MoonBark.Framework` - Shared utilities + ECS components for MoonBark games (includes merged `MoonBark.ECS`)

### games/ (2 spokes)
- `moonbark-idle` - Idle game
- `thistletide` - Thistletide game

### plugins/ (21 public + 3 internal = 24 directories)

## Plugin Integration Status — moonbark-idle

| # | Plugin | Type | Status | Notes |
|---|--------|------|--------|-------|
| 1 | **MoonBark.Attributes** | public | ✅ Integrated | HealthComponent, HungerComponent, NeedDecaySystem — fully wired |
| 2 | **MoonBark.StatusEffects** | public | ✅ Integrated | StatusEffectsModule registered; well_fed buff active |
| 3 | **MoonBark.GridPathfinding** | public | ✅ Integrated | IPathfinder; GridPathfinder instantiation in harness |
| 4 | **MoonBark.GridPlacement** | public | ✅ Integrated | PlacementSystem, GridOccupancySystem, ECS components |
| 5 | **MoonBark.ItemVault** | public | ✅ Integrated | EcsInventorySystem, InventoryOwnerComponent |
| 6 | **MoonBark.WorldTime** | public | ✅ Integrated | WorldTimeIntegration, IGameTimeSource bridge |
| 7 | **MoonBark.TaskDistribution** | public | ✅ Integrated | TaskDistributionSystems, TaskClaim, FarmingTaskSystem |
| 8 | **MoonBark.AI** | public | ✅ Integrated | Chickensoft LogicBlocks (GOAP planner via BehaviorDispatcher) |
| 9 | **MoonBark.Economy** | public | ✅ Integrated | Passive production, resource pools |
| 10 | **MoonBark.Upgrades** | public | ✅ Integrated | Cost-scaling purchase system |
| 11 | **MoonBark.Framework** | core | ✅ Integrated | Always integrated (CoreVector2I, BaseComponent, ISimulationSystem, etc.) |
| 12 | **MoonBark.Quest** | public | 🔜 Not integrated yet | No references in moonbark-idle |
| 13 | **MoonBark.TiledMapLoader** | public | 🔜 Not integrated yet | Level loading uses custom TiledGroundLoader |
| 14 | **MoonBark.WorldGen2D** | public | 🔜 Not integrated yet | Level is hand-crafted (sunhatch-glade.tmj) |
| 15 | **MoonBark.Minimap** | public | 🔜 Not integrated yet | UI feature, not yet built |
| 16 | **MoonBark.AudioSystem** | public | 🔜 Not integrated yet | No audio in idle game yet |
| 17 | **MoonBark.FogOfWar** | public | 🔜 Not integrated yet | No fog mechanic in idle game |
| 18 | **MoonBark.ItemDrops** | public | 🔜 Not integrated yet | Drop-on-death not applicable to idle game |
| 19 | **MoonBark.WorldState** | public | 🔜 Not integrated yet | No multi-world state tracking |
| 20 | **MoonBark.NetworkSync** | internal | ❌ Won't integrate | Server/authoritative架构 not applicable to single-player idle |
| 21 | **MoonBark.EcsPhysics2D** | internal | ❌ Won't integrate | No physics in idle game (top-down tile grid) |
| 22 | **MoonBark.EntityTargetingSystem** | internal | ❌ Won't integrate | No combat; task-targeting via TaskDistribution |
| 23 | **MoonBark.Sensors** | internal | ❌ Won't integrate | No spatial sensing; grid-based position queries |
| 24 | **MoonBark.RenderingOptimizations** | internal | ❌ Won't integrate | Godot-rendering concern, not ECS game logic |
| 25 | **MoonBark.PrototypeUI** | internal | ❌ Won't integrate | One-off prototype; idle uses Godot UI directly |

Legend:
- ✅ **Integrated** — referenced in `MoonBark.Idle.Core.csproj` and actively used
- 🔜 **Not integrated yet** — plugin exists, not yet wired to moonbark-idle
- ❌ **Won't integrate** — architectural mismatch; plugin serves a different game type

## What's Next

### Phase 1: Hub Stabilization
- [x] Reorganize into cores/games/plugins structure
- [x] Align .gitmodules submodule names with paths
- [ ] Fix nested submodule in moonbark-idle/godot/assets/maps
- [ ] Standardize branch tracking (all spokes → main or all → master)
- [ ] Unify SSH/HTTPS for all remote URLs

### Phase 2: Cross-Spoke Integration
- [ ] Validate all csproj references work after Framework path changes
- [ ] Set up CI/CD to test plugin spokes against game spokes
- [ ] Establish submodule update workflow

### Phase 3: Developer Experience
- [ ] PowerShell scripts for scaffolding new spokes
- [ ] Standardize linting/formatting across all spokes
- [ ] Document spoke creation/removal procedures

## Missing Spokes

These repos may exist but are not yet linked:
- `plugins/Configuration` - repo may be private
- `plugins/MoonBark.FogOfWar` - repo may not exist yet
