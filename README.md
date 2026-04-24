# GodotProjects

MoonBark Studio's Godot plugin ecosystem and game projects.

## Structure

```
├── cores/           # Core libraries (no Godot dependencies)
│   ├── MoonBark.ECS
│   └── MoonBark.Framework
├── games/           # Game projects
│   ├── moonbark-idle
│   └── thistletide
└── plugins/         # Godot plugins
    ├── Abilities
    ├── AudioSystem
    ├── EcsPhysics2D
    ├── Sensors
    ├── EntityTargetingSystem
    ├── GridPathfinding
    ├── GridPlacement
    ├── ItemVault
    ├── MoonBark.AI
    ├── MoonBark.Attributes
    ├── MoonBark.ItemDrops
    ├── MoonBark.Minimap
    ├── MoonBark.Quest
    ├── NetworkSync
    ├── PrototypeUI
    ├── RenderingOptimizations
    ├── TaskDistribution
    ├── TiledMapLoader
    ├── WorldGen2D
    ├── WorldState
    ├── WorldTime
    └── moonbark-docs
```

## Submodules

This repo uses git submodules. After cloning, run:

```bash
git submodule update --init --recursive
```
