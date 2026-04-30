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
| 12 | **MoonBark.Quest** | public | ✅ Integrated | Referenced in MoonBark.Idle.Core.csproj; quests.yaml wired (content TBD) |
| 13 | **MoonBark.MapLoader** | public | ✅ Integrated | TiledMapLoader.Godot referenced in Idle Godot csproj; TiledGroundLoader uses it |
| 14 | **MoonBark.WorldState** | public | ✅ Integrated | WorldState.Core referenced in MoonBark.Idle.Godot.csproj for global state query |
| 15 | **MoonBark.WorldGen2D** | public | 🔜 Not integrated yet | Level is hand-crafted (sunhatch-glade.tmj); procedural gen not needed yet |
| 16 | **MoonBark.Minimap** | public | 🔜 Not integrated yet | UI feature not yet built |
| 17 | **MoonBark.AudioSystem** | public | 🔜 Not integrated yet | No audio in idle game yet |
| 18 | **MoonBark.ItemDrops** | public | 🔜 Not integrated yet | Drop-on-death not applicable to idle game |
| 19 | **MoonBark.NetworkSync** | public | ❌ Won't integrate | Server/authoritative arch not applicable to single-player idle |
| 20 | **MoonBark.EcsPhysics2D** | public | ❌ Won't integrate | No physics in idle game (top-down tile grid) |
| 21 | **MoonBark.EntityTargetingSystem** | public | ❌ Won't integrate | No combat; task-targeting via TaskDistribution |
| 22 | **MoonBark.Sensors** | public | ❌ Won't integrate | No spatial sensing; grid-based position queries |
| 23 | **MoonBark.RenderingOptimizations** | public | ❌ Won't integrate | Godot-rendering concern, not ECS game logic |
| 24 | **MoonBark.PrototypeUI** | public | ❌ Won't integrate | Idle game uses Godot UI directly |

Legend:
- ✅ **Integrated** — referenced in csproj and actively used
- 🔜 **Not integrated yet** — plugin exists, not yet wired to game
- ❌ **Won't integrate** — architectural mismatch; plugin serves a different game type

## Plugin Integration Status — thistletide

| # | Plugin | Status | Notes |
|---|--------|--------|-------|
| 1 | **MoonBark.AI** | ✅ Integrated | GOAP/UtilityAI referenced in Thistletide.Core |
| 2 | **MoonBark.Abilities** | ✅ Integrated | AbilityCommandHandler wired in ECS autoload |
| 3 | **MoonBark.Attributes** | ✅ Integrated | HungerComponent, HealthComponent in ECS |
| 4 | **MoonBark.AudioSystem** | ✅ Integrated | AudioSystem.Core (Core) + AudioSystem.Godot (Godot layer) |
| 5 | **MoonBark.EcsPhysics2D** | ✅ Integrated | Box2D physics bridge to ECS |
| 6 | **MoonBark.EntityTargetingSystem** | ✅ Integrated | Target acquisition for combat |
| 7 | **MoonBark.GridAgents** | ✅ Integrated | Grid-based agent movement recovery |
| 8 | **MoonBark.GridPathfinding** | ✅ Integrated | A* pathfinding ECS systems |
| 9 | **MoonBark.GridPlacement** | ✅ Integrated | Placement Core + ECS in Thistletide.Core |
| 10 | **MoonBark.ItemDrops** | ✅ Integrated | Drop tables referenced in Core |
| 11 | **MoonBark.ItemVault** | ✅ Integrated | ECS inventory system |
| 12 | **MoonBark.NetworkSync** | ✅ Integrated | Snapshot interpolation for networked state |
| 13 | **MoonBark.PrototypeUI** | ✅ Integrated | Core UI contracts in Thistletide.Godot (fixed 2026-05: broken addon path → spoke) |
| 14 | **MoonBark.RenderingOptimizations** | ✅ Integrated | Culling in Thistletide.Godot (fixed 2026-05: wrong `internal/` path → `plugins/`) |
| 15 | **MoonBark.Sensors** | ✅ Integrated | Raycast/area sensing Core + ECS |
| 16 | **MoonBark.TaskDistribution** | ✅ Integrated | Work assignment to agents |
| 17 | **MoonBark.WorldState** | ✅ Integrated | Global game state registry |
| 18 | **MoonBark.Framework** | ✅ Integrated | Always integrated |
| 19 | **MoonBark.Quest** | 🔜 Not integrated yet | Quest system not wired in Thistletide |
| 20 | **MoonBark.StatusEffects** | 🔜 Not integrated yet | Buff/debuff system not wired yet |
| 21 | **MoonBark.Upgrades** | 🔜 Not integrated yet | Upgrade system not wired yet |
| 22 | **MoonBark.WorldTime** | 🔜 Not integrated yet | Day/night cycle not wired yet |
| 23 | **MoonBark.Economy** | 🔜 Not integrated yet | Resource production not wired yet |
| 24 | **MoonBark.MapLoader** | 🔜 Not integrated yet | Thistletide uses direct tilemaps |
| 25 | **MoonBark.Minimap** | 🔜 Not integrated yet | UI feature not yet built |
| 26 | **MoonBark.WorldGen2D** | 🔜 Not integrated yet | Procedural gen not needed for current prototype |

## What's Next

### Phase 0: Reference Hygiene (partially complete 2026-05)
- [x] Fix Thistletide.Godot.csproj: PrototypeUI broken addon path → spoke reference
- [x] Fix Thistletide.Godot.csproj: RenderingOptimizations `internal/` path → `plugins/`
- [x] Fix Thistletide.Godot.csproj: remove wrong/redundant GridPlacement.Core reference
- [x] Fix Thistletide.Godot.csproj: SDK 4.6.1 → 4.6.2 to match ecosystem
- [x] Fix Thistletide.Godot.csproj: re-enable solution build (remove BuildProjectWhenBuildingSolution=false)
- [x] Fix Thistletide.Godot.Tests.csproj: same PrototypeUI reference + dead compile glob
- [x] Fix Thistletide.slnx: remove non-existent spoke refs, correct all plugin paths
- [x] Fix MoonBark.Idle.Godot.slnx: TiledMapLoader → MapLoader folder name
- [x] Add Directory.Build.props parent-import chain (hub → game level)
- [x] Hub Directory.Build.props: add canonical NuGet version properties
- [x] Hub Directory.Build.props: fix SylvesVendorPath (was ../../vendor/, now ../vendor/)
- [ ] Thistletide.Godot: align GridPlacement API (ModeService, GridMode API drift in calling code)
- [ ] Thistletide.Godot: resolve PlayerInputComponent missing type in PlayerInputBridge.cs
- [ ] Migrate existing csproj PackageReference versions to use hub-level version properties

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
