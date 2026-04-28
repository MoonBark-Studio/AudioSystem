# GodotProjects

MoonBark Studio's Godot plugin ecosystem and game projects.

## Structure

For full submodule list → [HEALTH.md](./HEALTH.md).
For plugin integration status → [ROADMAP.md](./ROADMAP.md).

```
├── cores/           # Core libraries (no Godot dependencies)
│   └── MoonBark.Framework
├── games/           # Game projects
│   ├── moonbark-idle
│   └── thistletide
└── plugins/         # Godot plugins (24)
    ├── Abilities
    ├── AI
    ├── AudioSystem
    ├── Attributes
    ├── Economy
    ├── EcsPhysics2D
    ├── EntityTargetingSystem
    ├── GridPathfinding
    ├── GridPlacement
    ├── ItemDrops
    ├── ItemVault
    ├── Minimap
    ├── NetworkSync
    ├── PrototypeUI
    ├── Quest
    ├── RenderingOptimizations
    ├── Sensors
    ├── StatusEffects
    ├── TaskDistribution
    ├── Telemetry
    ├── TiledMapLoader
    ├── Upgrades
    ├── WorldGen2D
    ├── WorldState
    └── WorldTime
```

## Submodules

This repo uses git submodules. After cloning, run:

```bash
git submodule update --init --recursive
```
