# Plugin → Game Integration Map

**Date:** 2026-04-29
**Status:** Current — reflects actual ProjectReference in `.csproj` files

---

## Summary

|| Plugin | Health | MoonBark Idle | Thistletide | Notes |
|--------|--------|---------------|-------------|-------|
|| **Framework** | — | ✅ Core only | ✅ Core only | `CoreVector2I`, `IEventBus`, `IWorldView`, `ISimulationSystem` |
|| **AI** | — | ❌ Removed | ✅ Core | GOAP planner — removed from idle (uses priority ladder) |
|| **Abilities** | 82 | ❌ Wrong genre | ✅ Core (wired) | Ability system with cooldowns, mana — action RPG only |
|| **Attributes** | 55 | ⚠️ Not wired | ✅ Core (wired) | Health/Energy/Mana — Thistletide combat; idle has stub ref |
|| **AudioSystem** | 52 | ❌ Wrong genre | ✅ Godot (wired) | JSON audio — both games could use; low priority for idle |
|| **Crafting** | 65 | ❌ Wrong genre | ❌ Wrong genre | Recipe-based crafting — neither game has crafting UI or recipes |
|| **Economy** | 48 | ✅ ECS (wired) | ❌ Wrong genre | Passive production, resource pools — idle only |
|| **EcsPhysics2D** | 58 | ❌ Wrong genre | ✅ Core (wired) | Box2D + Friflo ECS — action RPG physics only |
|| **EntityTargetingSystem** | 52 | ❌ Wrong genre | ✅ Core (wired) | Faction-based targeting — combat/action only |
|| **GridAgents** | 100 | ❌ Wrong game | ✅ Core (wired) | Agent runtime scaffold — could benefit idle but not wired |
|| **GridPathfinding** | 100 | ✅ Core (wired) | ✅ Core (wired) | Hierarchical A* — both games |
|| **GridPlacement** | — | ✅ ECS + Godot.Views (wired) | ✅ Core (wired) | Building/placement — both games |
|| **ItemDrops** | 60 | ❌ Wrong genre | ✅ Core (wired) | Loot tables — idle uses direct production, not drops |
|| **ItemVault** | 58 | ✅ Core (wired) | ✅ Core (wired) | Inventory containers — both games |
|| **MapLoader** | 50 | ✅ Godot (wired) | ❌ Wrong genre | Tiled `.tmj` loader — idle ground maps only |
|| **Minimap** | 58 | ❌ Not needed | ❌ Not needed | Fog-of-war minimap — neither game has explored exploration |
|| **NetworkSync** | 58 | ❌ Wrong genre | ✅ Core (wired) | LiteNetLib multiplayer — action RPG only |
|| **PrototypeUI** | 55 | ❌ Not needed | ✅ Godot (wired) | Lightweight HUD — Thistletide debug; idle uses native UI |
|| **Quest** | 60 | ✅ Core (wired) | ❌ Not wired | Quest YAML — idle has quests; Thistletide uses GOAP/abilities |
|| **RenderingOptimizations** | 55 | ❌ Wrong game | ✅ Godot (wired) | ECS rendering opts — Thistletide only |
|| **Sensors** | 62 | ❌ Wrong genre | ✅ Core (wired) | Visual AI sensors — idle agents use TaskDistribution, not vision |
| StatusEffects | 50 | ✅ Core (wired) | ❌ Not wired | Buff/attribute modifiers — complements Abilities in Thistletide (abilities trigger effects → effects apply status) |
|| **TaskDistribution** | 60 | ✅ Core (wired) | ✅ Core (wired) | Task board + claim system — both games |
|| **Telemetry** | 70 | ✅ Editor plugin | ❌ Not wired | Perf monitoring — editor tool; both games could use |
|| **Upgrades** | 65 | ✅ ECS (wired) | ❌ Wrong genre | Cost-scaling purchases — idle upgrade shop only |
|| **WorldGen2D** | 55 | ❌ Wrong genre | ❌ Wrong genre | Procedural terrain — neither game; MapLoader for idle, custom terrain for Thistletide |
|| **WorldState** | 52 | ⚠️ Unverified | ✅ Core (wired) | KV state store — Thistletide active; idle has ref but unverified |
|| **WorldTime** | 95 | ✅ Core + ECS + Godot.Views (wired) | ❌ Not wired | Calendar/time — idle aging/crops; Thistletide needs day/night |

---

## What Belongs Where

### MoonBark Idle — Genre: Idle / Tycoon

Idle mechanics: passive production, time-driven growth, resource conversion, upgrades, quests, grid placement.

| Plugin | Verdict | Reason |
|--------|---------|--------|
| Framework | ✅ Belongs | ECS infrastructure, shared types |
| GridPathfinding | ✅ Belongs | Agent pathfinding |
| GridPlacement | ✅ Belongs | Building and plot placement |
| TaskDistribution | ✅ Belongs | Agent task assignment |
| ItemVault | ✅ Belongs | Inventory backend |
| WorldTime | ✅ Belongs | Crop growth, harvest timing, game clock |
| StatusEffects | ✅ Belongs | Speed buffs, deposit multipliers on agents |
| Economy | ✅ Belongs | Passive resource production |
| Upgrades | ✅ Belongs | Upgrade shop with exponential cost scaling |
| Quest | ✅ Belongs | Quest system from `quests.yaml` |
| MapLoader | ✅ Belongs | Tiled ground layer maps |
| Telemetry | ✅ Belongs | Editor-only telemetry plugin |
| WorldState | ⚠️ Verify | ProjectReference present — confirm actual usage |
| Attributes | ❌ Don't wire | No combat; agents don't have health/energy pools |
| AudioSystem | ❌ Don't wire | Nice-to-have but not needed; no SFX design in scope |
| PrototypeUI | ❌ Don't wire | Idle has native Godot UI; no debug HUD gap |
| Minimap | ❌ Don't wire | Idle camera covers viewport; no exploration |
| AI | ❌ Don't wire | Removed — idle uses priority ladder, not GOAP |
| Abilities | ❌ Wrong genre | No combat, no abilities, no mana |
| ItemDrops | ❌ Wrong genre | Idle has direct production, not loot drops |
| Crafting | ❌ Wrong genre | No crafting UI or recipe system |
| Economy (ECS) | ✅ Already wired | Already integrated |
| EcsPhysics2D | ❌ Wrong genre | No physics simulation |
| EntityTargetingSystem | ❌ Wrong genre | No combat system |
| Sensors | ❌ Wrong genre | No AI vision; TaskDistribution handles agent routing |
| NetworkSync | ❌ Wrong genre | Single-player game |
| RenderingOptimizations | ❌ Wrong game | Designed for Thistletide's rendering needs |
| WorldGen2D | ❌ Wrong genre | Uses MapLoader for handcrafted maps |
| GridAgents | ❌ Wrong game | Thistletide-specific agent scaffold |

### Thistletide — Genre: Action RPG / Dungeon Crawler

Action RPG mechanics: AI-controlled characters with abilities, combat, loot drops, faction-based targeting, Box2D physics, LiteNetLib multiplayer.

| Plugin | Verdict | Reason |
|--------|---------|--------|
| Framework | ✅ Belongs | ECS infrastructure, shared types |
| GridPathfinding | ✅ Belongs | Agent/dungeon navigation |
| GridPlacement | ✅ Belongs | Building/interaction placement |
| TaskDistribution | ✅ Belongs | Task board, agent assignment |
| ItemVault | ✅ Belongs | Inventory containers |
| AI | ✅ Belongs | GOAP planner for agent decision making |
| Abilities | ✅ Belongs | Castable abilities with mana, cooldowns |
| Attributes | ✅ Belongs | Health/Energy/Mana on characters |
| EcsPhysics2D | ✅ Belongs | Box2D physics integration |
| EntityTargetingSystem | ✅ Belongs | Faction-based combat targeting |
| GridAgents | ✅ Belongs | Reusable agent runtime scaffold |
| ItemDrops | ✅ Belongs | Loot from enemies, chests, containers |
| AudioSystem | ✅ Belongs | JSON-configured audio cues for actions/combat |
| NetworkSync | ✅ Belongs | LiteNetLib multiplayer |
| Sensors | ✅ Belongs | Visual detection for AI awareness |
| RenderingOptimizations | ✅ Belongs | ECS rendering optimizations |
| WorldState | ✅ Belongs | Global/faction KV state |
| WorldTime | ❌ Not wired | **High priority** — day/night, ability durations, buff timers |
| Quest | ❌ Not wired | **Consider** — YAML quest content without GOAP complexity |
| Telemetry | ❌ Not wired | **Consider** — perf monitoring for ECS at scale |
| PrototypeUI | ✅ Already wired | Godot layer debug HUD scaffolding |
| MapLoader | ❌ Wrong genre | Tiled maps — idle-specific; Thistletide uses custom terrain |
| StatusEffects | ❌ Don't wire | Buff/attribute system — Thistletide uses Abilities for effects |
| Economy | ❌ Wrong genre | Passive production — no idle mechanics |
| Upgrades | ❌ Wrong genre | Cost-scaling purchases — idle shop; Thistletide uses Abilities/Attributes |
| Crafting | ❌ Wrong genre | No crafting system designed |
| AudioSystem (idle) | ❌ Wrong game | Belongs to idle if wired at all |
| Minimap | ❌ Don't wire | No explored dungeon layout; no fog-of-war gameplay |
| WorldGen2D | ❌ Wrong genre | Procedural terrain — custom solution in use |
| GridPlacement.Godot.Views | ❌ Wrong game | Idle-specific placement preview UI |

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
| Attributes | ⚠️ unused | ✅ Core | **Shared** — wire in idle if needed |
| AI | ❌ Removed | ✅ Core | Thistletide only |
| Abilities | ❌ Wrong genre | ✅ Core | Thistletide only |
| AudioSystem | ❌ Wrong genre | ✅ Godot | Thistletide only |
| EcsPhysics2D | ❌ Wrong genre | ✅ Core | Thistletide only |
| EntityTargetingSystem | ❌ Wrong genre | ✅ Core | Thistletide only |
| GridAgents | ❌ Wrong game | ✅ Core | Thistletide only |
| ItemDrops | ❌ Wrong genre | ✅ Core | Thistletide only |
| NetworkSync | ❌ Wrong genre | ✅ Core | Thistletide only |
| Sensors | ❌ Wrong genre | ✅ Core | Thistletide only |
| RenderingOptimizations | ❌ Wrong game | ✅ Godot | Thistletide only |
| WorldTime | ✅ Core+ECS+Views | ❌ Not wired | Idle only |
| StatusEffects | ✅ Core | ❌ Wrong genre | Idle only |
| Economy | ✅ ECS | ❌ Wrong genre | Idle only |
| Upgrades | ✅ ECS | ❌ Wrong genre | Idle only |
| Quest | ✅ Core | ❌ Not wired | Idle only |
| MapLoader | ✅ Godot | ❌ Wrong genre | Idle only |
| Telemetry | ✅ Editor plugin | ❌ Not wired | Editor tool |
| Crafting | ❌ Wrong genre | ❌ Wrong genre | Neither |
| Minimap | ❌ Not needed | ❌ Not needed | Neither |
| PrototypeUI | ❌ Not needed | ✅ Godot | Thistletide only |
| WorldGen2D | ❌ Wrong genre | ❌ Wrong genre | Neither |
| GridAgents | ❌ Wrong game | ✅ Core | Thistletide only |

---

## Priority Integration Candidates

### For MoonBark Idle

1. **Attributes** — `HealthComponent`, `EnergyComponent` in csproj but unused. Only wire if agents need vitals. Low effort.
2. **WorldState** — Verify `ProjectReference` is actually active in game code. Document in plugin-connections.md if confirmed.
3. **AudioSystem** — Nice-to-have. No SFX design in scope yet.

### For Thistletide

1. **WorldTime** (95) — **High priority.** Day/night cycle, ability duration tracking, buff timers. `WorldTime.Core + ECS` layers fit Thistletide's plugin structure.
2. **Quest** (60) — YAML quest content could complement Thistletide's content without GOAP complexity.
3. **Telemetry** (70) — Perf monitoring for ECS systems at scale.

---

## Plugin Genre Classification

| Classification | Plugins |
|---------------|---------|
| **Idle/Tycoon only** | Economy, StatusEffects, Upgrades, Quest, MapLoader, Telemetry (editor) |
| **Action RPG only** | Abilities, Attributes, EcsPhysics2D, EntityTargetingSystem, ItemDrops, NetworkSync, Sensors, RenderingOptimizations, AI |
| **Shared (both genres)** | Framework, GridPathfinding, GridPlacement, TaskDistribution, ItemVault, WorldState, GridAgents |
| **Neither game (wrong genre)** | Crafting, WorldGen2D, Minimap |
| **Idle could use, not wired** | GridAgents, AudioSystem |
| **Thistletide-only** | PrototypeUI |

---

*Last Updated: 2026-04-29*
