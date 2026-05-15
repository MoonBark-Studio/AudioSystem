# Plan: Fix Missing AgentStallMonitor — Agent Pathfinding Stall Debug

## Problem

Runtime: `AgentStallMonitorSystem` repeatedly failing to route agents to target `(14,12)`.
Build error: `AgentStallMonitor` does not exist in the current context (line 490, `IdleSimulationHarness.cs`).

## Root Cause

`IdleSimulationHarness` references `AgentStallMonitor` as a system to add to the runner, but **no such type exists anywhere in the codebase**. The `GridAgents` plugin provides the `StallMonitor` class (`GridAgents/Core/Monitoring/StallMonitor.cs`) which is a pure tracker — it needs an ECS system wrapper in moonbark-idle to:

1. Query all agent entities each tick
2. Build `ObservedAgentMotion` from agent state (position, move target, current waypoint, active intent)
3. Call `StallMonitor.Observe()` on each agent
4. Execute the returned `RecoveryPlan` via `RecoveryController`

## Files Likely to Change

| File | Change |
|------|--------|
| `games/moonbark-idle/cs/Core/Simulation/IdleSimulationHarness.cs` | Add `AgentStallMonitor` property of the new system type; wire it into `_systemRunner` at line 490 |
| `games/moonbark-idle/cs/Core/Systems/` (new) | Create `AgentStallMonitorSystem.cs` — implements the ECS system that bridges agents to `StallMonitor` |
| `games/moonbark-idle/cs/Core/MoonBark.IdleGame.Core.csproj` | Add `<Compile Include="Core/Systems/AgentStallMonitorSystem.cs" />` |
| `games/moonbark-idle/cs/MoonBark.IdleGame.Tests/AgentStallMonitorIntegrationTests.cs` | These tests exist (in `TestResults/`) — find and verify they pass after the fix |

## Implementation Steps

### Step 1 — Create `AgentStallMonitorSystem`

Location: `games/moonbark-idle/cs/Core/Systems/AgentStallMonitorSystem.cs`

Responsibilities:
- Implement `ISimulationSystem` or extend the existing `DelegateSimulationSystem` pattern
- Each tick, query all entities with `AgentComponent` + `MovementStateComponent` (or equivalent)
- For each agent, construct an `ObservedAgentMotion` struct:
  - `AgentId` / `EntityId` from entity
  - `State` from `MovementStateComponent.CurrentState`
  - `WorldPosition` / `GridPosition` from `TransformComponent` (or world-space position)
  - `MoveTarget` from `NavAgentComponent.TargetPosition` (or equivalent)
  - `CurrentWaypoint` from pathfinding waypoint buffer
  - `ActiveIntent` from `IntentHandle` on the agent
  - `ConsecutiveTicks` from movement state
  - `AtMoveTarget` from whether agent is already at target
  - `TargetEntityId` if applicable
- Call `_stallMonitor.Observe(observation)` for each agent
- If result is non-null, call `_recoveryController.Execute(agentEntityId, result.Value.Plan)`

Constructor deps:
- `EntityStore` (to query agents)
- `StallMonitor` instance (from `GridAgents.Core`)
- `RecoveryController` instance (from `GridAgents.Core`)
- `AgentRuntimeOptions` (stall thresholds)

### Step 2 — Wire into `IdleSimulationHarness`

```csharp
// Add property (near line 133 with other systems)
public AgentStallMonitorSystem AgentStallMonitor { get; }

// In constructor — after Movement system is created (line ~431):
var stallMonitorOptions = new AgentRuntimeOptions { StallTickThreshold = 3, UnrecoverableRecoveryAttemptThreshold = 2 };
AgentStallMonitor = new AgentStallMonitorSystem(_store, stallMonitorOptions, /* intent lifecycle */, /* movement recovery */);

// In _systemRunner.Add order — already at line 490:
// _systemRunner.Add(AgentStallMonitor);  ← this line already exists, now it will compile
```

The exact shape of `AgentStallMonitorSystem` depends on how intents and movement recovery are wired in idle — may need `IIntentLifecycle` + `IMovementRecovery` adapters implemented as inner classes or injected interfaces already present in the harness.

### Step 3 — Add to csproj

In `MoonBark.IdleGame.Core.csproj`, add explicit compile include if using explicit pattern:
```xml
<Compile Include="Core/Systems/AgentStallMonitorSystem.cs" />
```

### Step 4 — Verify Tests Pass

Find `AgentStallMonitorIntegrationTests.cs`:
```bash
find . -name "AgentStallMonitorIntegrationTests.cs"
```

Run:
```bash
dotnet test MoonBark.IdleGame.Tests/MoonBark.IdleGame.Tests.csproj --filter "AgentStallMonitor"
```

Expected: all 5 tests pass (they were passing in latest.trx at `current.trx`).

## Open Questions

1. **What interfaces does idle use for intent lifecycle?** Need to find `IIntentLifecycle` implementation in moonbark-idle to pass to `RecoveryController`.
2. **What component holds the agent's `IntentHandle`?** Need to find the intent component used by `TaskDistribution`.
3. **What component holds movement state / waypoints?** Need `MovementStateComponent` or equivalent.
4. **Does `AgentRuntimeOptions` need config from YAML or can it be hardcoded for now?** Hardcode reasonable defaults (StallTickThreshold=3, UnrecoverableRecoveryAttemptThreshold=2).
5. **Is there an existing `RecoveryController` in idle**, or does one need to be created? The `GridAgents` plugin has `RecoveryController` — it may need an idle-specific adapter.

## Risks

- `AgentStallMonitorSystem` may need to query many component types — ensure the query is architeture-efficient (use `Query<T1, T2, T3>()` with only needed components)
- `StallMonitor.Observe` is called per-agent per-tick — with many agents this could be slow; but `StallMonitor` is O(1) dict lookup so should be fine
- The `RecoveryController` needs `IIntentLifecycle` and `IMovementRecovery` implementations — these may not exist in idle yet and would need to be stubbed or implemented
