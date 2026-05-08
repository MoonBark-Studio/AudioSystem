# 20-Step Test Infrastructure Plan

> Status: Phase 1 complete (Steps 1–4). Current totals: **56 passing, 21 failing** across WorldTime + Minimap Godot tests.

---

## Phase 1: Fix Broken Test Runners ✅ COMPLETE

| Step | Task | Outcome |
|------|------|---------|
| 1 | WorldTime: create `WorldTimeTests.tscn`, fix `project.godot` assembly name | Runner instantiates |
| 2 | WorldTime: fix `.csproj` — exclude `EditorPlugin`, add `CopyLocalLockFileAssemblies` | Assembly loads |
| 3 | Minimap: create runner scene, move `MinimapGodotTests.cs` to main assembly | Runner scene exists |
| 4 | Clean rebuild all `.godot` caches; resolve stale-assembly `MissingMethodException` | Tests execute |

---

## Phase 2: Close Coverage Gaps

### Step 5 — Fix WorldTime Godot Failures (7 tests)
**Effort**: Small | **Priority**: High

| Failing Test | Root Cause | Recommended Fix |
|-------------|------------|-----------------|
| `DateChanged_Signal_FiresOnMidnightRollover` | `_lastEmittedDay = -1` skips first emission | Add `Initialize()` call in test setup, or expose `_lastEmittedDay` reset |
| `DateChanged_Signal_ReportsCorrectNewAndPreviousDates` | Same guard | Same fix |
| `AdvanceTime_PastLastDayOfYear_RollsToNextYear` | `CurrentDate.Year` stays 1 | Verify `AdvanceTime` triggers `OnTimeChanged` date rollover logic |
| `AdvanceTime_MultipleDays_FiresDateChangedForEachDay` | `dateChangedCount` = 0 | `_Process` not called in headless; call `_timeManager.Update()` manually in test |
| `OnDateChanged_CSharpEvent_FiresOnMidnightRollover` | C# event never fires | Same as signal tests — needs `_Process` or manual update |
| `TimeScale_DefaultsToOne` | Returns `60f` (TimeScale.SecondsPerMinute) | Property returns `DeltaMultiplier`, but test expects `1.0f`; verify `Initialize()` sets default `DeltaMultiplier = 1f` |
| `TimeStateChanged_Signal_IsEmitted_WhenPaused` | Signal not received | `Pause()` doesn't emit if already unpaused; ensure `Initialize()` called before test |

**Common pattern**: Most failures share the same root — `TimeContextNode.Initialize(GameCalendar)` is required before signals/properties work, but some tests instantiate the node without calling `Initialize()`.

---

### Step 6 — Fix Minimap Godot Failures (14 tests)
**Effort**: Medium | **Priority**: High

| Category | Count | Root Cause | Recommended Fix |
|----------|-------|------------|-----------------|
| Reticle offset | 5 | Offset math returns `107.52` instead of expected range | `MinimapView` reticle calculation uses `OffsetLeft/Right` with anchor offsets; test expectations may be outdated after zoom math changes |
| Zoom boundary | 3 | `should be less than X but was X` | Floating-point exact comparison; change `ShouldBeLessThan` → `ShouldBeInRange` with epsilon |
| MouseFilter | 2 | `Stop` instead of `Pass` | `ReferenceRect`/`Panel` default changed in Godot 4.x; set `MouseFilter = Pass` in test `[Setup]` or update test assertion |
| Camera not moving | 1 | `_mainCamera.Position` unchanged | `ClickHandler` requires `Enabled = true` and signal wiring; verify `_clickHandler.TargetCamera` is set |
| Zoom sync | 1 | `Math.Abs(Zoom - initialZoom) = 0.34` | `MinimapView.Zoom` property may lag one frame; add `await ProcessFrame` before assertion |
| Anchor update | 1 | `Math.Abs(anchorLeft2 - anchorLeft1) = 0` | `ReticleFill` anchors not updating when camera moves; verify `SetPlayerPosition` triggers anchor recalculation |

**Recommended approach**: Fix the 3 zoom boundary tests first (trivial `ShouldBeInRange` change), then tackle the reticle math as a group.

---

### Step 7 — ItemDrops Core: Investigate 2 Skipped Tests
**Effort**: Small | **Priority**: Medium

Locate and read the skipped tests in `ItemDrops/Core/Tests/`. Determine if they are:
- **Obsolete** → delete
- **Blocked by missing feature** → document blocker and re-enable
- **Flaky** → add `[Trait("Category", "Flaky")]` and skip with explicit reason

---

### Step 8 — Audit All 25 Plugins for Hidden/Stale Test Assemblies
**Effort**: Medium | **Priority**: High

Search each plugin for:
1. `.csproj` with `IsTestProject=true` or `GoDotTest` reference
2. `project.godot` with `run/main_scene` pointing to a test scene
3. `Tests/` or `test/` folders with `.cs` files not referenced by any `.csproj`

**Deliverable**: Spreadsheet/list of which plugins have tests, which have broken runners, which have orphaned test code.

**Likely candidates** (based on folder names):
- `GridPlacement`, `Inventory`, `CharacterController`, `WorldGen2D`, `Dialogue`, `SaveLoad`

---

### Step 9 — Establish Minimum Coverage Threshold Per Plugin
**Effort**: Small | **Priority**: Medium

Define two tiers:

| Tier | Plugins | Requirement |
|------|---------|-------------|
| **Core** | Framework, WorldTime, Minimap, ItemDrops | All tests must pass; no skipped without documented reason |
| **Plugin** | All others | At minimum, project builds with 0 errors; test runner scene exists if tests present |

Document in `plugins/.kimi/AGENTS.md`.

---

## Phase 3: Workspace-Wide Test Infrastructure

### Step 10 — Create Root-Level Test Runner Script
**Effort**: Medium | **Priority**: High

Create `plugins/run-all-tests.ps1` that:
1. Discovers all plugins with `.sln` or `.csproj`
2. Builds Core/xUnit projects via `dotnet test`
3. Builds Godot projects via `dotnet build`
4. Runs Godot headless tests for each plugin with a test runner scene
5. Aggregates results into a single exit code + report

**Usage**:
```powershell
./run-all-tests.ps1
# Output: summary table + exit code = number of failures
```

---

### Step 11 — Unify Test Project Structure Across All Plugins
**Effort**: Medium | **Priority**: Medium

Standardize every Godot plugin to follow this layout:

```
PluginName/
├── Core/
├── Godot/
│   ├── project.godot
│   ├── Tests/
│   │   ├── runner/
│   │   │   ├── PluginNameTests.tscn   ← main scene for test runs
│   │   │   └── PluginNameTests.cs     ← #if DEBUG only
│   │   ├── integration/
│   │   └── MoonBark.PluginName.Godot.Tests.csproj
│   └── PluginName.csproj
```

**Required `.csproj` settings**:
```xml
<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
<EnableDefaultCompileItems>false</EnableDefaultCompileItems>  <!-- Godot only -->
```

**Required test `.csproj` settings**:
```xml
<OutputPath>.godot/mono/temp/bin/$(Configuration)/</OutputPath>
```

---

### Step 12 — Document Test Patterns in `AGENTS.md`
**Effort**: Small | **Priority**: Medium

Expand `plugins/.kimi/AGENTS.md` with:
- Exact command to run Godot headless tests per plugin
- How to add a new plugin to the test matrix
- Troubleshooting section for dual-assembly-load issues
- Chickensoft.GoDotTest lifecycle attributes reference

---

### Step 13 — Create Workspace Test Health Dashboard
**Effort**: Small | **Priority**: Low

Create `plugins/TEST_RESULTS.md` (auto-upenerated by `run-all-tests.ps1`) showing:

| Plugin | Stack | Total | Pass | Fail | Skip | Last Run |
|--------|-------|-------|------|------|------|----------|
| WorldTime | GoDotTest | 32 | 25 | 7 | 0 | 2026-05-07 |
| Minimap | GoDotTest | 45 | 31 | 14 | 0 | 2026-05-07 |
| ItemDrops | xUnit | 162 | 160 | 0 | 2 | ... |

---

## Phase 4: Code Health & CI Readiness

### Step 14 — Fix `CalendarDisplay.cs` Delegate Signatures
**Effort**: Small | **Priority**: Medium

`addons/WorldTime/UI/CalendarDisplay.cs` is excluded from compilation. Fix the 3 mismatches:

| Line | Problem | Fix |
|------|---------|-----|
| 68, 69 | `Action<int>` assigned `() => ...` (0 args) | Change field type to `Action` or add `int` parameter |
| 89 | `Action<int,int,int,int,int,int>` | Verify `TimeContextNode.SignalName.TimeOfDayChanged` still uses 6 ints |
| 91 | `Action<GameDate,GameDate>` assigned 6-arg lambda | Use `OnDateChanged(GameDate, GameDate)` overload or match signal signature |

After fixing, remove `<Compile Remove="...CalendarDisplay.cs" />` from `Godot.csproj`.

---

### Step 15 — Standardize `.csproj` Settings Across All Godot Plugins
**Effort**: Medium | **Priority**: Medium

Audit all 10 plugins with `.sln` files for:
- `CopyLocalLockFileAssemblies` missing → add
- Explicit `GodotSharp`/`Godot.SourceGenerators` PackageReferences → remove (causes NETSDK1023 warnings)
- `Nullable` not enabled → add `<Nullable>enable</Nullable>`
- `ImplicitUsings` not enabled → add `<ImplicitUsings>enable</ImplicitUsings>`

---

### Step 16 — Add Package READMEs to Suppress NuGet Warnings
**Effort**: Small | **Priority**: Low

WorldTime and Minimap builds emit:
```
The package X is missing a readme.
```

Add `<PackageReadmeFile>README.md</PackageReadmeFile>` + `README.md` to each plugin that generates NuGet packages, OR set `<IsPackable>false</IsPackable>` if packages are not actually distributed.

---

### Step 17 — Remove/Fix Stale `project.godot` Assembly Name Mismatches
**Effort**: Small | **Priority**: High

Scan all `project.godot` files:
```powershell
gci -r project.godot | % { $_; sls "assembly_name" $_ }
```

Verify `project/assembly_name` exactly matches the `<AssemblyName>` in the corresponding `.csproj`. Fix any mismatches (WorldTime was already fixed; Minimap was already correct).

---

### Step 18 — Pre-Commit Build Verification
**Effort**: Medium | **Priority**: Medium

Create `plugins/build-all.ps1` that:
1. Builds every `.sln` with `dotnet build --no-restore`
2. Fails on any error (warnings OK per policy)
3. Outputs which plugin failed and why

Run this before any PR/commit to catch assembly name mismatches, missing references, and stale cache issues early.

---

### Step 19 — CI-Ready GitHub Actions / Build Script
**Effort**: Medium | **Priority**: Low

Create `.github/workflows/test.yml` (or equivalent) that:
1. Installs Godot 4.6.2 via `godotenv`
2. Runs `build-all.ps1`
3. Runs `run-all-tests.ps1`
4. Publishes `TEST_RESULTS.md` as artifact

**Note**: Requires `GODOT` env var setup and Windows runner (or WINE on Linux for Godot headless).

---

### Step 20 — Final Audit & Documentation
**Effort**: Small | **Priority**: Medium

1. Verify every plugin in the matrix builds with 0 errors
2. Verify every test runner scene runs headless and exits cleanly
3. Update `AGENTS.md` with final architecture decisions
4. Archive this plan; open follow-up issues for any deferred work

---

## Recommended Execution Order

```
Week 1 (High impact, low effort):
  Step 5  → Step 6  → Step 7  → Step 14

Week 2 (Infrastructure):
  Step 8  → Step 10 → Step 11 → Step 12

Week 3 (Polish & CI):
  Step 15 → Step 16 → Step 17 → Step 18 → Step 19 → Step 20

Ongoing:
  Step 9  (maintain thresholds)
  Step 13 (update dashboard after each run)
```

---

## Risk Register

| Risk | Mitigation |
|------|------------|
| Reticle math fixes in Step 6 may require UI redesign | Timebox to 2 days; if not trivial, split to separate issue |
| Other plugins may have worse `EditorPlugin` headless issues | Step 8 audit will surface them; apply same `#if TOOLS` / exclusion pattern |
| `godotenv` path varies across machines | Document in `AGENTS.md`; use `where godotenv` fallback in scripts |
| Floating-point tests continue to fail on different hardware | Standardize on `ShouldBeInRange` with 0.01–0.1 epsilon everywhere |
