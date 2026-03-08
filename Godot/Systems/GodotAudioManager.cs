using AudioSystem.Core.Configuration;
using Godot;
using System;
using System.IO;
using System.Reflection;

namespace AudioSystem.Godot.Systems;

/// <summary>
/// Godot audio manager that mirrors world-state audio directives into music, ambient, and one-shot playback.
/// </summary>
public partial class GodotAudioManager : Node
{
    private const string MusicPlayerNodeName = "MusicPlayer";
    private const string AmbientPlayerNodeName = "AmbientPlayer";
    private const float DefaultMusicVolume = 0.0f;
    private const float DefaultAmbientVolume = -10.0f;

    private readonly AudioPathCollection _cuePaths = new();
    private readonly AudioPathCollection _musicPaths = new();
    private readonly AudioPathCollection _ambientPaths = new();
    private AudioStreamPlayer? _musicPlayer;
    private AudioStreamPlayer? _ambientPlayer;
    private global::WorldState.WorldState? _worldState;
    private string _currentMusicTrack = string.Empty;
    private string _currentAmbientTrack = string.Empty;
    private int _lastCueVersion;

    /// <summary>
    /// Initializes audio players and loads configured cue, music, and ambient path mappings.
    /// </summary>
    public override void _Ready()
    {
        _musicPlayer = new AudioStreamPlayer();
        _musicPlayer.Name = MusicPlayerNodeName;
        _musicPlayer.Bus = "Master";
        _musicPlayer.VolumeDb = DefaultMusicVolume;
        _musicPlayer.Finished += () =>
        {
            if (_musicPlayer.Stream != null)
            {
                _musicPlayer.Play();
            }
        };
        AddChild(_musicPlayer);

        _ambientPlayer = new AudioStreamPlayer();
    _ambientPlayer.Name = AmbientPlayerNodeName;
        _ambientPlayer.Bus = "Master";
        _ambientPlayer.VolumeDb = DefaultAmbientVolume;
        AddChild(_ambientPlayer);

        try
        {
            string configPath = AudioConfigLoader.ResolveConfigPath();
            AudioConfigDocument config = AudioConfigLoader.Load(configPath);
            CopyMappings(_cuePaths, AudioConfigLoader.BuildAbsolutePathMap(config, config.Cues, configPath));
            CopyMappings(_musicPaths, AudioConfigLoader.BuildAbsolutePathMap(config, config.Music, configPath));
            CopyMappings(_ambientPaths, AudioConfigLoader.BuildAbsolutePathMap(config, config.Ambient, configPath));
        }
        catch (FileNotFoundException ex)
        {
            GD.PrintErr($"GodotAudioManager: Audio config file not found: {ex.Message}");
        }
        catch (IOException ex)
        {
            GD.PrintErr($"GodotAudioManager: Failed to read audio config file: {ex.Message}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"GodotAudioManager: Unexpected error loading audio config: {ex.Message}");
        }
    }

    /// <summary>
    /// Polls the bound world state and updates active audio playback to match the latest audio keys.
    /// </summary>
    /// <param name="delta">The elapsed frame time in seconds.</param>
    public override void _Process(double delta)
    {
        if (_worldState == null)
        {
            return;
        }

        string musicTrack = _worldState.GetValue("audio.music") as string ?? string.Empty;
        string ambientTrack = _worldState.GetValue("audio.ambient") as string ?? string.Empty;
        int cueVersion = GetInt(_worldState.GetValue("audio.cue_version"));
        string cueId = _worldState.GetValue("audio.last_cue") as string ?? string.Empty;

        if (!string.Equals(musicTrack, _currentMusicTrack, StringComparison.OrdinalIgnoreCase))
        {
            _currentMusicTrack = musicTrack;
            PlayLoop(_musicPlayer, musicTrack, _musicPaths);
        }

        if (!string.Equals(ambientTrack, _currentAmbientTrack, StringComparison.OrdinalIgnoreCase))
        {
            _currentAmbientTrack = ambientTrack;
            PlayLoop(_ambientPlayer, ambientTrack, _ambientPaths);
        }

        if (cueVersion > _lastCueVersion && !string.IsNullOrWhiteSpace(cueId))
        {
            PlayOneShot(cueId);
            _lastCueVersion = cueVersion;
        }
    }

    private static void CopyMappings(AudioPathCollection target, AudioPathCollection source)
    {
        target.Clear();
        foreach (string cueId in source.CueIds)
        {
            if (source.TryGetPath(cueId, out string? path))
            {
                target.Add(cueId, path);
            }
        }
    }

    /// <summary>
    /// Sets the world-state source used to drive audio playback decisions.
    /// </summary>
    /// <param name="worldState">The world-state instance containing audio directives.</param>
    public void SetWorldState(global::WorldState.WorldState worldState)
    {
        _worldState = worldState;
    }

    private void PlayLoop(AudioStreamPlayer? player, string cueId, AudioPathCollection mapping)
    {
        if (player == null)
        {
            return;
        }

        player.Stop();
        player.Stream = null;

        if (!mapping.TryGetPath(cueId, out string? path) || string.IsNullOrWhiteSpace(path) || !CanResolveAudioPath(path))
        {
            return;
        }

        AudioStream? stream = LoadAudioStream(path);
        if (stream == null)
        {
            return;
        }

        player.Stream = stream;
        player.Play();
    }

    private void PlayOneShot(string cueId)
    {
        if (!_cuePaths.TryGetPath(cueId, out string? path) || string.IsNullOrWhiteSpace(path) || !CanResolveAudioPath(path))
        {
            return;
        }

        AudioStream? stream = LoadAudioStream(path);
        if (stream == null)
        {
            return;
        }

        AudioStreamPlayer oneShotPlayer = new();
        oneShotPlayer.Bus = "Master";
        oneShotPlayer.Stream = stream;
        AddChild(oneShotPlayer);
        oneShotPlayer.Finished += () =>
        {
            oneShotPlayer.QueueFree();
        };
        oneShotPlayer.Play();
    }

    private static AudioStream? LoadAudioStream(string path)
    {
        try
        {
            if (IsGodotResourcePath(path))
            {
                AudioStream? resourceStream = ResourceLoader.Load<AudioStream>(path);
                if (resourceStream != null)
                {
                    return resourceStream;
                }

                GD.PrintErr($"GodotAudioManager: Failed to load audio resource '{path}': ResourceLoader returned null");
                return null;
            }

            MethodInfo? loadFromFileMethod = typeof(AudioStreamWav).GetMethod(
                "LoadFromFile",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(string)],
                modifiers: null);
            if (loadFromFileMethod == null)
            {
                GD.PrintErr($"GodotAudioManager: External wav loading is unavailable in this Godot runtime: {path}");
                return null;
            }

            AudioStreamWav? stream = loadFromFileMethod.Invoke(obj: null, parameters: [path]) as AudioStreamWav;
            if (stream != null)
            {
                return stream;
            }

            GD.PrintErr($"GodotAudioManager: Failed to load external wav '{path}': LoadFromFile returned null");
            return null;
        }
        catch (FileNotFoundException)
        {
            GD.PrintErr($"GodotAudioManager: Audio file not found: {path}");
            return null;
        }
        catch (IOException ex)
        {
            GD.PrintErr($"GodotAudioManager: Failed to read audio file '{path}': {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"GodotAudioManager: Unexpected error loading audio '{path}': {ex.Message}");
            return null;
        }
    }

    private static bool IsGodotResourcePath(string path)
    {
        return path.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanResolveAudioPath(string path)
    {
        return IsGodotResourcePath(path)
            ? ResourceLoader.Exists(path)
            : File.Exists(path);
    }

    private static int GetInt(object? value)
    {
        return value switch
        {
            int integer => integer,
            float single => (int)single,
            double dbl => (int)dbl,
            _ => 0
        };
    }
}
