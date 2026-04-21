# Telemetry — Health

**Module:** `internal.MoonBark.Telemetry`

## Status

**Health Score: 7 / 10**
**Workspace Health Score: 70/100**
✅ Architecture correct
✅ Boundaries clean
✅ Documentation aligned
✅ Roadmap finalized
⏳ Implementation in progress

Current Phase: 0 Scaffold complete. Ready for Phase 1 ECS interface work.

### Rationale
Friflo.ECS intentionally provides NO native Godot performance panel, monitor, or dashboard. This is an explicit upstream design decision, not an oversight. This is the reason this module exists: we are required to implement full ECS performance tracking, profiling, and editor integration at the Framework layer, with no upstream dependency.

## Quality Gates

- [ ] `IPerformanceData` interface compiles with zero Friflo dependencies
- [ ] `IdleSimulationHarness` implements `IPerformanceData` without breaking existing tests
- [ ] `SystemRunner` tracked metrics are readable through the interface
- [ ] TelemetryPanel renders without affecting simulation performance
- [ ] F3 toggle works in moonbark-idle dev builds

## Discovered Issues

None yet.

## Architecture Rules

- `Core/` has zero Friflo.ECS NuGet references — pure C# only
- `Godot/` layer wires TO the Core interfaces, never FROM them
- Panel is read-only — no mutations to `IPerformanceData` or any simulation state
- No ECS types in public interface contracts

## Dependencies

- `MoonBark.Framework` (Core layer)
- `MoonBark.Idle.Core` (harness impl, for wiring only)
