# AudioSystem — Roadmap

## What's Next

| Priority | Item | Target | Notes |
|----------|------|--------|-------|
| P1 | Add ROADMAP.md | v1.0 | Documentation debt |
| P2 | Remove debug print in AudioBenchmark | v1.1 | Dead code in production |
| P2 | Add audio fade/blend transitions | v1.2 | Missing feature |
| P3 | Consider audio bus abstraction | v2.0 | For multi-engine support |

## Version History

### v1.0 (Current)
- ECS-driven cue selection via AudioCueSystem
- GodotAudioManager for playback (refactored 2026-04-14)
- JSON audio configuration loading
- World state integration (reads time.is_night, narrative.active_event_kind, task.completed_last_frame)

### v1.1 (Next)
- Clean up AudioBenchmark debug code

### v1.2 (Planned)
- Audio fade/blend transitions between cues
- Music/ambient crossfade support

### v2.0 (Future)
- Audio bus abstraction for non-Godot engines
- Spatial audio support

## Dependencies

- `WorldState` (reads/writes audio.* keys)
- `Friflo.Engine.ECS`
- `GodotSharp`

## Architecture

```
AudioSystem/
├── Core/
│   ├── Configuration/ (AudioConfigLoader, AudioConfigModels)
│   └── IAudioService.cs
├── Godot/
│   ├── GodotAudioManager.cs (refactored 2026-04-14)
│   └── AudioBenchmark.cs
└── Tests/ (47 tests)
```