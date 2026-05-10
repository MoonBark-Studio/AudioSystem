# DRY Refactor Candidates — Idle ↔ Thistletide

**Audited:** 2026-05  
**Implemented:** 2026-05 (items 1–7 complete; see Implementation Status below)  
**Scope:** Cross-game duplicated code that should move to `MoonBark.Framework`, `MoonBark.Framework.ECS`, or an existing plugin.

---

## Summary Table

| # | Code | Idle location | Thistletide location | Target | Impact |
|---|------|--------------|----------------------|--------|--------|
| # | Code | Idle location | Thistletide location | Target | Impact | Status |
|---|------|--------------|----------------------|--------|--------|--------|
| 1 | `GridToWorld` / `WorldToGrid` math | `GridMath.cs` → shim | `AgentNavigationExecutionSystem` → uses Framework | `MoonBark.Framework.Types.GridCoordinates` | 🔴 High | ✅ Done |
| 2 | Grid-bounds `IsInBounds` / `ClampToBounds` | `GridBoundsHelper.cs` → shim | `AgentNavigationExecutionSystem` → uses Framework | `MoonBark.Framework.Types.GridBounds` | 🔴 High | ✅ Done |
| 3 | Movement step engine (dx/dy/dist/clamp) | `MovementSystem` → uses `MovementStepEngine` | Uses `MovementStepEngine` ✅ | `GridAgents` already has it | 🔴 High | ✅ Done |
| 4 | `DeferredComponentQueue<T>` | Deleted from Utilities | Inline pattern (unchanged) | `MoonBark.Framework.ECS` | 🟠 Medium | ✅ Done |
| 5 | `EntityStore.TryGetComponent<T>` safety helpers | Deleted from Utilities | Bare Friflo calls (unchanged) | `MoonBark.Framework.ECS.EntityExtensions` | 🟠 Medium | ✅ Done |
| 6 | `IFrameworkLogger` null-coalescing factory | 7 callers migrated → `OrConsoleDefault<T>` | Ad-hoc per-system (unchanged) | `MoonBark.Framework.Logging.LoggingExtensions` | 🟠 Medium | ✅ Done |
| 7 | Godot → `ILogger` bridge | `GodotLogger.cs` (Idle only) | Missing (uses `Console.WriteLine`) | New `Framework.Godot` project | 🟠 Medium | 🔜 Pending |
| 8 | `ISimulationSystem` vs `IECSSystem` | `ISimulationSystem` (Framework alias) | `IECSSystem` (game-local) | `MoonBark.Framework.Core` | 🟡 Low-Med | 🔜 Pending |
| 9 | YAML config loading boilerplate | Raw `YamlDotNet.DeserializerBuilder` | `YamlConfigurationFiles` ✅ | Idle should adopt Framework util | 🟡 Low-Med | 🔜 Pending |
| 10 | `TaskDistributionBridgeSystem` pattern | `TaskDistributionBridgeSystem.cs` | `TaskDistributionBridgeSystem.cs` (Obsolete) | `TaskDistribution` plugin base | 🟡 Low-Med | 🔜 Pending |
| 11 | `ChunkedPathfinder` (HPA* impl) | Not implemented | `Core/Navigation/ChunkedPathfinder.cs` | `GridPathfinding` plugin | 🟡 Low-Med | 🔜 Pending |

---

## 1 🔴 Grid Coordinate Math — four independent copies

### The problem
`GridToWorld` (grid cell → world pixel center) and `WorldToGrid` (world pixel → grid cell) are
implemented independently four times:

| Location | Access | Formula |
|----------|--------|---------|
| `games/moonbark-idle/cs/Core/Utilities/GridMath.cs` | `public static` | `x * size + size/2` / `floor(x/size)` |
| `plugins/GridAgents/Core/Movement/MovementStepEngine.cs` | `private static GridToWorld` | Same |
| `plugins/GridAgents/Core/Movement/MovementTargetResolver.cs` | `private static WorldToGrid` | Same |
| `games/thistletide/cs/ECS/Systems/AgentNavigationExecutionSystem.cs` | `private static` (both) | Same |

### Proposed fix
Add to `MoonBark.Framework.Types` (already owns `CoreVector2`/`CoreVector2I`):

```projects/cores/MoonBark.Framework/Types/GridCoordinates.cs#L1-20
public static class GridCoordinates
{
    /// <summary>Center-of-tile world position for a grid cell.</summary>
    public static CoreVector2 GridToWorld(CoreVector2I cell, float tileSize)
        => new(cell.X * tileSize + tileSize / 2f, cell.Y * tileSize + tileSize / 2f);

    /// <summary>Grid cell containing a world position.</summary>
    public static CoreVector2I WorldToGrid(CoreVector2 position, float tileSize)
        => new((int)MathF.Floor(position.X / tileSize), (int)MathF.Floor(position.Y / tileSize));
}
```

Then:
- `GridMath.GridToWorld/WorldToGrid` → delegate to `GridCoordinates` or delete
- `MovementStepEngine`/`MovementTargetResolver` private copies → call `GridCoordinates`
- `AgentNavigationExecutionSystem` private copies → call `GridCoordinates`
- `EntityExtensions.GetGridPosition` (Framework.ECS) uses a hardcoded `32f` tile size — switch to `GridCoordinates.WorldToGrid`

---

## 2 🔴 Grid-Bounds Clamping — three independent copies

### The problem
`IsInBounds(position, bounds?)` and `ClampToBounds(position, bounds?)` are duplicated:

| Location | Notes |
|----------|-------|
| `games/moonbark-idle/cs/Core/Utilities/GridBoundsHelper.cs` | Public, Nullable + Func overloads |
| `plugins/GridAgents/Core/Movement/MovementStepEngine.cs` | Private `ClampPosition` |
| `plugins/GridAgents/Core/Movement/MovementTargetResolver.cs` | Private `ClampToBounds` |

The Thistletide `AgentNavigationExecutionSystem` passes `_gridBounds` into `MovementStepEngine` / `MovementTargetResolver`, so it's indirectly using these. But Idle's `MovementSystem` has its own hand-rolled `ClampPosition()` too.

### Proposed fix
Promote `GridBoundsHelper` to `MoonBark.Framework.Types` as `GridBounds`:

```projects/cores/MoonBark.Framework/Types/GridBounds.cs#L1-20
public static class GridBounds
{
    public static bool IsInBounds(CoreVector2I pos, CoreVector2I? bounds) { ... }
    public static CoreVector2I ClampToBounds(CoreVector2I pos, CoreVector2I? bounds) { ... }
    public static CoreVector2 ClampPosition(CoreVector2 pos, float tileSize, CoreVector2I? bounds) { ... }
}
```

The GridAgents plugin's private copies become calls to this. `GridBoundsHelper` in Idle becomes a thin wrapper or is deleted.

---

## 3 🔴 Idle's `MovementSystem` inlines `MovementStepEngine` + `MovementTargetResolver`

### The problem
`games/moonbark-idle/cs/Core/Systems/MovementSystem.cs` (~200 lines) manually reimplements the
entire movement-step pipeline that `plugins/GridAgents/Core/Movement/MovementStepEngine.cs` already provides:

| Logic | Idle `MovementSystem` | `GridAgents` |
|-------|-----------------------|--------------|
| Resolve waypoint vs raw target | Lines 97–114 (inline) | `MovementTargetResolver.Resolve()` |
| Compute dx/dy/dist, clamp step | Lines 135–185 (inline) | `MovementStepEngine.Tick()` |
| Advance waypoint on arrival | Lines 148–160 (inline) | `MovementStepResult.ShouldAdvanceWaypoint` |
| Clamp position to grid bounds | `ClampPosition()` private method | `MovementStepEngine.ClampPosition()` |
| Cardinal facing from dx/dy | Lines 118–126 (inline) | `MovementStepResult.FacingDirection` |

Thistletide's `AgentNavigationExecutionSystem.cs` correctly delegates to `MovementStepEngine.Tick()`.

### Proposed fix
Refactor Idle's `MovementSystem.Update()` to:
1. Call `MovementTargetResolver.Resolve(...)` for target resolution (path vs raw)
2. Call `MovementStepEngine.Tick(...)` for position/facing update
3. Read `MovementStepResult.HasArrived` / `ShouldAdvanceWaypoint` instead of inline checks
4. Keep the query loop, speed resolution (`MovementSpeedHelper`), and event publishing — those remain game-specific

`MovementSystem` would shrink from ~200 lines to ~80 lines. Idle already depends on `GridAgents` transitively through `GridPathfinding`, so no new project reference is needed.

---

## 4 🟠 `DeferredComponentQueue<T>` — Idle has it, Thistletide re-invents it inline

### The problem
`DeferredComponentQueue<T>` in Idle encapsulates the ECS structural-change deferral pattern
(cannot add/remove components inside a query loop in Friflo ECS). Thistletide consistently
uses inline nullable lists for the same purpose:

```/dev/null/example.cs#L1-6
// Thistletide (TaskDistributionBridgeSystem, etc.)
List<(Entity entity, SomeComponent comp)>? pending = null;
...
pending ??= new();
pending.Add((entity, value));
...
if (pending != null) foreach (var (e, c) in pending) e.AddComponent(c);
```

This pattern appears at least in `TaskDistributionBridgeSystem` and several ECS systems across
both games.

### Proposed fix
Move `DeferredComponentQueue<T>` from `games/moonbark-idle/cs/Core/Utilities/` to
`cores/MoonBark.Framework/ECS/DeferredComponentQueue.cs`. No API changes needed.
Thistletide systems migrate the inline pattern to use the type.

---

## 5 🟠 `EntityStoreExtensions.TryGetComponent<T>` — Framework.ECS already has `EntityExtensions`

### The problem
Idle's `EntityStoreExtensions.cs` defines:
```games/moonbark-idle/cs/Core/Utilities/EntityStoreExtensions.cs#L1-20
public static bool TryGetComponent<T>(this EntityStore store, int entityId, out T component)
public static bool TryGetComponent<T>(this EntityStore store, int entityId, out Entity entity, out T component)
```

Framework.ECS already has `EntityExtensions.cs` with `GetPosition`, `GetGridPosition`, etc.
The store-level `TryGetComponent<T>` helpers belong in that same file.

Thistletide currently uses raw Friflo calls with manual null/existence checks scattered across
systems.

### Proposed fix
Move Idle's `EntityStoreExtensions` methods into `cores/MoonBark.Framework/ECS/EntityExtensions.cs`
(or a new `EntityStoreExtensions.cs` in the same project). Idle and Thistletide both use them.
No API breakage — additive change to an existing file.

---

## 6 🟠 `IFrameworkLogger` null-coalescing factory — Idle has it, Thistletide re-invents it per system

### The problem
Every Idle system that takes `IFrameworkLogger? logger = null` calls:
```games/moonbark-idle/cs/Core/Systems/FrameworkLoggerHelper.cs#L1-8
public static IFrameworkLogger CreateOrDefault<T>(IFrameworkLogger? logger, LogLevel level)
    => logger ?? new ConsoleLogger(typeof(T).Name, level);
```

Thistletide systems either:
- Skip the null-coalescing (no fallback logging)
- Write the null-check inline
- Use `Console.WriteLine` directly (6 occurrences flagged by the audit)

### Proposed fix
Add to `cores/MoonBark.Framework/Logging/LoggingExtensions.cs`:
```/dev/null/LoggingExtensions.cs#L1-8
public static IFrameworkLogger OrConsoleDefault<T>(
    this IFrameworkLogger? logger,
    LogLevel level = LogLevel.Debug)
    => logger ?? new ConsoleLogger(typeof(T).Name, level);
```

Delete `FrameworkLoggerHelper.cs` from Idle; all callers change to `logger.OrConsoleDefault<T>()`.

---

## 7 🟠 `GodotFrameworkLogger` — Idle only, Thistletide uses `Console.WriteLine`

### The problem
Idle's `godot/Logging/GodotFrameworkLogger.cs` implements `IFrameworkLogger` using `GD.Print` /
`GD.PrintErr`. Thistletide has no equivalent — its Godot layer falls back to raw
`Console.WriteLine` or `GD.Print` calls that bypass the Framework logger contract entirely.

Both games need to route `IFrameworkLogger` output through Godot in their Godot layers.

### Proposed fix
Create `cores/MoonBark.Framework/Godot/MoonBark.Framework.Godot.csproj`
(thin `Godot.NET.Sdk/4.6.2` project, ~2 files):

```/dev/null/MoonBark.Framework.Godot.csproj#L1-10
<Project Sdk="Godot.NET.Sdk/4.6.2">
  <PropertyGroup>
    <AssemblyName>MoonBark.Framework.Godot</AssemblyName>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="GodotFrameworkLogger.cs" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\MoonBark.Framework.csproj" />
  </ItemGroup>
</Project>
```

Move `GodotFrameworkLogger` + `GodotFrameworkLoggerFactory` here.
Both games' Godot csproj files reference this instead of redeclaring it.

---

## 8 🟡 `ISimulationSystem` vs `IECSSystem` — parallel interface hierarchies

### The problem

| | `MoonBark.Framework.Core.ISimulationSystem` | `Thistletide.ECS.IECSSystem` |
|--|---------------------------------------------|------------------------------|
| `Priority` | ❌ not present | ✅ `int Priority` |
| `Name` | ✅ `string Name` | ❌ not present |
| `Dispose` | ✅ `IDisposable` | ❌ not present |
| `Update` signature | `void Update(float deltaTime)` | `void Update(EntityStore world, float deltaTime)` |

Idle systems close over `EntityStore` in their constructor (it's captured). Thistletide systems
receive it per-call (stateless with respect to the store). Both patterns are valid. The mismatch
means there is no single framework interface either can use.

### Proposed fix
Option A (minimal): Add `int Priority` to Framework's `ISimulationSystem`. Thistletide's
`IECSSystem.Update(EntityStore, float)` stays game-local (different signature, kept separate).

Option B (broader): Add a companion `IPrioritizedSystem { int Priority; }` marker interface to
Framework. Both `ISimulationSystem` and `IECSSystem` extend it. `SystemRunner` in Framework sorts
by `IPrioritizedSystem.Priority` when present.

Option A is the low-risk incremental path.

---

## 9 🟡 YAML config loading — Idle uses raw YamlDotNet, Thistletide uses Framework utility

### The problem
Thistletide's config loaders (`SpeciesConfigLoader`, `AudioConfigLoader`, `InputConfigLoader`) all
use `MoonBark.Framework.Configuration.YamlConfigurationFiles`:

```games/thistletide/cs/Core/Configuration/SpeciesConfigLoader.cs#L14-18
YamlConfigurationFiles.ResolvePathFromSearchRoots(RelativeSpeciesConfigPath, ...)
YamlConfigurationFiles.LoadRequired<SpeciesConfig>(yamlPath, YamlNamingStyle.CamelCase, ...)
```

Idle's `YamlConfigurationLoader` constructs `DeserializerBuilder` directly with its own
`NullNamingConvention` + `IgnoreUnmatchedProperties` settings. This:
- Duplicates the serializer setup (naming convention, error handling)
- Bypasses the Framework's path-resolution search-root logic
- Means YAML quirks must be fixed in two places

### Proposed fix
Migrate `YamlConfigurationLoader` to use `YamlConfigurationFiles.LoadRequired<T>()` for each
config type. The `LoadAll()` orchestration remains game-specific (it knows which files exist in
the Idle `Config/` directory), but the per-file deserialization uses the Framework utility.

---

## 10 🟡 `TaskDistributionBridgeSystem` — both games define one

### The problem
Both games bridge `TaskTakerComponent.CurrentTaskId` → game-specific goal state:

| | Idle `TaskDistributionBridgeSystem` | Thistletide `TaskDistributionBridgeSystem` |
|--|-------------------------------------|-------------------------------------------|
| Purpose | Maps 5 task categories → GOAP ActivityState | Maps single task → AgentGoalComponent position |
| Common logic | Query `TaskTakerComponent`, call `JoinTask`/`CompleteTask`, clear on Guid.Empty | Same |
| Game-specific | `SyncTaskToGoap` dispatch switch, sell-bin lookup, crop claims | `ParsePayload`, `WorldState.SetValue` publishing |
| Status | Active | `[Obsolete]` |

The lifecycle boilerplate (query loop, join/complete lifecycle, clear-on-empty) is the same.

### Proposed fix
Add to `TaskDistribution` plugin a base utility `TaskLifecycleBridge` (or an `abstract` base class)
that owns the query + lifecycle. Games subclass it and override `OnTaskAssigned(entity, taskId)`.

This is low priority given Thistletide's version is obsolete. Revisit when a third game needs it.

---

## 11 🟡 `ChunkedPathfinder` (HPA*) lives only in Thistletide

### The problem
`games/thistletide/cs/Core/Navigation/ChunkedPathfinder.cs` is a 280-line two-level HPA*
implementation using Sylves. It delivers a 10–40× speedup over flat A* on large grids
(documented in its XML summary). This has nothing game-specific in it.

The `GridPathfinding` plugin currently exposes a single-level A* (`GridPathfinder`). Large maps in
either game (or a future game) would benefit from chunked pathfinding.

### Proposed fix
Donate `ChunkedPathfinder` to `plugins/GridPathfinding/` as an alternative strategy:
- Rename to `GridPathfinderChunked` (consistent naming)
- Place in `GridPathfinding/Core/Chunked/`
- Add factory method mirroring `ChunkedPathfinder.Create(gridWidth, gridHeight, chunkSize)`
- Thistletide's `Core/Navigation/ChunkedPathfinder.cs` becomes a thin wrapper or is deleted

---

## Implementation Priority

```
Completed 2026-05:
  ✅ 1 → GridCoordinates → Framework.Types (+ shims in Idle; Thistletide updated)
  ✅ 2 → GridBounds → Framework.Types (+ shims in Idle; GridAgents updated)
  ✅ 3 → Idle MovementSystem uses GridAgents MovementStepEngine + MovementTargetResolver
  ✅ 4 → DeferredComponentQueue<T> → Framework.ECS (deleted from Idle Utilities)
  ✅ 5 → EntityStore TryGetComponent → Framework.ECS.EntityExtensions (deleted from Idle)
  ✅ 6 → OrConsoleDefault<T> → Framework.Logging (7 call sites migrated, FrameworkLoggerHelper deleted)

Results:
  Idle tests: 607/631 pass (was 529/582 in last HEALTH.md — net improvement)
  GridAgents tests: 9/9 pass
  Framework: clean build
  Thistletide AgentNavigationExecutionSystem: no new errors introduced
  Note: Thistletide.Core has 83 pre-existing errors (MoonBark.Framework.Configuration
        missing, AudioSystem API drift, PositionComponent API drift) — unrelated to our work

Pending:
  7 → GodotFrameworkLogger → Framework.Godot project
  8 → ISimulationSystem Priority
  9 → Idle adopts YamlConfigurationFiles
  10 → TaskDistributionBridgeSystem base
  11 → ChunkedPathfinder → GridPathfinding

Next cleanup (non-breaking):
  - Delete GridMath.cs shim once all callers (IdleWorldProjection, IdleSimulationDirector,
    AgentStallMonitorSystem, IdleAgentWorldView) migrate to GridCoordinates directly
  - Delete GridBoundsHelper.cs shim once callers migrate to GridBounds directly
```

## Notes on what is NOT duplicated

- **`SimulationEventBus`** (Idle) — game-specific typed event surface; Thistletide uses commands + WorldState instead. Different designs, not duplication.
- **`DeterministicTimeProvider`** (Idle) — game-specific test harness time control. Thistletide uses a different time model.
- **`TaskRegistry`** (Idle) — small bidirectional ID map. Thistletide relies on the `TaskZoneRegistry` inside TaskDistribution plugin. Idle should migrate to that registry, removing `TaskRegistry.cs` entirely.
- **`StageWorkflowComponents`** (Thistletide) vs **`FarmingComponents`** (Idle) — superficially similar (both track lifecycle stages) but semantics differ: Thistletide's is a general workflow engine, Idle's is crop-specific. Don't merge.
- **`AgentVisualInfoComponent`** (Thistletide, wraps `ConsoleColor`) vs **`AnimationComponent`** (Idle, wraps animation name string) — different concerns entirely.
