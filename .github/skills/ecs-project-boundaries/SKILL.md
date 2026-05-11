---
name: ecs-project-boundaries
description: "Validate and enforce plugin ECS project boundaries and ECS bridge contracts. Use when auditing plugins for ECS-dependent code outside ECS folders or .ECS.csproj projects, checking namespace and csproj dependency drift, scanning for Component-suffixed types outside ECS, tightening plugin-level tests around ECS bridge APIs, or running the workspace ECS boundary validator/fixer script."
user-invocable: true
---

# ECS Project Boundaries

Use this skill when a plugin needs to follow the workspace ECS split rule:

- ECS-dependent runtime code belongs under an `ECS/` folder.
- ECS-dependent runtime code should compile from a dedicated `.ECS.csproj`.
- ECS namespaces should match the ECS project root namespace.
- `*Component` runtime types belong in ECS code, not Core, Godot, or root runtime projects.
- Shared ECS bridge APIs should be immediately usable after construction or attachment; do not hide required follow-up setup in downstream call sites.
- API parameters should reflect one concept each. Do not overload a bridge parameter to mean both inventory/container count and per-container capacity or other unrelated semantics.

For MoonBark plugin bus wiring, provider patterns, or `GodotSignalBus` migration work, use the companion `moonbark-plugin-architecture` skill. This skill is only for ECS folder, namespace, and project-boundary enforcement.

## Bridge Contract Lessons

- When fixing a shared ECS/plugin bug, add regression coverage in the owning plugin test project before relying on downstream game tests.
- Plugin-level tests should assert setup results for bridge calls such as `TryAddItem`, `AddContainer`, or `TryCreateInventory`; do not ignore returned success/failure values during fixture seeding.
- Cover the default contract and the explicit-expansion contract separately. Example: prove that a created inventory is usable immediately, and prove that additional containers only work when the caller explicitly requests capacity for them.
- If a downstream game test fails after a bridge-contract change, first determine whether the runtime is broken or whether the consumer test was depending on stale setup assumptions.

## Workspace Script

Primary tool:

```powershell
pwsh ./scripts/Validate-EcsProjectBoundaries.ps1
```

Common variants:

```powershell
pwsh ./scripts/Validate-EcsProjectBoundaries.ps1 -Plugin AI,Abilities,EntityTargetingSystem
pwsh ./scripts/Validate-EcsProjectBoundaries.ps1 -Fix
pwsh ./scripts/Validate-EcsProjectBoundaries.ps1 -Json
```

## What It Checks

- ECS-dependent `.cs` files outside `ECS/` folders.
- `ECS/` files compiled by non-ECS projects.
- ECS namespace drift relative to the owning `.ECS.csproj` root namespace.
- Non-ECS runtime projects that still reference `Friflo.Engine.ECS` or `MoonBark.Framework.ECS`.
- Non-ECS projects that forgot to exclude `ECS/**/*.cs` from compilation.
- `*Component` runtime type declarations outside ECS code.
- Plugins that contain an `ECS/` folder but no dedicated `.ECS.csproj`.

## What `-Fix` Does

Safe automatic fixes only:

- rewrite ECS file namespaces to the expected namespace derived from the owning `.ECS.csproj`
- add missing `Compile Remove="...ECS..."` entries to non-ECS runtime projects when an ECS folder exists beneath them

Structural fixes are still reported, not auto-applied:

- creating a missing `.ECS.csproj`
- moving files between Core/Godot/root and `ECS/`
- removing unsafe ECS package references from non-ECS projects
- renaming public types or splitting projects

## Expected Workflow

1. Run the validator for the whole workspace or a target plugin.
2. Review `Error` violations first.
3. Run with `-Fix` for safe namespace and csproj exclusion fixes.
4. For shared ECS bridge changes, add or update plugin-level regression tests around the affected contract before widening to downstream game suites.
5. Manually move ECS files or create missing `.ECS.csproj` projects where required.
6. Re-run the validator and the owning plugin test slice until both exit cleanly.

## Report Back

When using this skill, summarize results by plugin:

- which plugin failed
- whether the issue is file placement, namespace drift, missing `.ECS.csproj`, or csproj dependency drift
- whether the owning plugin contract tests were added or updated
- which violations were auto-fixed versus still manual