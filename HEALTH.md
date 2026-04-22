# Health

## Overview

MoonBark Studio's Godot plugin ecosystem and game projects - organized as a hub-and-spoke architecture.

## Project Status

- **Last Updated:** 2026-04-21
- **Repository:** [GodotProjects](https://github.com/MoonBark-Studio/GodotProjects)
- **Main Branch:** main
- **Health:** ⚠️ Stable but needs attention (see below)

## Hub-Spoke Architecture

```
GodotProjects (hub)
├── cores/         (2 spokes)
├── games/         (2 spokes)
└── plugins/       (21 spokes)
```

## Submodules (25 total)

### cores/
| Spoke | URL | Status |
|-------|-----|--------|
| MoonBark.ECS | https://github.com/MoonBark-Studio/MoonBark.ECS | ✅ main |
| MoonBark.Framework | https://github.com/MoonBark-Studio/Framework | ✅ main |

### games/
| Spoke | URL | Status |
|-------|-----|--------|
| moonbark-idle | https://github.com/MoonBark-Studio/IdleGame.git | ⚠️ nested submodule issue |
| thistletide | git@github.com:MoonBark-Studio/thistletide-godot.git | ✅ master |

### plugins/
| Spoke | URL | Branch |
|-------|-----|--------|
| Abilities | https://github.com/MoonBark-Studio/Abilities.git | master |
| AudioSystem | https://github.com/MoonBark-Studio/AudioSystem.git | master |
| EcsPhysics2D | https://github.com/MoonBark-Studio/EcsPhysics2D.git | main |
| EntitySensors | https://github.com/MoonBark-Studio/EntitySensors.git | main |
| EntityTargetingSystem | https://github.com/MoonBark-Studio/EntityTargetingSystem.git | master |
| GridPathfinding | https://github.com/MoonBark-Studio/GridPathfinding.git | main |
| GridPlacement | https://github.com/MoonBark-Studio/GridPlacement.git | main |
| ItemVault | https://github.com/MoonBark-Studio/ItemVault.git | main |
| MoonBark.AI | https://github.com/MoonBark-Studio/MoonBark.AI | main |
| MoonBark.Attributes | https://github.com/MoonBark-Studio/MoonBark.Attributes | main |
| MoonBark.ItemDrops | https://github.com/MoonBark-Studio/ItemDrops.git | main |
| MoonBark.Minimap | https://github.com/MoonBark-Studio/Minimap.git | master |
| MoonBark.Quest | https://github.com/MoonBark-Studio/MoonBark.Quest.git | main |
| NetworkSync | https://github.com/MoonBark-Studio/NetworkSync.git | master |
| PrototypeUI | https://github.com/MoonBark-Studio/PrototypeUI.git | main |
| RenderingOptimizations | https://github.com/MoonBark-Studio/RenderingOptimizations.git | master |
| TaskDistribution | https://github.com/MoonBark-Studio/TaskDistribution.git | master |
| TiledMapLoader | git@github.com:MoonBark-Studio/TiledMapLoader.git | main |
| WorldGen2D | https://github.com/MoonBark-Studio/WorldGen2D | main |
| WorldState | https://github.com/MoonBark-Studio/WorldState.git | master |
| WorldTime | https://github.com/MoonBark-Studio/WorldTime | main |

## Recent Changes

- 2026-04-21: Reorganized into hub-and-spoke structure
- 2026-04-21: Moved MoonBark.Framework and MoonBark.ECS to cores/
- 2026-04-21: Removed moonbark-docs (docs moved to separate repo)
- 2026-04-21: Updated .gitmodules submodule names to match paths
- 2026-04-21: Fixed Framework project references (local csproj paths updated)
- 2026-04-21: Renamed MoonBark.* plugins to drop prefix (AI, Attributes, ItemDrops, Minimap, Quest)
- 2026-04-21: Fixed Framework duplicate glob (Core/Abstractions included twice)
- 2026-04-21: Fixed all csproj paths to point to cores/Framework
- 2026-04-21: TiledMapLoader — YATI plugin removed; replaced with `TiledMapLoaderStandalone` (pure C# JSON parser) + `GodotFileReader`. `IFileReader` abstraction in Core.
- 2026-04-21: TiledGroundLoader — fixed terrain IDs (Grass=0, Dirt=3 from TerrainTileSet.tres). sunhatch-glade GID 2 remapped to DirtTerrain.
- 2026-04-21: sunhatch-glade.tmj wired as first level (was test_farm.tmj). README example paths fixed (.tmx → .tmj).
- 2026-04-21: `TiledMapLoaderGodot.cs` marked `[Obsolete]`.
- 2026-04-21: WorldState.Tests xunit.runner.visualstudio upgraded 2.8.2→3.1.4 for SDK 10.0.103 compatibility.
- 2026-04-21: WorldTime Godot.csproj SDK upgraded 4.4.1→4.6.2.
- 2026-04-21: WorldGen2D — added `FastNoiseLite` 1.0.0 package (was placeholder stub).
- 2026-04-21: PrototypeUI — `net10.0` → `net8.0` to match ecosystem.
- 2026-04-21: moonbark-idle — broken `internal/BehaviorTrees` reference commented out (path doesn't exist).

## Discovered Issues

### Critical

- ❌ **Terrain mapping mismatch in TiledGroundLoader** — `sunhatch-glade.tmj` has 42 dirt-path cells as GID 2, but atlas col 1 = Path terrain (not Dirt). `TiledGroundLoader` had `GrassTerrain=3, DirtTerrain=2` but `TerrainTileSet.tres` defines Grass=0, Dirt=3. **FIXED** — terrain IDs corrected and GID 2 remapped to DirtTerrain.
- ❌ **WorldGen2D stub** — no noise library, placeholder only. **FIXED** — `FastNoiseLite` 1.0.0 package added.
- ❌ **TiledMapLoaderGodot orphaned** — YATI wrapper with no consumers. **FIXED** — marked `[Obsolete("Use TiledMapLoaderStandalone...")]`.

### Warnings

- ⚠️ **sunhatch-glade.tmj GID→terrain semantic mismatch** — map artist used GID 2 for dirt paths but tileset col 1 = Path. TiledGroundLoader remaps GID 2 → DirtTerrain at runtime. If tileset columns are rearranged, update the loader's GID switch. Consider aligning map GIDs with tileset columns to eliminate runtime remapping.
- ⚠️ **Branch inconsistency** — 10 plugins track `master`, 11 track `main`.
- ⚠️ **Nested submodule** — `games/moonbark-idle/godot/assets/maps` is a submodule (`tiled-maps-moonbark-idle.git`). Contains `sunhatch-glade.tmj` (24×32 production map) and `test_farm.tmj` (16×12 test stub). Run `git submodule update --init` on fresh clones.
- ⚠️ **GoDotTest DLL copy hardcoded** — `chickensoft.godottest\1.3.4\lib\net6.0\Chickensoft.GoDotTest.dll` fragile on version bumps.
- ⚠️ **MoonBark.AI broken reference** — `moonbark-idle.Core.csproj` referenced `internal/BehaviorTrees` which doesn't exist. Commented out. BehaviorTrees code is not present in the monorepo.
- ⚠️ **PrototypeUI net10.0→net8.0** — all plugins now target net8.0. **FIXED**.
- ⚠️ **ItemDrops Godot.Tests skips all integration and runtime tests**.

## sunhatch-glade.tmj — Map Analysis

| Property | Value |
|----------|-------|
| Dimensions | 24×32 tiles (384×512px logical) |
| Tile size | 16×16px |
| Orientation | orthogonal |
| Layers | Ground (768 tiles), Structures (768), SpawnPoints (768) |
| Tileset | Embedded `Terrain` — 4 tiles, 4 columns, `1_Terrains_16x16.png` |
| Ground GIDs | GID 1 → 726 cells (Grass), GID 2 → 42 cells (Dirt-path) |

**Tileset atlas → terrain mapping** (from `TerrainTileSet.tres`):
```
col 0 (tile 0, GID 1) → terrain 0 = Grass
col 1 (tile 1, GID 2) → terrain 1 = Path   ← sunhatch-glade uses GID 2 for dirt paths
col 2 (tile 2, GID 3) → terrain 2 = Water
col 3 (tile 3, GID 4) → terrain 3 = Dirt
```

**Note:** `TiledGroundLoader` remaps GID 2 → DirtTerrain so dirt cells visually render as dirt. GID 1 → GrassTerrain paints as grass correctly. No tile image mismatch — the tileset has all 4 terrain types; only Grass and Dirt are used in the map.

## Compilation Status

| Project | Status | Notes |
|---------|--------|-------|
| MoonBark.Framework | ✅ Builds | |
| GridPathfinding.Core | ✅ Builds | |
| GridPlacement.Core | ✅ Builds | |
| ItemVault.Core | ✅ Builds | |
| TiledMapLoader.Core | ✅ Builds | YATI-free; pure C# JSON |
| TiledMapLoader.Godot | ✅ Builds | GodotFileReader + orphaned TiledMapLoaderGodot.cs |
| moonbark-idle | ✅ Builds | test_farm.tmj wired as first level |
| WorldTime.Godot | ✅ SDK fixed | Upgraded 4.4.1→4.6.2 |
| WorldState.Core | ✅ Builds | |
| WorldState.Tests | ⚠️ Package fixed | xunit.runner 2.8.2→3.1.4 |
| WorldGen2D | ⚠️ Stub only | No noise library — placeholder |
| TaskDistribution.Core | ✅ Builds | |
| PrototypeUI.Godot | ⚠️ TargetFramework mismatch | net10.0 vs rest net8.0 |
| Attributes | ⚠️ No Godot csproj | Core only, no Godot integration |

## Plugin Health Summary

| Plugin | Health | Issue |
|--------|--------|-------|
| AI | ⚠️ Unused | Referenced but disabled in moonbark-idle |
| Abilities | ✅ | |
| Attributes | ⚠️ Core only | No Godot csproj |
| EcsPhysics2D | ⚠️ Box2D | Not wired into any game |
| EntitySensors | ✅ | |
| EntityTargetingSystem | ✅ | |
| GridPathfinding | ✅ | |
| GridPlacement | ✅ | |
| ItemDrops | ⚠️ Tests skipped | Integration/runtime excluded |
| Minimap | ✅ | |
| PrototypeUI | ⚠️ net10.0 | Inconsistent TargetFramework |
| Quest | ✅ | |
| RenderingOptimizations | ✅ | |
| TaskDistribution | ✅ | |
| TiledMapLoader | ✅ | YATI removed, standalone works |
| WorldGen2D | ❌ Stub | No implementation |
| WorldState | ✅ | |
| WorldTime | ✅ | SDK version fixed |

## Cross-Spoke CI/CD Validation

- [ ] Implement automated pipelines to test plugin spokes against game spokes
- [ ] All spokes should track stable branches, not detached commits

## Dependency & Namespace Audit

- [ ] Review for conflicting C# namespaces
- [ ] Check for duplicate Godot resource IDs

## Unresolved Nested Submodule

- `games/moonbark-idle/godot/assets/maps` — nested submodule with no .gitmodules entry. Contains only `test_farm.tmj`.
