# Plugin → Game Integration Map

**Date:** 2026-04-29
**Status:** Current — reflects actual ProjectReference in `.csproj` files

---

## Summary

| Plugin | Health | MoonBark Idle | Thistletide | Notes |
|--------|--------|---------------|-------------|-------|
| **Framework** | — | ✅ Core only | ✅ Core only | `CoreVector2I`, `IEventBus`, `IWorldView`, `ISimulationSystem` |
| **AI** | — | ❌ Removed | ✅ Core | GOAP planner — removed from idle (uses priority ladder); active in Thistletide |
| **Abilities** | 82 | ❌ | ✅ Core (wired) | Ability system with cooldowns, mana costs — better fit for action RPG |
| **Attributes** | 55 | ⚠️ Not wired | ✅ Core (wired) | Health/Energy/Mana components — valuable for Thistletide combat; referenced in idle but unused |
| **AudioSystem** | 52 | ⚠️ Not wired | ✅ Godot (wired) | JSON-configured audio cues — both games could use |
| **Crafting** | 65 | ❌ | ❌ Not wired | Recipe-based crafting with ItemVault — genre mismatch for both games |
| **Economy** | 48 | ✅ ECS (wired) | ❌ Not wired | Passive production, resource pools — core to idle, not action RPG |
| **EcsPhysics2D** | 58 | ❌ | ✅ Core (wired) | Box2D + Friflo ECS bridge — Thistletide only |
| **EntityTargetingSystem** | 52 | ❌ | ✅ Core (wired) | Faction-based targeting — Thistletide only |
| **GridAgents** | 100 | ❌ Not wired | ✅ Core (wired) | Reusable agent runtime scaffold — both games could benefit |
| **GridPathfinding** | 100 | ✅ Core (wired) | ✅ Core (wired) | Hierarchical A* — both games |
| **GridPlacement** | — | ✅ ECS + Godot.Views (wired) | ✅ Core (wired) | Building/placement validation — both games |
| **ItemDrops** | 60 | ❌ Not wired | ✅ Core (wired) | Loot tables, conditional drops — Thistletide action content |
| **ItemVault** | 58 | ✅ Core (wired) | ✅ Core (wired) | Inventory containers — both games |
| **MapLoader** | 50 | ✅ Godot (wired) | ❌ Not wired | Tiled `.tmj` loader — idle ground layer; Thistletide uses custom terrain |
| **Minimap** | 58 | ⚠️ Not wired | ❌ Not wired | Fog-of-war minimap — could enhance both |
| **NetworkSync** | 58 | ❌ Not wired | ✅ Core (wired) | LiteNetLib multiplayer — Thistletide only |
| **PrototypeUI** | 55 | ⚠️ Not wired | ✅ Godot (wired) | Lightweight HUD scaffolding — idle could use for debug UI |
| **Quest** | 60 | ✅ Core (wired) | ❌ Not wired | Quest YAML system — idle has quests; Thistletide may want |
| **RenderingOptimizations** | 55 | ❌ Not wired | ✅ Godot (wired) | ECS rendering optimizations — Thistletide only |
| **Sensors** | 62 | ❌ Not wired | ✅ Core (wired) | Visual sensor detection for AI — Thistletide only |
| **StatusEffects** | 50 | ✅ Core (wired) | ❌ Not wired | Buff/attribute modifiers — idle has speed buffs, deposit multipliers |
| **TaskDistribution** | 60 | ✅ Core (wired) | ✅ Core (wired) | Task board + claim system — both games |
| **Telemetry** | 70 | ✅ Editor plugin | ❌ Not wired | Runtime perf monitoring — both games benefit |
| **Upgrades** | 65 | ✅ ECS (wired) | ❌ Not wired | Cost-scaling purchase system — idle upgrade shop |
| **WorldGen2D** | 55 | ❌ Not wired | ❌ Not wired | Procedural heightmap/biome generation — neither; MapLoader for idle, Thistletide uses custom |
| **WorldState** | 52 | ⚠️ ProjectRef present | ✅ Core (wired) | KV state store — Thistletide active; idle has ref but unverified |
| **WorldTime** | 95 | ✅ Core + ECS + Godot.Views (wired) | ❌ Not wired | Calendar/time types — idle aging/crops; Thistletide needs time for day/night |

---

## MoonBark Idle — Plugin Integration Detail

### ✅ Active (wired and used)

| Plugin | Layer | Usage |
|--------|-------|-------|
| MoonBark.Framework | Core | `CoreVector2I`, `IReadOnlyWorldView`, `IEventBus`, `ISimulationSystem`, wiring bootstrap |
| GridPlacement | ECS + Godot.Views | `PlacementSystem`, `GridOccupancySystem` — farming plots, sell bins, pantries |
| GridPathfinding | Core | `IPathfinder` — agent A* movement |
| TaskDistribution | Core | `TaskClaimSystem`, `ITaskRequestPolicy` — agent-to-task matching |
| WorldTime | Core + ECS + Godot.Views | `WorldTimeIntegration`, `AgingComponent` — crop growth, harvest timing |
| StatusEffects | Core | `AttributeModifierSystem` — speed buffs, deposit multipliers |
| Economy | ECS | Passive production, `ResourceComponent`-based |
| Upgrades | ECS | Cost-scaling purchase system |
| ItemVault | Core | Inventory storage backend |
| Quest | Core | Quests loaded from `quests.yaml` via `QuestIntegration` |
| Telemetry | Editor plugin | `MoonBark.Telemetry.Godot` enabled in Godot editor |
| MapLoader | Godot | `TiledGroundLoader` — loads `.tiled` ground layer maps |

### ⚠️ ProjectRef present — verify before using

| Plugin | Layer | Notes |
|--------|-------|-------|
| WorldState | Core | `ProjectReference` present — state store; verify active integration |
| Attributes | Core | Referenced by StatusEffects/WorldTime but not directly used in game code |

### ❌ Not wired (plugins exist, not integrated)

| Plugin | Health | Should integrate? | Reason |
|--------|--------|-------------------|--------|
| AudioSystem | 52 | Consider | JSON-configured audio cues — would enrich idle SFX (coin pickup, crop harvest, notifications) |
| Minimap | 58 | Consider | Fog-of-war minimap — low priority for idle; camera viewport covers most needs |
| PrototypeUI | 55 | Consider | Lightweight HUD scaffolding — could replace hand-rolled debug panels |
| NetworkSync | 58 | No | LiteNetLib multiplayer — idle is single-player |
| WorldGen2D | 55 | No | Procedural generation — idle uses handcrafted Tiled maps via MapLoader |
| Sensors | 62 | No | Visual AI sensors — idle agents use TaskDistribution, not AI vision |
| EntityTargetingSystem | 52 | No | Faction-based targeting — no combat system in idle |
| ItemDrops | 60 | No | Loot tables — idle uses direct resource production via Economy |
| EcsPhysics2D | 58 | No | Box2D physics — idle uses grid-based logic, not physics simulation |
| Abilities | 82 | No | Ability system — idle has no combat/ability casting |
| Crafting | 65 | No | Recipe crafting — idle has direct resource conversion; no crafting UI |
| RenderingOptimizations | 55 | No | ECS rendering optimizations — Thistletide focus |

---

## Thistletide — Plugin Integration Detail

### ✅ Active (wired and used)

| Plugin | Layer | Usage |
|--------|-------|-------|
| MoonBark.Framework | Core | `CoreVector2I`, `IEventBus`, `IWorldView`, wiring bootstrap |
| AI | Core | `GOAPPlanner` — agent decision making |
| WorldState | Core | Global/faction KV state store |
| TaskDistribution | Core | Task board, agent assignment |
| ItemDrops | Core | Weighted loot tables, conditional drops for enemies/chests |
| Abilities | Core | Castable abilities with mana/cooldown |
| EntityTargetingSystem | Core | Faction-based targeting validation |
| AudioSystem | Godot | JSON audio cue routing |
| GridPlacement | Core | Building placement and occupancy |
| GridPathfinding | Core | Hierarchical A* pathfinding |
| GridAgents | Core | Reusable agent runtime scaffold |
| RenderingOptimizations | Godot | ECS rendering optimizations |
| NetworkSync | Core | LiteNetLib multiplayer |
| Sensors | Core | Visual sensor detection for AI agents |
| ItemVault | Core | Inventory containers |
| Attributes | Core | Health/Energy/Mana components |
| EcsPhysics2D | Core | Box2D physics integration |

### ❌ Not wired (plugins exist, not integrated)

| Plugin | Health | Should integrate? | Reason |
|--------|--------|-------------------|--------|
| WorldTime | 95 | **Yes — high priority** | Calendar/time types — day/night cycle, ability cooldowns, buff durations |
| WorldGen2D | 55 | Consider | Procedural terrain — Thistletide uses custom terrain system; MapLoader not applicable |
| Telemetry | 70 | Consider | Perf monitoring — would help optimize ECS systems at scale |
| Quest | 60 | Consider | Quest YAML system — Thistletide may want quest content without GOAP complexity |
| Minimap | 58 | Consider | Fog-of-war minimap — fits action RPG exploration |
| PrototypeUI | 55 | Consider | HUD scaffolding — Thistletide has Godot-native UI; PrototypeUI for debug tools |
| StatusEffects | 50 | No | Buff system — Thistletide uses Abilities for effects; different design |
| Economy | 48 | No | Passive production — no idle economy in action RPG |
| Upgrades | 65 | No | Cost-scaling purchases — Thistletide uses character progression via Abilities/Attributes |
| Crafting | 65 | No | Recipe crafting — no crafting system designed for Thistletide |
| AudioSystem | 52 | Already wired | Already wired via Godot layer |
| MapLoader | 50 | No | Tiled loader — Thistletide uses custom terrain; MapLoader is idle-specific |

---

## Cross-Game Plugin Matrix

| Plugin | Idle | Thistletide | Shared? |
|--------|------|-------------|---------|
| Framework | ✅ Core | ✅ Core | **Shared** — `cores/MoonBark.Framework/` |
| GridPathfinding | ✅ Core | ✅ Core | **Shared** — both use A* |
| GridPlacement | ✅ ECS+Views | ✅ Core | **Shared** — different layers |
| TaskDistribution | ✅ Core | ✅ Core | **Shared** — task board |
| ItemVault | ✅ Core | ✅ Core | **Shared** — inventory |
| WorldState | ⚠️ unverified | ✅ Core | **Shared** — verify idle |
| AI | ❌ | ✅ Core | Thistletide only |
| Abilities | ❌ | ✅ Core | Thistletide only |
| Attributes | ⚠️ unused | ✅ Core | **Shared** — wire in idle |
| AudioSystem | ❌ | ✅ Godot | Thistletide only |
| EcsPhysics2D | ❌ | ✅ Core | Thistletide only |
| EntityTargetingSystem | ❌ | ✅ Core | Thistletide only |
| GridAgents | ❌ | ✅ Core | Thistletide only |
| ItemDrops | ❌ | ✅ Core | Thistletide only |
| NetworkSync | ❌ | ✅ Core | Thistletide only |
| Sensors | ❌ | ✅ Core | Thistletide only |
| RenderingOptimizations | ❌ | ✅ Godot | Thistletide only |
| WorldTime | ✅ Core+ECS+Views | ❌ | Idle only |
| StatusEffects | ✅ Core | ❌ | Idle only |
| Economy | ✅ ECS | ❌ | Idle only |
| Upgrades | ✅ ECS | ❌ | Idle only |
| Quest | ✅ Core | ❌ | Idle only |
| MapLoader | ✅ Godot | ❌ | Idle only |
| Telemetry | ✅ Editor plugin | ❌ | Idle only |
| Crafting | ❌ | ❌ | Neither |
| Minimap | ❌ | ❌ | Neither |
| PrototypeUI | ❌ | ✅ Godot | Thistletide only |
| WorldGen2D | ❌ | ❌ | Neither |
| AudioSystem (idle) | ❌ | — | Could integrate |

---

## Priority Integration Candidates

### For MoonBark Idle

1. **Attributes** — `HealthComponent`, `EnergyComponent` wired into agent entities; StatusEffects references it but it's not in the csproj. Low effort.
2. **AudioSystem** — JSON audio cues for game events. Low complexity, high feedback value.
3. **WorldState** — Verify `ProjectReference` is actually active; if so, document in plugin-connections.md.
4. **Minimap** — Low priority; idle camera covers most needs.

### For Thistletide

1. **WorldTime** — High value. Day/night cycle, ability durations, buff timers. `WorldTime.Core + ECS` layers — matches Thistletide's existing plugin structure.
2. **Quest** — YAML quest system could complement Thistletide's content without GOAP complexity.
3. **Telemetry** — Perf monitoring for ECS systems. Good for optimization work.
4. **PrototypeUI** — Debug HUD scaffolding for development.

---

*Last Updated: 2026-04-29*
