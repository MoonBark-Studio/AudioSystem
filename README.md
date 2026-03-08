# AudioSystem Plugin

A reusable world-state-driven audio orchestration plugin for Godot games.

## Includes
- `AudioCueSystem` for ECS-driven cue selection
- `GodotAudioManager` for Godot playback
- YAML-backed audio configuration loading
- `AudioStateComponent` for runtime state

## Godot Audio Manager Contract
- Creates named `MusicPlayer` and `AmbientPlayer` child nodes during `_Ready()`.
- Mirrors `audio.music`, `audio.ambient`, `audio.last_cue`, and `audio.cue_version` from world state.
- Loads both Godot resource paths like `res://...` and external `.wav` file paths.

## Dependencies
- `WorldState`
- `Friflo.Engine.ECS`
- `YamlDotNet`
- `GodotSharp` for Godot integration

## World State Contract
Reads:
- `time.is_night`
- `narrative.active_event_kind`
- `task.completed_last_frame`

Writes:
- `audio.ambient`
- `audio.music`
- `audio.last_cue`
- `audio.cue_version`
