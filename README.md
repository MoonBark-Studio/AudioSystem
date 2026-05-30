# MoonBark AudioSystem Plugin

A high-performance modular plugin for the MoonBark framework.

## Installation

Add a reference to your C# project:
```xml
<ProjectReference Include="../plugins/AudioSystem/Godot/MoonBark.AudioSystem.Godot.csproj" />
```

## Features
- Standardized layer separation
- Fully decoupled C# Core design
- Friflo ECS integration
- Built-in automated test coverage
- **Playlist support** with shuffle and inter-track delay
- **One-shot audio pool** (32-slot ring buffer)
- **Fade in/out** for music and ambient tracks
- **Ambient ducking** when one-shots play

## Audio Configuration

The plugin uses `audio_config.json` to map cue IDs to file paths:

```json
{
  "source_pack": {
    "name": "My Audio Pack",
    "license_name": "MIT"
  },
  "audio_root_path": "res://assets/audio",
  "cues": {
    "jump": "sfx/jump.wav",
    "coin": "sfx/coin.wav"
  },
  "music": {
    "main_theme": "music/background.ogg",
    "battle": "music/battle.ogg"
  },
  "ambient": {
    "forest": "ambient/forest.ogg"
  },
  "playlists": {
    "gameplay": {
      "id": "gameplay",
      "track_ids": ["main_theme", "battle"],
      "shuffle": false,
      "inter_track_delay_sec": 2.0,
      "loop": true
    }
  }
}
```

### Playlist Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Playlist identifier |
| `track_ids` | string[] | Ordered list of music cue IDs |
| `shuffle` | bool | Randomize track order (default: false) |
| `inter_track_delay_sec` | float | Silence gap between tracks (default: 0) |
| `loop` | bool | Loop playlist at end (default: true) |

## API Reference

### IAudioPlayback
```csharp
void PlayOneShot(string cueId);
void PlayMusic(string cueId, float fadeInDurationSec = 0f);
void PlayAmbient(string cueId, float fadeInDurationSec = 0f);
void StopMusic(float fadeOutSec = 0.3f);
void StopAmbient(float fadeOutSec = 0.3f);
```

### IAudioVolumeControl
```csharp
void SetMasterVolume(float volume);   // 0-1
void SetMusicVolume(float volume);    // 0-1
void SetSoundEffectsVolume(float volume); // 0-1
```

### IAudioPlaylistControl
```csharp
void SetActivePlaylist(string playlistId, float fadeDurationSec = 0.5f);
void ClearActivePlaylist();
void SetShuffleEnabled(bool enabled);
void SetInterTrackDelay(float seconds);
void SkipToNext();
void SkipToPrevious();
```

## Game Settings Integration

Configure playlist playback via `game_settings.json`:

```json
{
  "audio": {
    "master_volume": 1.0,
    "music_volume": 0.7,
    "sfx_volume": 0.8,
    "music": {
      "enabled": true,
      "active_playlist": "gameplay",
      "shuffle": false,
      "inter_track_delay_sec": 2.0
    }
  }
}
```

## Architecture

```
AudioSystem/
├── Core/                           # Pure C#, no Godot dependencies
│   ├── IAudioService.cs            # Interfaces: IAudioPlayback, IAudioVolumeControl, IAudioPlaylistControl
│   └── Configuration/
│       └── AudioConfigModels.cs    # AudioConfigDocument, PlaylistConfig
├── Godot/                          # Godot integration layer
│   └── Systems/
│       └── GodotAudioManager.cs    # Main implementation (Node, creates AudioStreamPlayers)
├── ECS/                            # ECS integration
└── addons/audio_system/           # Godot addon
```

## Godot API Analysis

The following capabilities are **custom-built** (Godot has no built-in equivalents):

| Capability | Custom Implementation |
|------------|----------------------|
| Playlist queue | `_playlistOrder` list + index tracking |
| Shuffle | Fisher-Yates algorithm in `ShuffleList()` |
| Inter-track delay | `_delayRemaining` countdown in `_Process()` |
| Cross-track fade | Pending cue → fade → play in `_Process()` |

The following capabilities use **Godot built-ins**:

| Capability | Godot API |
|-----------|----------|
| Stream loading | `ResourceLoader.Load<AudioStream>()` |
| Per-stream loop | `AudioStreamOggVorbis.Loop`, `AudioStreamWav.LoopMode` |
| Fade in/out | `VolumeDb` + `_Process` delta |
| One-shot pool | 32-slot `AudioStreamPlayer[]` ring buffer |
| Volume per bus | `AudioServer.GetBusVolumeDb()` |
