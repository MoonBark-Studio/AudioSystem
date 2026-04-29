# plugins — Roadmap

**Module:** `plugins`

## Per-Plugin Quality Scores (100 = perfect)

Scoring: Boundary 30% | Implementation Quality 30% | Testability 20% | Test Suite 20%.

| Plugin | Score | Tier |
|--------|-------|------|
| GridPlacement | 47 | CRITICAL |
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
| WorldTime | 73 | MEDIUM |
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

### P0 — GridPlacement Reconstruction
- Score: 47/100
- 35 ECS boundary violations — ECS types buried in Core/
- 646 structure violations — cs/ wrapper, 349 double-nested paths, build artifacts committed
- 16 catch-all Exception handlers
- 75 magic numbers (4+ digits)
- 7 async void methods
- 5 DeepInheritance chains in EventBus files
- **Action:** Needs dedicated restructuring sprint. Collapse cs/ wrapper, move ECS types to ECS/, audit all throw/catch patterns, replace magic numbers with named constants.

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

## TODO (unprioritized)

- [ ] Implement test suite for GridAgents (currently scaffold only)
- [ ] Audit AI plugin: 6 unexpected directories at root (EventWeaver/, GOAP/, Hybrid/, Shared/, UtilityAI/, src/)
- [ ] Audit Crafting plugin: 10 structure violations (cs/ wrapper)
- [ ] Audit WorldTime plugin: 78 structure violations (gdscript/, archived/, .godot/temp/)

## Future Work

- Establish per-plugin CI gating (tests must pass before merge)
- Move from manual golden trio audits to automated pre-commit hooks
- Standardize Godot addon layout (plugin.cfg at canonical path)
- Consider consolidating GridAgents + GridPathfinding (overlapping concerns)

## Changelog

### 2026-04-29
- Per-plugin quality scoring added to HEALTH/ROADMAP
- Consolidated duplicate changelog entries in ROADMAP
- 27 plugins scored across 4 dimensions (Boundary, Impl Quality, Testability, Test Suite)
- GridPlacement identified as critical (47/100), Crafting as best (96/100)

### 2026-04-22
- Audit run: 172 issues, 35 changed files
- ECS violations: 58
- IDisposable lifetime violations: 22
- Structure violations: 681
