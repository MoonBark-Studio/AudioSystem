# Roadmap

## Current Structure

### cores/ (2 spokes)
- `MoonBark.ECS` - ECS library
- `MoonBark.Framework` - Shared utilities for MoonBark games

### games/ (2 spokes)
- `moonbark-idle` - Idle game
- `thistletide` - Thistletide game

### plugins/ (21 spokes)
Abilities, AudioSystem, EcsPhysics2D, EntitySensors, EntityTargetingSystem, GridPathfinding, GridPlacement, ItemVault, MoonBark.AI, MoonBark.Attributes, MoonBark.ItemDrops, MoonBark.Minimap, MoonBark.Quest, NetworkSync, PrototypeUI, RenderingOptimizations, TaskDistribution, TiledMapLoader, WorldGen2D, WorldState, WorldTime

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
