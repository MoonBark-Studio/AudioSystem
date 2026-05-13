# Agents

## Project Overview

This repository serves as the **Hub** in our **Hub-and-Spoke Architecture**, unifying MoonBark Studio's Godot ecosystem. The individual game and plugin repositories act as the **Spokes**, connected via Git submodules.

It contains:
- **cores/** - Framework spokes (e.g., MoonBark.Framework)
- **games/** - Game project spokes (e.g., moonbark-idle, thistletide)
- **plugins/** - Plugin and core framework spokes (all internals, generic plugins)

## Working with Submodules

### Clone All

```bash
git clone --recurse-submodules https://github.com/MoonBark-Studio/GodotProjects
```

### Update All Submodules

```bash
git submodule update --init --recursive
```

### Pull with Submodules

```bash
git pull --recurse-submodules
```

### Sync Submodule to Parent

When a submodule has new commits that need to be tracked by the hub:

```bash
git add <submodule-path>
git commit -m "chore: sync <submodule>"
git push
```

## Adding New Submodules

```bash
git submodule add <repo_url> <path>
git commit -m "submodule: add <name>"
git push origin main
```

## Removing Submodules

```bash
git rm <path>
git commit -m "submodule: remove <name>"
git push origin main
```

## Branch Structure

- **main** - Production-ready state
- Submodules have their own branch/release cycles

## Godot Test Running

- Use the Godot test-running skill and project-local runner wrappers for Godot/GDUnit/GoDotTest suites.
- Prefer the smallest affected class or suite first, then widen only after focused verification passes.
- Treat runner/build infrastructure failures separately from test assertion failures and document blockers explicitly.

## Plugin EventBus / SignalBus Contract

- Core `EventBus` types are engine-agnostic C# event publishers for domain logic and tests.
- Godot `SignalBus` types are adapter/delegation layers that subscribe to or wrap Core buses and expose the same event payloads through C# callbacks and Godot signals.
- For plugins with both layers, add parity tests proving Core publishes and SignalBus publishes/bridges preserve callback triggering and payload values.
