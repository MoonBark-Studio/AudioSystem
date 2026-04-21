# Health

## Overview

MoonBark Studio's Godot plugin ecosystem and game projects.

## Project Status

- **Last Updated:** 2026-04-21
- **Repository:** [GodotProjects](https://github.com/MoonBark-Studio/GodotProjects)
- **Main Branch:** main

## Structure

```
├── cores/           # Core libraries
├── games/           # Game projects  
└── plugins/         # Godot plugins (18 total)
```

## Submodules

All submodules are tracked via `.gitmodules` and initialized on clone:

```bash
git clone --recurse-submodules https://github.com/MoonBark-Studio/GodotProjects
```

## Recent Changes

- 2026-04-21: Initial structure with cores/, games/, plugins/
- Moved MoonBark.Framework and MoonBark.ECS to cores/
- Reorganized plugin directory

## Known Issues

- Nested submodule in games/moonbark-idle/godot/assets/maps not configured
