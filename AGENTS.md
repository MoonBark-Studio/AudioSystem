# Agents

## Project Overview

This is the **MoonBark Studio monorepo** containing all games, plugins, and framework code.

```
projects/                     ← Git repo root
├── cores/                    # Shared frameworks
│   └── MoonBark.Framework/
├── games/                    # Game projects
│   ├── moonbark-idle/
│   └── thistletide/
└── plugins/                  # Plugin projects
    ├── GridPlacement/
    ├── ItemVault/
    ├── WorldTime/
    └── ...
```

## Common Commands

```bash
cd projects

# Status
git status

# Commit all changes
git add .
git commit -m "description"
git push

# Check diffs
git diff --stat
```

## Branch Structure

- **main** - Production-ready state
