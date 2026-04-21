# Agents

## Project Overview

This is the git superproject for MoonBark Studio's Godot ecosystem. It contains:

- **cores/** - Core libraries (MoonBark.ECS, MoonBark.Framework)
- **games/** - Game projects (moonbark-idle, thistletide)
- **plugins/** - Godot plugins (18 total)

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
