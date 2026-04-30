# AudioSystem

A JSON-configured, Godot-integrated audio management system for MoonBark games. Loads cue-to-path mappings from `audio_config.json` and exposes music, ambient, and one-shot playback via a pooled, zero-GC `GodotAudioManager` node.

## Key Features

- **JSON Configuration** — `AudioConfigLoader` resolves and deserializes `audio_config.json` with support for `res://` and absolute file paths.
- **Cue Mapping** — `AudioPathCollection` provides case-insensitive, type-safe lookups for cues, music tracks, and ambient loops.
- **GodotAudioManager** — Godot `Node` that manages:
  - Dedicated music & ambient loop players with cross-fade support
  - Pre-allocated one-shot pool (32 slots, ring-buffer reuse, zero GC after init)
  - Automatic ambient ducking during one-shot playback
  - Master / music / SFX volume control
- **Pure C# Core** — `IAudioService` interface and configuration models have no Godot dependencies, enabling testability outside the engine.
- **Benchmark Scene** — Included Godot project for load-time and runtime audio benchmarking.

## Architecture

```
AudioSystem/
├── Core/
│   ├── AudioSystemModule.cs              # Framework module registration
│   ├── IAudioService.cs                  # Pure C# audio service interface
│   └── Configuration/
│       ├── AudioConfigLoader.cs          # JSON resolution & loading
│       └── AudioConfigModels.cs          # Document, path collection, source metadata
├── Godot/
│   ├── Systems/
│   │   └── GodotAudioManager.cs          # Godot Node implementation
│   ├── scenes/
│   │   └── benchmark.tscn
│   └── project.godot
├── Tests/
│   ├── AudioConfigLoaderTests.cs
│   ├── AudioConfigModelsTests.cs
│   ├── GodotAudioManagerTests.cs
│   └── Fixtures/
│       └── audio_config.json
└── docs/
    └── ARCHITECTURE.md
```

## Dependencies

- **.NET 8.0**
- **Friflo.Engine.ECS** 3.6.0
- **MoonBark.Framework** (project reference)
- **Godot 4.x** (for `GodotAudioManager` only; core is engine-agnostic)

## Usage Example

```csharp
// Core: load configuration
var config = AudioConfigLoader.Load("Assets/Audio/audio_config.json");
var cues = AudioConfigLoader.BuildAbsolutePathMap(config, config.Cues);

// Godot: add GodotAudioManager to scene
var audioManager = new GodotAudioManager();
AddChild(audioManager); // _Ready() auto-loads config

// Play from GDScript signal handlers
audioManager.TryPlayMusic("main_theme", fadeInDurationSec: 0.5f);
audioManager.TryPlayOneShot("sword_swing");
audioManager.TryStopMusic(fadeOutSec: 0.3f);

// Volume control
audioManager.SetMasterVolume(0.8f);
audioManager.SetMusicVolume(0.5f);
```

## Status
- ✅ Audited: 2026-04-30
- Changed files this run: 13
- File count: 13 C# files (~2533 lines)
## Key Types
## Key Types (13 files, ~2533 lines)
AudioBenchmark, AudioConfigDocument, AudioConfigDocumentTests, AudioConfigLoader, AudioConfigLoaderTests, AudioPathCollection, AudioPathCollectionTests, AudioSourceConfig, AudioSourceConfigTests, AudioSystemModule, BenchmarkPhase, GodotAudioManager, GodotAudioManagerTests, IAudioService, TestPlayer

## Namespaces
- `MoonBark.AudioSystem`
- `MoonBark.AudioSystem.Core`
- `MoonBark.AudioSystem.Core.Configuration`
- `MoonBark.AudioSystem.Godot.Systems`
- `MoonBark.AudioSystem.Tests`

## ECS Architecture (v2)
- ECS subdirectories: none
- ECS files outside subdirectories: 0
- Flat structure: Core/, ECS/, Godot/ (cs/ prefix not required)