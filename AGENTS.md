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
