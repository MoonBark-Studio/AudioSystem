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

## Key Types
## Key Types (3328 files, ~475720 lines)
AbilityAction, AbilityCommand, ActionContent, AgentState, CommandCompleted, CommandResult, CommandSource, CommandStarted, ConsoleFrameworkLogger, ConsoleFrameworkLoggerFactory, ContainerDefinition, ContainerId, CooldownInfo, DeferredComponentQueue, EffectContext, EffectTargetRequirements, EntityExtensions, EventBus, ExtendedCommandResult, FailureReason

## Namespaces
- `MoonBark.Framework.AI`
- `MoonBark.Framework.Camera`
- `MoonBark.Framework.Commands`
- `MoonBark.Framework.Core`
- `MoonBark.Framework.ECS`
- `MoonBark.Framework.Effects`
- `MoonBark.Framework.Events`
- `MoonBark.Framework.Exploration`
- `MoonBark.Framework.Godot`
- `MoonBark.Framework.Grids`
- `MoonBark.Framework.Items`
- `MoonBark.Framework.Logging`
- `MoonBark.Framework.Movement`
- `MoonBark.Framework.Pathfinding`
- `MoonBark.Framework.Slots`
- `or`

## ECS Architecture (v2)
- ECS subdirectories: cores/MoonBark.Framework/ECS, cores/MoonBark.Framework/Core/Abstractions/ECS, games/thistletide/cs/ECS, plugins/Abilities/ECS, plugins/AI/ECS, plugins/Attributes/ECS, plugins/Economy/ECS, plugins/EcsPhysics2D/ECS, plugins/GridPathfinding/ECS, plugins/GridPlacement/ECS, plugins/ItemVault/ECS, plugins/Minimap/ECS, plugins/Quest/ECS, plugins/StatusEffects/ECS, plugins/Upgrades/ECS, plugins/WorldTime/ECS, plugins/Crafting/cs/ECS, plugins/GridPlacement/cs/ECS, plugins/GridPlacement/Tests/ECS, plugins/GridPlacement/cs/test/ECS, plugins/GridPlacement/cs/Godot/test/ECS, plugins/GridPlacement/cs/test/Core/ECS, plugins/GridPlacement/Godot/test/ECS, plugins/GridPlacement/Tests/Core/ECS, plugins/ItemDrops/Tests/ECS, plugins/ItemVault/Tests/ECS, plugins/Sensors/cs/ECS, plugins/WorldTime/ECS/tests/ECS
- ECS files outside subdirectories: 101
- Flat structure: Core/, ECS/, Godot/ (cs/ prefix not required)

## Status
- ✅ Audited: 2026-04-30
- Changed files this run: 4
- File count: 3328 C# files (~475720 lines)