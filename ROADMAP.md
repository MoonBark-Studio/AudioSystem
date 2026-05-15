# Roadmap

## Current Structure

### cores/ (1 spoke)
- `MoonBark.Framework` - Shared utilities + ECS components for MoonBark games (includes merged `MoonBark.ECS`)

### games/ (2 spokes)
- `moonbark-idle` - Idle game
- `thistletide` - Thistletide game

### plugins/ (27 submodules)

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
| 12 | **MoonBark.Quest** | public | ✅ Integrated | Referenced in MoonBark.IdleGame.Core.csproj; quests.yaml wired (content TBD) |
| 13 | **MoonBark.MapLoader** | public | ✅ Integrated | TiledMapLoader.Godot referenced in Idle Godot csproj; TiledGroundLoader uses it |
| 14 | **MoonBark.WorldState** | public | ✅ Integrated | WorldState.Core referenced in MoonBark.IdleGame.Godot.csproj for global state query |
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
- [x] Fix MoonBark.IdleGame.Godot.slnx: TiledMapLoader → MapLoader folder name
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

## Fog of War Architecture (Completed 2026-05-07)

Fog of war is now a first-class Framework feature, NOT a separate plugin:

```
MoonBark.Framework/
├── Exploration/
│   ├── IFogOfWarProvider.cs      # Interface for fog state consumption
│   ├── FogOfWarState.cs          # Core state (HashSet<Vector2Int> explored tiles)
│   └── IFogOfWarRevealListener.cs # Event listener interface
├── ECS/
│   ├── VisionComponent.cs         # Reveal radius per entity
│   ├── FogOfWarRevealSystem.cs   # Generic system (games provide position extractor)
│   └── EcsFogOfWarAdapter.cs     # Adapter implementing IFogOfWarProvider
└── ...

MoonBark.Minimap/ (consumer)
├── Core/FogOfWar/FogOfWarState.cs # Inherits from Framework
└── Godot/FogOfWarOverlay.cs        # Visual rendering (uses IFogOfWarProvider)
```

**Key decisions:**
- FogOfWarState lives in Framework (gameplay state, not just visual)
- VisionComponent + FogOfWarRevealSystem are game-level (games provide position extractor)
- Minimap consumes IFogOfWarProvider (visual rendering only)

## Position SSOT Refactoring (In Progress 2026-05-07)

### Goal
Single source of truth for all positions: `TransformComponent2D` (2D) and `TransformComponent3D` (3D).

### Current Problems
- `TargetPositionComponent` (GridPlacement) duplicates position as Vector2Int grid
- `PositionComponent` aliases scattered across plugins (Sensors, GridPathfinding)
- Stale data risk when grid and world positions diverge
- 500+ position component usages across ecosystem

### Target State
```csharp
public struct TransformComponent2D : IComponent
{
    public Vector2F Position;  // World position (canonical SSOT)
    public float Rotation;

    // Grid conversion via floor (entity at tile center is at x.5)
    public Vector2Int GridPosition => new(
        (int)MathF.Floor(Position.X),
        (int)MathF.Floor(Position.Y)
    );

    public Vector2Int ToGridPosition(Vector2F tileSize) => new(
        (int)MathF.Floor(Position.X / tileSize.X),
        (int)MathF.Floor(Position.Y / tileSize.Y)
    );

    public static TransformComponent2D FromGrid(Vector2Int gridPos, Vector2F tileSize) => new()
    {
        Position = new Vector2F(gridPos.X * tileSize.X + tileSize.X * 0.5f,
                                 gridPos.Y * tileSize.Y + tileSize.Y * 0.5f),
        Rotation = 0f
    };
}
```

### Change Detection
- Use `entity.Entity.Id.version` (Friflo built-in) instead of `Revision` field
- For archetype-based dirty tracking: `store.Query<T>(Flags.Dirty archetype)`

### Migration Phases

| Phase | Target | Effort | Status |
|-------|--------|--------|--------|
| 1 | Framework: Add grid helpers to Transform2D/3D | 2h | 🔄 In Progress |
| 2 | GridPlacement: Deprecate TargetPositionComponent | 1-2 days | 📋 Pending |
| 3 | Sensors: Replace AgentPositionComponent | 4h | 📋 Pending |
| 4 | GridPathfinding: Replace GridPositionComponent | 4h | 📋 Pending |
| 5 | Thistletide: Replace all PositionComponent usages | 2-3 days | 📋 Pending |
| 6 | moonbark-idle: Replace all usages | 1 day | 📋 Pending |

### Files to Delete After Migration
- `plugins/GridPlacement/ECS/Components/TargetPositionComponent.cs`
- `plugins/Sensors/cs/ECS/Components/PositionComponent.cs`
- `plugins/GridPathfinding/ECS/Components/PositionComponent.cs`

### Effort: ~1 week total, ~112 files, ~7,600 lines

## Action Items from Latest Audit

### Critical (2026-05-07)
- [ ] **CommandBus thread safety**: RegisterHandler lacks lock (race condition)
- [ ] **Division by zero**: Minimap EcsTerrainAdapter, RenderingOptimizations SpatialPartitioning
- [ ] **Broken dead code**: ItemDropsSignalBus references non-existent FrameworkEventBus.PickupEvent
- [ ] **Plugin boundary violations**: Game code queries plugin ECS components directly

### High Priority
- [ ] **Duplicate IEffectsSink**: Two interfaces with same name in GridPlacement
- [ ] **Upgrades→Economy coupling**: Queries Economy ECS components directly
- [ ] **GridAgents empty group**: Plugin installs but does nothing
- [ ] **Missing concurrent tests**: CommandBus thread safety untested

### Medium Priority
- [ ] **ServiceRegistry thread safety**: Dictionary without locks
- [ ] **Framework Events dead code**: TimeEvents, InventoryEvents, CombatEvents have no consumers
- [ ] **DeathEvent no consumers**: Documented but not implemented by any plugin
- [ ] **TargetingService interface drift**: Methods not in interface
- [ ] **Abilities command handler mislocated**: Not a proper ECS system

### Code Smells (Volume)
- [ ] Fix: Console.WriteLine (36x)
- [ ] Fix: Property bag access ["key"] (90x)
- [ ] Fix: catch-all Exception (61x)
- [ ] Fix: Magic number (4+ digits) (224x)
- [ ] Fix: TODO comment (4x)
- [ ] Fix: Empty catch block (4x)
- [ ] Fix: NotImplementedException (1x)
- [ ] Fix: Property bag (Dictionary) (3x)
- [ ] ECS refactor: move 101 files with ECS types into ECS/ subdirectory
  - `games/moonbark-idle/cs/Core/EntityStoreFarmingExtensions.cs`
  - `games/moonbark-idle/cs/MoonBark.IdleGame.Tests/DebugDryCropTest.cs`
  - `games/moonbark-idle/cs/MoonBark.IdleGame.Tests/DiagnosticTest.cs`

## Health Score Progression
| Date | Score | Notes |
|------|-------|-------|
| 2026-05-07 | 91/100 | Thread safety, division guards, dead code removed |
| 2026-05-05 | 88/100 | Initial audit |
| Baseline | 78/100 | Pre-audit state |

## Changelog
### 2026-05-07 (continued)
- **Fog of War Architecture**: Moved FogOfWarState to Framework (was Minimap), added VisionComponent, FogOfWarRevealSystem, EcsFogOfWarAdapter
- **SSOT Position Refactoring**: Plan created - TransformComponent2D/3D will be single source of truth for all positions
- **Minimap Core Tests**: 26 tests passing
- **Framework Tests**: 609 tests passing

### 2026-05-07
- **Critical Risk Fixes**: CommandBus thread safety, division by zero guards, dead code removal
- **Test Coverage**: Framework 609 tests, WorldTime 519 tests all passing
- **API Contract Fixes**: Removed orphan interfaces, fixed naming collisions
- **Entity ID Standardization**: IMovementService now uses `int` (matches Frilfo ECS)
- Coverage audit: Framework.ECS at 100% coverage (562 tests)
- GridPathfinding.ECS: +26 new tests (MovementComponentsTests.cs, GridPathfindingModuleTests.cs)
- GridTypes verified as NOT dead code (used by GridPlacement and games)
- Pre-existing flaky test fixed (Obstacle_Changes_Affect_New_Paths_Not_Active_Ones)
- GridPlacement coverage: 32.8% line coverage (requires Godot runtime for higher coverage)

### 2026-05-05
- Coverage audit: Framework.ECS at 100% coverage (562 tests)
- GridPathfinding.ECS: +26 new tests (MovementComponentsTests.cs, GridPathfindingModuleTests.cs)
- GridTypes verified as NOT dead code (used by GridPlacement and games)
- Pre-existing flaky test fixed (Obstacle_Changes_Affect_New_Paths_Not_Active_Ones)
- GridPlacement coverage: 32.8% line coverage (requires Godot runtime for higher coverage)

### 2026-04-30
- Audit run — 423 total issues (4 changed files)
- ECS violations: 101
- Framework bridge gaps: 0

<!-- previous entries preserved -->