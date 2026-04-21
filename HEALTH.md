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
| MoonBark.ItemDrops | https://github.com/MoonBark-Studio/ItemDrops | main |
| MoonBark.Minimap | https://github.com/MoonBark-Studio/Minimap.git | master |
| MoonBark.Quest | https://github.com/MoonBark-Studio/Quest.git | main |
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

## Known Issues

- ⚠️ **Nested submodule:** `games/moonbark-idle/godot/assets/maps` has no URL configured
- ⚠️ **Branch inconsistency:** Some spokes track `master`, others track `main`
- ⚠️ **SSH/HTTPS mix:** TiledMapLoader and thistletide use SSH, others use HTTPS
- ⚠️ **csproj paths:** Framework paths updated locally but commits not pushed to all submodules

## Cross-Spoke CI/CD Validation

- [ ] Implement automated pipelines to test plugin spokes against game spokes
- [ ] All spokes should track stable branches, not detached commits

## Dependency & Namespace Audit

- [ ] Review for conflicting C# namespaces
- [ ] Check for duplicate Godot resource IDs

## Unresolved Nested Submodule

- `games/moonbark-idle/godot/assets/maps` - nested submodule with no .gitmodules entry
