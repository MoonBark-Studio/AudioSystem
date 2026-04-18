# AudioSystem Architecture

## Signal Bus Pattern

**Rule:** Each domain plugin owns its own signal bus. Consumers subscribe to whatever buses they need — like a log viewer subscribing to all of them.

No `GetNode()` calls. No hardcoded paths. No GDScript routing layer. Pure C# signal buses that any Godot node can subscribe to.

---

## Pattern: Dual Event/Signal Bus

Every plugin follows this two-layer pattern:

```
[Plugin].Core/Events/          → pure C# event types + event bus
[Plugin].Godot/Events/          → Godot RefCounted signal bus (bridges core → Godot signals)
```

**Why both:**
- Core events: engine-agnostic, unit-testable, no Godot dependency
- Signal bus: inherits from `RefCounted`, can emit Godot `[Signal]` attributes, callable from GDScript
- Bridge: signal bus subscribes to core events and re-emits them as Godot signals

**Reference implementation:** `MoonBark.GridPlacement.Godot.Events.PlacementSignalBus` — the canonical example.

---

## Existing Signal Buses (Audited 2026-03-31)

| Plugin | Path | Status | Events |
|--------|------|--------|--------|
| **WorldTime** | `WorldTime.Godot.Events.WorldTimeSignalBus` | ⚠️ Stub — signals defined but `Publish()` methods are no-ops. Core `WorldTimeEventBus` exists but is not bridged. | `TimeAdvancedSignal`, `DateChangedSignal`, `TimeOfDayChangedSignal`, `WorldAgeChangedSignal` |
| **MoonBark.GridPlacement** | `MoonBark.GridPlacement.Godot.Events.PlacementSignalBus` | ✅ Full implementation | `PlacementSuccessSignal`, `PlacementFailedSignal`, `PlacementValidatedSignal`, `PlaceableSelectedSignal`, `PlacementCancelledSignal` |
| **Wiring** | `Wiring.Core.Events.FrameworkEventBus` | ⚠️ Static generic bus — not a Godot signal bus. No Godot-level bridge. | Generic `Action<T>` — typed events via `QuestEvents`, `ItemPickupEvent`, etc. |
| **EntityStats** | `EntityStats.Core.Events.DamageEvents` | Pure C# events only — no Godot signal bus | `DamageEvent`, `DeathEvent` |
| **Inventory** | `Inventory.Core.Events.InventoryEvents` | Pure C# events only — no Godot signal bus | `ItemAddedEvent`, `ItemRemovedEvent`, `ItemMovedEvent`, etc. |
| **ItemDrops** | `ItemDrops.Godot.Resources.ItemDropsBus` | Godot RefCounted bus | `ItemDroppedSignal`, `ItemPickedUpSignal` |

---

## AudioSystem Subscriptions

`GodotAudioManager` subscribes to **existing domain signal buses**. It does NOT own a domain signal bus — audio is a consumer, not an event source.

### Required subscriptions

| Source bus | Signal | Handler | Audio action |
|-----------|--------|---------|--------------|
| `WorldTimeSignalBus` | `TimeOfDayChangedSignal(hour, min, sec)` | `_OnTimeOfDayChanged` | Play dawn/dusk/midnight music at hours 6/18/0 |
| `PlacementSignalBus` | `PlacementSuccessSignal(hash, pos)` | `_OnPlacementSuccess` | Play `place_{placeableHash}` one-shot |
| `InventorySignalBus` | (none exists — no Godot bridge) | — | Cannot subscribe yet |
| `ItemDropsBus` | `ItemPickedUpSignal` | `_OnItemPickedUp` | Play `pickup_{itemId}` one-shot |
| *(game-level)* | `QuestCompleted` | `_OnQuestCompleted` | Play `quest_complete` one-shot |

### Subscription pattern (in `_Ready()`)

```csharp
// Get signal bus via autoload — no GetNode() with relative paths
var timeBus = GetNode<WorldTimeSignalBus>("/root/WorldTimeSignalBus");
if (timeBus != null) {
    timeBus.Connect(WorldTimeSignalBus.SignalName.TimeOfDayChangedSignal, 
        Callable.From(_OnTimeOfDayChanged));
}

// Placement success → one-shot
var placementBus = GetNode<PlacementSignalBus>("/root/PlacementSignalBus");
if (placementBus != null) {
    placementBus.Connect(PlacementSignalBus.SignalName.PlacementSuccessSignal,
        Callable.From(_OnPlacementSuccess));
}
```

---

## Missing / Incomplete

### 1. WorldTime signal bus is a stub
`WorldTimeSignalBus.Publish()` methods are empty no-ops. Core `WorldTimeEventBus` fires events but they never reach the Godot signals. **Audio cannot subscribe to `TimeOfDayChangedSignal` until this is fixed.**

### 2. Inventory has no Godot signal bus
`InventoryEvents` (pure C#) exist but there's no `InventorySignalBus` bridging them to Godot signals. Subscribing from audio is not possible.

### 3. BuildEvents / ResourceEvents don't exist
These were in the plan for Wiring level but have not been created. Placement signals cover building. Resource events need a home.

---

## Architecture Rules

1. **Each plugin owns its signal bus.** Bus lives at `[Plugin].Godot/Events/`.
2. **Signal buses are Godot RefCounted**, not static. Accessed via autoload (`GetNode<T>("/root/BusName")`).
3. **Consumers (like GodotAudioManager) subscribe to existing buses**, not own domain events.
4. **No node path coupling.** No `GetNode("../QuestSystem")` or similar.
5. **No GDScript routing.** All routing in C#.
6. **Unused signals float.** A bus can emit events nothing subscribes to. That's fine — keeps the bus reusable.

---

## Pending

- [ ] Fix `WorldTimeSignalBus` to bridge `WorldTimeEventBus` → Godot signals (stub is broken)
- [ ] Create `InventorySignalBus` bridging `InventoryEvents` → Godot signals
- [ ] Implement `GodotAudioManager` subscriptions to existing buses (blocked by above)
- [ ] Audit `ItemDropsBus` for `ItemPickedUp` signal signature
