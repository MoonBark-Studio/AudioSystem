# plugins — Roadmap

**Module:** `plugins`

## Architecture Standard (2026-05-06)

### Single ECS Backend with Beginner/Advanced Paths

All MoonBark plugins follow this architecture:

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Plugin Architecture                            │
│                                                                     │
│  ┌─────────────┐     ┌─────────────────────┐     ┌──────────────┐  │
│  │ Beginner API │────▶│   Godot Facade     │────▶│  ECS Backend │  │
│  │ (Simple)    │     │ (IManipulationSvc) │     │ (Friflo ECS) │  │
│  └─────────────┘     └─────────────────────┘     └──────────────┘  │
│                                                                     │
│  ┌─────────────┐     ┌─────────────────────┐     ┌──────────────┐  │
│  │Advanced API │────▶│   IEcsPlugin        │────▶│  EcsWorld    │  │
│  │ (Full ECS)  │     │ (RegisterSystems)  │     │ (Shared)     │  │
│  └─────────────┘     └─────────────────────┘     └──────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

**Beginner Path:** Facade with simple method names. Pre-built scenes. "It just works."

**Advanced Path:** Implement `IEcsPlugin`. Get full `EcsWorld` access. Custom systems, cross-plugin queries.

**Rationale:**
- Single ECS backend — one codebase, one architecture
- Facade hides ECS from beginners — no dual code paths
- Advanced users extend via `IEcsPlugin` — no lock-in
- Shared world enables cross-plugin entity queries

### Plugin Requirements

All plugins MUST:
1. Implement `IEcsPlugin` interface for ECS integration
2. Provide Godot facade (`I[Domain]Service`) for beginner use
3. Expose `EcsWorld.Store` for advanced users
4. Use `MoonBark.Framework.ECS` as the ECS foundation

## Per-Plugin Quality Scores (100 = perfect)

Scoring: Boundary 30% | Implementation Quality 30% | Testability 20% | Test Suite 20%.

| Plugin | Score | Tier |
|--------|-------|------|
| GridPlacement | 52 | MEDIUM |
| ItemDrops | 55 | LOW |
| Economy | 63 | LOW |
| EntityTargetingSystem | 63 | LOW |
| NetworkSync | 63 | LOW |
| PrototypeUI | 63 | LOW |
| RenderingOptimizations | 63 | LOW |
| StatusEffects | 63 | LOW |
| TaskDistribution | 63 | LOW |
| WorldState | 63 | LOW |
| GridAgents | 64 | LOW |
| Sensors | 69 | LOW |
| Telemetry | 69 | LOW |
| WorldGen2D | 69 | LOW |
| GridPathfinding | 70 | LOW |
| Abilities | 72 | MEDIUM |
| ItemVault | 73 | MEDIUM |
| WorldTime | 78 | MEDIUM | ✓ Framework.ECS |
| AI | 75 | MEDIUM |
| Upgrades | 75 | MEDIUM |
| AudioSystem | 79 | MEDIUM |
| MapLoader | 79 | MEDIUM |
| Minimap | 79 | MEDIUM |
| Attributes | 85 | HIGH |
| EcsPhysics2D | 85 | HIGH |
| Quest | 85 | HIGH |
| Crafting | 96 | HIGH |

## Critical Debt — Fix in Priority Order

### P0 — GridPlacement IEcsPlugin + Facade
- Score: 52/100
- **Action:** Implement `IEcsPlugin` for GridPlacement.ECS. Ensure facade properly hides ECS.

### P1 — ItemDrops ECS Boundary + Serialization
- Score: 55/100
- 7 ECS boundary violations in Core/Conditions/
- HardcodedSerializationKey [HIGH] in 5 files
- NullConditionalInHotPath [HIGH] in 14 files
- 65 structure violations
- **Action:** Move ECS types from Core/Conditions/ to ECS/. Replace hardcoded serialization keys with strongly-typed key constants.

### P2 — IDisposable Lifetime Fix (all plugins)
- 22 Godot Node files with event subscriptions missing `_ExitTree()`
- Nodes leak after removal from scene tree
- Worst: AudioSystem, GridPlacement, ItemDrops (7 files), ItemVault (4 files), Minimap (3 files), Quest (2 files)
- **Action:** Add `_ExitTree()` override to each, unsubscribe all delegates.

### P3 — WorldTime Nullable Reference Safety
- 6 NullConditionalInHotPath [HIGH] in large files (3000-11000+ chars)
- Files: LightingSystem.cs (11157 chars), WorldAgeSystem.cs (7225 chars)
- **Action:** Audit `?.`/`?[]` usages in hot-path files, add null guards.

### P4 — GridPathfinding ECS Boundary (3 violations)
- ECS types in Core/ instead of ECS/
- **Action:** Move to ECS/ and update namespaces.

### P5 — Abilities ECS Boundary (7 violations)
- ECS types in Core/Execution/ instead of ECS/
- **Action:** Move to ECS/ and update namespaces.

### P6 — Audit + Replace Magic Numbers (all plugins)
- 114 magic numbers across the codebase
- **Action:** Replace with named constants in relevant feature areas.

### P7 — Remove Console.WriteLine (20 occurrences)
- Debug output committed to source
- **Action:** Replace with proper logging framework calls.

### P8 — Catch-all Exception Handlers (33 occurrences)
- 16 in GridPlacement alone
- **Action:** Replace with specific exception types or result types.

### P9 — Empty Catch Blocks (4 occurrences)
- **Action:** Either log and rethrow, or remove entirely.

### P10 — Structure Cleanup (681 violations)
- Remove all `cs/` wrappers (Crafting, ItemDrops, Sensors, MapLoader, TiledMapLoader)
- Delete committed build artifacts (bin/, obj/, .godot/mono/temp/)
- Delete legacy archived/ directories (ItemDrops/archived, WorldTime/archived)
- Flatten Godot addon paths to canonical depth
- **Action:** Bulk cleanup with git rm + per-plugin restructuring.

## Action Items from Latest Audit
- [ ] Fix: Magic number (4+ digits) (43x)
- [ ] Fix: Property bag access ["key"] (36x)
- [ ] Fix: Console.WriteLine (17x)
- [ ] Fix: catch-all Exception (24x)
- [ ] Fix: Empty catch block (4x)
- [ ] Fix: Property bag (Dictionary) (1x)
- [ ] Fix: TODO comment (2x)
- [ ] ECS refactor: move 16 files with ECS types into ECS/ subdirectory
  - `plugins/Abilities/godot/scripts/AbilitiesPlugin.cs`
  - `plugins/Abilities/godot/scripts/AbilityDemo.cs`
  - `plugins/Abilities/godot/scripts/AbilitySignalBus.cs`
## TODO (unprioritized)

- [ ] Implement test suite for GridAgents (currently scaffold only)
- [ ] Audit AI plugin: 6 unexpected directories at root (EventWeaver/, GOAP/, Hybrid/, Shared/, UtilityAI/, src/)
- [ ] Audit Crafting plugin: 10 structure violations (cs/ wrapper)
- [ ] Audit WorldTime plugin: 78 structure violations (gdscript/, archived/, .godot/temp/)

## Future Work

- [x] GridPlacement: NodeManipulationService tests written (20 tests, needs Godot runtime to execute)
- Establish per-plugin CI gating (tests must pass before merge)
- Move from manual golden trio audits to automated pre-commit hooks
- Standardize Godot addon layout (plugin.cfg at canonical path)
- Consider consolidating GridAgents + GridPathfinding (overlapping concerns)

## Changelog
### 2026-05-07
- Audit run — 127 total issues (212 changed files)
- ECS violations: 16
- Framework bridge gaps: 0

<!-- previous entries preserved -->