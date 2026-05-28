using MoonBark.AudioSystem.Core.Configuration;
using MoonBark.AudioSystem.Core.Diagnostics;
using MoonBark.Framework.Logging;
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MoonBark.AudioSystem.Godot.Systems;

/// <summary>
/// Godot audio manager that plays music, ambient, and one-shot audio cues via direct method calls.
/// Call TryPlayMusic / TryPlayAmbient / TryPlayOneShot from GDScript signal handlers.
/// YAML config provides cue-to-path mappings.
/// </summary>
public partial class GodotAudioManager : Node
{
    private const string MusicPlayerNodeName = "MusicPlayer";
    private const string AmbientPlayerNodeName = "AmbientPlayer";
    private const float DefaultMusicVolume = 0.0f;
    private const float DefaultAmbientVolume = -10.0f;
    internal const int OneShotPoolMaxSizeConstant = 32;
    private const int OneShotPoolMaxSize = OneShotPoolMaxSizeConstant;  // MoonBark Idle target; covers virtually all concurrent needs
    private const float FadeEpsilon = 0.001f;

    private readonly AudioPathCollection _cuePaths = new();
    private readonly AudioPathCollection _musicPaths = new();
    private readonly AudioPathCollection _ambientPaths = new();
    private AudioStreamPlayer? _musicPlayer;
    private AudioStreamPlayer? _ambientPlayer;

    // ── Approach B: Pre-allocated pool (primary) ────────────────────────────────
    // Zero GC after init. Ring-buffer reuse — just call Play() on an idle slot.
    // Concurrent cap: OneShotPoolMaxSize (hard cap; sounds beyond are silently dropped).
    //
    // APPROACH D (AudioStreamGenerator + PushFrame) as overflow:
    //   D requires per-frame PCM feeding via AudioStreamGeneratorPlayback.PushFrame().
    //   This adds CPU overhead and is only useful for truly procedural audio (synths, DSP).
    //   For one-shot file playback, D offers no benefit over B — increase pool size instead.
    //   Pool size is configurable via OneShotPoolMaxSize. For >32 concurrent sounds,
    //   increase to 64 or more; Godot can handle hundreds of idle AudioStreamPlayer nodes.
    private AudioStreamPlayer[] _oneShotPool = Array.Empty<AudioStreamPlayer>();
    private int _poolCursor;  // ring-buffer cursor for round-robin reuse

    // Volume control
    private float _masterVolume = 1.0f;
    private float _musicVolume = 1.0f;
    private float _ambientVolume = 1.0f;

    // Fade control
    private float _musicFadeRate;
    private float _ambientFadeRate;
    private bool _musicFadingIn;
    private bool _ambientFadingIn;
    private string? _pendingMusicCueId;
    private string? _pendingAmbientCueId;

    // Ambient ducking (ref-counted while one-shots are playing)
    private float _ambientVolumeBeforeDuck = 1.0f;
    private bool _ambientDucking = false;
    private int _playingOneShotCount;

    private string? _activeMusicCueId;

    private readonly GodotAudioPlaybackDiagnostics _diagnostics;
    private readonly ILogger? _logger;

    private Action? _musicFinishedHandler;
    private Action? _oneShotFinishedHandler;

    // Bus names
    private string _musicBus = "Music";
    private string _sfxBus = "SFX";

    /// <summary>Optional absolute path to audio_config.json. When unset, <see cref="AudioConfigLoader.ResolveConfigPath"/> is used.</summary>
    public string? ConfigFilePath { get; set; }

    public GodotAudioManager() : this(null) { }

    public GodotAudioManager(ILogger? logger)
    {
        _logger = logger;
        _diagnostics = new GodotAudioPlaybackDiagnostics(logger);
    }

    /// <summary>
    /// Initializes audio players and loads configured cue, music, and ambient path mappings.
    /// </summary>
    public override void _Ready()
    {
        // Resolve bus names (fall back to Master if not found)
        _musicBus = ResolveBusName("Music");
        _sfxBus = ResolveBusName("SFX");

        _musicPlayer = new AudioStreamPlayer();
        _musicPlayer.Name = MusicPlayerNodeName;
        _musicPlayer.Bus = _musicBus;
        _musicPlayer.VolumeDb = DefaultMusicVolume;
        _musicFinishedHandler = OnMusicFinished;
        _musicPlayer.Finished += _musicFinishedHandler;
        AddChild(_musicPlayer);

        _ambientPlayer = new AudioStreamPlayer();
        _ambientPlayer.Name = AmbientPlayerNodeName;
        _ambientPlayer.Bus = _sfxBus;
        _ambientPlayer.VolumeDb = DefaultAmbientVolume;
        AddChild(_ambientPlayer);

        _oneShotFinishedHandler = OnOneShotPoolPlayerFinished;

        // ── Approach B: Pre-allocate one-shot pool ─────────────────────────────
        // Pool size 32 covers virtually all idle game concurrent needs.
        // Zero GC after init — nodes are reused, never allocated during play.
        _oneShotPool = new AudioStreamPlayer[OneShotPoolMaxSize];
        for (int i = 0; i < OneShotPoolMaxSize; i++)
        {
            var player = new AudioStreamPlayer();
            player.Bus = _sfxBus;
            player.VolumeDb = 0f;
            player.Finished += _oneShotFinishedHandler;
            AddChild(player);
            _oneShotPool[i] = player;
        }

        try
        {
            string configPath = !string.IsNullOrWhiteSpace(ConfigFilePath)
                ? ConfigFilePath
                : AudioConfigLoader.ResolveConfigPath();
            AudioConfigDocument config = AudioConfigLoader.Load(configPath);
            CopyMappings(_cuePaths, AudioConfigLoader.BuildAbsolutePathMap(config, config.Cues, configPath));
            CopyMappings(_musicPaths, AudioConfigLoader.BuildAbsolutePathMap(config, config.Music, configPath));
            CopyMappings(_ambientPaths, AudioConfigLoader.BuildAbsolutePathMap(config, config.Ambient, configPath));

            // Validate all cue files exist
            ValidateCueFiles(_cuePaths, "cue");
            ValidateCueFiles(_musicPaths, "music");
            ValidateCueFiles(_ambientPaths, "ambient");
        }
        catch (FileNotFoundException ex)
        {
            _logger?.Error($"Audio config file not found: {ex.Message}");
        }
        catch (IOException ex)
        {
            _logger?.Error($"Failed to read audio config file: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            _logger?.Error($"Unexpected error loading audio config: {ex.Message}");
        }
    }

    public override void _ExitTree()
    {
        if (_musicPlayer != null)
        {
            _musicPlayer.Finished -= _musicFinishedHandler;
            _musicPlayer.QueueFree();
            _musicPlayer = null;
        }

        if (_ambientPlayer != null)
        {
            _ambientPlayer.QueueFree();
            _ambientPlayer = null;
        }

        foreach (var player in _oneShotPool)
        {
            player.Finished -= _oneShotFinishedHandler;
            player.QueueFree();
        }
        _oneShotPool = Array.Empty<AudioStreamPlayer>();
    }

    private string ResolveBusName(string busName)
    {
        for (int i = 0; i < AudioServer.BusCount; i++)
        {
            if (AudioServer.GetBusName(i) == busName)
            {
                return busName;
            }
        }
        GD.PushWarning($"AudioSystem: bus '{busName}' not found — falling back to Master");
        _logger?.Warning($"Audio bus '{busName}' not found, using Master");
        return "Master";
    }

    private void ValidateCueFiles(AudioPathCollection collection, string category)
    {
        foreach (string cueId in collection.CueIds)
        {
            if (!collection.TryGetPath(cueId, out string? path) || string.IsNullOrWhiteSpace(path))
            {
                _diagnostics.Report(
                    AudioPlaybackFailureKind.CueNotRegistered,
                    category,
                    cueId,
                    "mapping has no path",
                    logEveryTime: true);
                continue;
            }

            if (!CanResolveAudioPath(path))
            {
                _diagnostics.Report(
                    AudioPlaybackFailureKind.AssetNotFound,
                    category,
                    cueId,
                    AudioPlaybackLog.AssetNotFoundDetail(path),
                    logEveryTime: true);
            }
        }
    }

    private static void CopyMappings(AudioPathCollection target, AudioPathCollection source)
    {
        target.Clear();
        foreach (string cueId in source.CueIds)
        {
            if (source.TryGetPath(cueId, out string? path) && !string.IsNullOrWhiteSpace(path))
            {
                target.Add(cueId, path);
            }
        }
    }

    public override void _Process(double delta)
    {
        float deltaSec = (float)delta;

        // Fade music player
        if (_musicPlayer != null && (_musicFadingIn || Math.Abs(_musicPlayer.VolumeDb - GetEffectiveMusicVolumeDb()) > FadeEpsilon))
        {
            float targetDb = GetEffectiveMusicVolumeDb();
            if (_musicFadingIn)
            {
                _musicPlayer.VolumeDb = Mathf.MoveToward(_musicPlayer.VolumeDb, targetDb, _musicFadeRate * deltaSec);
                if (Math.Abs(_musicPlayer.VolumeDb - targetDb) < FadeEpsilon)
                {
                    _musicFadingIn = false;
                }
            }
            else
            {
                // Fade out (target is 0)
                _musicPlayer.VolumeDb = Mathf.MoveToward(_musicPlayer.VolumeDb, targetDb, _musicFadeRate * deltaSec);
            }
        }

        // Fade ambient player
        if (_ambientPlayer != null && (_ambientFadingIn || Math.Abs(_ambientPlayer.VolumeDb - GetEffectiveAmbientVolumeDb()) > FadeEpsilon))
        {
            float targetDb = GetEffectiveAmbientVolumeDb();
            if (_ambientFadingIn)
            {
                _ambientPlayer.VolumeDb = Mathf.MoveToward(_ambientPlayer.VolumeDb, targetDb, _ambientFadeRate * deltaSec);
                if (Math.Abs(_ambientPlayer.VolumeDb - targetDb) < FadeEpsilon)
                {
                    _ambientFadingIn = false;
                }
            }
            else
            {
                // Fade out
                _ambientPlayer.VolumeDb = Mathf.MoveToward(_ambientPlayer.VolumeDb, targetDb, _ambientFadeRate * deltaSec);
            }
        }

        // Handle pending track changes after fade out
        if (_pendingMusicCueId != null && !_musicFadingIn && _musicPlayer != null && !IsPlayerFadingToward(_musicPlayer, GetEffectiveMusicVolumeDb()))
        {
            string cueId = _pendingMusicCueId;
            _pendingMusicCueId = null;
            PlayLoop(_musicPlayer, cueId, _musicPaths);
            _musicFadingIn = true;
        }

        if (_pendingAmbientCueId != null && !_ambientFadingIn && _ambientPlayer != null && !IsPlayerFadingToward(_ambientPlayer, GetEffectiveAmbientVolumeDb()))
        {
            string cueId = _pendingAmbientCueId;
            _pendingAmbientCueId = null;
            PlayLoop(_ambientPlayer, cueId, _ambientPaths);
            _ambientFadingIn = true;
        }
    }

    private bool IsPlayerFadingToward(AudioStreamPlayer player, float targetDb)
    {
        return Math.Abs(player.VolumeDb - targetDb) > FadeEpsilon;
    }

    private float GetEffectiveMusicVolumeDb()
    {
        return DefaultMusicVolume + LinearToDb(_masterVolume * _musicVolume);
    }

    private float GetEffectiveAmbientVolumeDb()
    {
        return DefaultAmbientVolume + LinearToDb(_masterVolume * _ambientVolume);
    }

    private static float LinearToDb(float linear)
    {
        if (linear <= 0f) return -80f;
        return Mathf.LinearToDb(linear);
    }

    /// <summary>
    /// Starts looping music for the given cue id. Call from a GDScript signal handler.
    /// </summary>
    /// <param name="cueId">The music track cue id from audio_config.yaml.</param>
    public void TryPlayMusic(string cueId)
    {
        TryPlayMusic(cueId, 0.5f);
    }

    private float GetCurrentStreamResourcePath(AudioStreamPlayer player)
    {
        if (player.Stream is AudioStreamOggVorbis ogg)
            return string.IsNullOrWhiteSpace(ogg.ResourcePath) ? 0f : 1f;
        if (player.Stream is AudioStreamWav wav)
            return string.IsNullOrWhiteSpace(wav.ResourcePath) ? 0f : 1f;
        return 0f;
    }

    private bool HasCurrentStreamPath(AudioStreamPlayer player)
    {
        if (player.Stream is AudioStreamOggVorbis ogg)
            return !string.IsNullOrWhiteSpace(ogg.ResourcePath);
        if (player.Stream is AudioStreamWav wav)
            return !string.IsNullOrWhiteSpace(wav.ResourcePath);
        return false;
    }

    /// <summary>
    /// Starts looping music for the given cue id with optional fade in. Call from a GDScript signal handler.
    /// </summary>
    /// <param name="cueId">The music track cue id from audio_config.yaml.</param>
    /// <param name="fadeDurationSec">Fade in duration in seconds (0 = instant).</param>
    public void TryPlayMusic(string cueId, float fadeDurationSec)
    {
        if (_musicPlayer == null)
        {
            _diagnostics.Report(
                AudioPlaybackFailureKind.PlayerNotReady,
                "music",
                cueId,
                "music AudioStreamPlayer is not initialized");
            return;
        }

        if (!string.IsNullOrWhiteSpace(cueId)
            && cueId == _activeMusicCueId
            && _musicPlayer.Playing)
        {
            return;
        }

        if (fadeDurationSec <= 0f)
        {
            PlayLoop(_musicPlayer, cueId, _musicPaths);
            return;
        }

        if (_musicPlayer.Playing && HasCurrentStreamPath(_musicPlayer))
        {
            _pendingMusicCueId = cueId;
            _musicFadeRate = 1.0f / fadeDurationSec;
            // Fade out to silence
            FadePlayerOut(_musicPlayer, fadeDurationSec);
        }
        else
        {
            PlayLoop(_musicPlayer, cueId, _musicPaths);
            _musicFadingIn = true;
            _musicFadeRate = 1.0f / fadeDurationSec;
            // Start at silence and fade in
            _musicPlayer.VolumeDb = DefaultMusicVolume + LinearToDb(_masterVolume * _musicVolume * FadeEpsilon);
        }
    }

    /// <summary>
    /// Starts looping ambient audio for the given cue id. Call from a GDScript signal handler.
    /// </summary>
    /// <param name="cueId">The ambient track cue id from audio_config.yaml.</param>
    public void TryPlayAmbient(string cueId)
    {
        TryPlayAmbient(cueId, 0.5f);
    }

    /// <summary>
    /// Starts looping ambient audio for the given cue id with optional fade in. Call from a GDScript signal handler.
    /// </summary>
    /// <param name="cueId">The ambient track cue id from audio_config.yaml.</param>
    /// <param name="fadeDurationSec">Fade in duration in seconds (0 = instant).</param>
    public void TryPlayAmbient(string cueId, float fadeDurationSec)
    {
        if (_ambientPlayer == null)
        {
            _diagnostics.Report(
                AudioPlaybackFailureKind.PlayerNotReady,
                "ambient",
                cueId,
                "ambient AudioStreamPlayer is not initialized");
            return;
        }

        if (fadeDurationSec <= 0f)
        {
            PlayLoop(_ambientPlayer, cueId, _ambientPaths);
            return;
        }

        // If something is playing, fade out first, then switch
        if (_ambientPlayer.Playing)
        {
            _pendingAmbientCueId = cueId;
            _ambientFadeRate = 1.0f / fadeDurationSec;
            FadePlayerOut(_ambientPlayer, fadeDurationSec);
        }
        else
        {
            PlayLoop(_ambientPlayer, cueId, _ambientPaths);
            _ambientFadingIn = true;
            _ambientFadeRate = 1.0f / fadeDurationSec;
            _ambientPlayer.VolumeDb = DefaultAmbientVolume + LinearToDb(_masterVolume * _ambientVolume * FadeEpsilon);
        }
    }

    /// <summary>
    /// Stops the music player with optional fade out.
    /// </summary>
    /// <param name="fadeOutSec">Fade out duration in seconds.</param>
    public void TryStopMusic(float fadeOutSec = 0.3f)
    {
        if (_musicPlayer == null) return;

        _pendingMusicCueId = null;
        _activeMusicCueId = null;
        if (fadeOutSec <= 0f)
        {
            _musicPlayer.Stop();
            _musicFadingIn = false;
            return;
        }

        FadePlayerOut(_musicPlayer, fadeOutSec);
        // Schedule stop after fade
        Callable.From(() =>
        {
            if (_musicPlayer != null)
            {
                _musicPlayer.Stop();
            }
        }).CallDeferred();
    }

    /// <summary>
    /// Stops the ambient player with optional fade out.
    /// </summary>
    /// <param name="fadeOutSec">Fade out duration in seconds.</param>
    public void TryStopAmbient(float fadeOutSec = 0.3f)
    {
        if (_ambientPlayer == null) return;

        _pendingAmbientCueId = null;
        if (fadeOutSec <= 0f)
        {
            _ambientPlayer.Stop();
            _ambientFadingIn = false;
            return;
        }

        FadePlayerOut(_ambientPlayer, fadeOutSec);
        Callable.From(() =>
        {
            if (_ambientPlayer != null)
            {
                _ambientPlayer.Stop();
            }
        }).CallDeferred();
    }

    private void FadePlayerOut(AudioStreamPlayer player, float durationSec)
    {
        if (durationSec <= 0f) return;

        float fadeRate = 1.0f / durationSec;
        float currentDb = player.VolumeDb;
        // We rely on _Process to fade toward silence
        // Store the fade rate for use during fade
        if (player == _musicPlayer)
        {
            _musicFadeRate = fadeRate;
        }
        else if (player == _ambientPlayer)
        {
            _ambientFadeRate = fadeRate;
        }
    }

    /// <summary>
    /// Sets the master volume (0–1).
    /// </summary>
    /// <param name="vol">Volume level from 0.0 to 1.0.</param>
    public void SetMasterVolume(float vol)
    {
        _masterVolume = Mathf.Clamp(vol, 0f, 1f);
        UpdateAllVolumes();
    }

    /// <summary>
    /// Sets the music volume (0–1).
    /// </summary>
    /// <param name="vol">Volume level from 0.0 to 1.0.</param>
    public void SetMusicVolume(float vol)
    {
        _musicVolume = Mathf.Clamp(vol, 0f, 1f);
        UpdateMusicVolume();
    }

    /// <summary>
    /// Sets the ambient volume (0–1).
    /// </summary>
    /// <param name="vol">Volume level from 0.0 to 1.0.</param>
    public void SetAmbientVolume(float vol)
    {
        _ambientVolume = Mathf.Clamp(vol, 0f, 1f);
        UpdateAmbientVolume();
    }

    private void UpdateAllVolumes()
    {
        UpdateMusicVolume();
        UpdateAmbientVolume();
    }

    private void UpdateMusicVolume()
    {
        if (_musicPlayer != null)
        {
            _musicPlayer.VolumeDb = GetEffectiveMusicVolumeDb();
        }
    }

    private void UpdateAmbientVolume()
    {
        if (_ambientPlayer != null && !_ambientDucking)
        {
            _ambientPlayer.VolumeDb = GetEffectiveAmbientVolumeDb();
        }
    }

    /// <summary>
    /// Plays a one-shot audio cue. Call from a GDScript signal handler.
    /// </summary>
    /// <param name="cueId">The one-shot cue id from audio_config.yaml.</param>
    public void TryPlayOneShot(string cueId)
    {
        PlayCue(cueId, _cuePaths);
    }

    private void PlayLoop(AudioStreamPlayer player, string cueId, AudioPathCollection mapping)
    {
        string category = player == _musicPlayer ? "music" : "ambient";
        player.Stop();
        player.Stream = null;

        if (!TryResolveCuePath(cueId, mapping, category, out string? path))
        {
            if (player == _musicPlayer)
                _activeMusicCueId = null;
            return;
        }

        AudioStream? stream = LoadAudioStream(cueId, category, path);
        if (stream == null)
        {
            if (player == _musicPlayer)
                _activeMusicCueId = null;
            return;
        }

        if (player == _musicPlayer)
        {
            _activeMusicCueId = cueId;
            if (stream is AudioStreamOggVorbis ogg)
                ogg.Loop = true;
            else if (stream is AudioStreamWav wav)
                wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        }

        player.Stream = stream;
        if (player == _musicPlayer)
            player.VolumeDb = GetEffectiveMusicVolumeDb();

        if (!TryStartPlayback(player, cueId, category))
        {
            if (player == _musicPlayer)
                _activeMusicCueId = null;
        }
    }

    private void OnMusicFinished()
    {
        if (_musicPlayer == null || string.IsNullOrWhiteSpace(_activeMusicCueId))
            return;

        if (_musicPlayer.Stream != null)
            _musicPlayer.Play();
    }

    private void PlayCue(string cueId, AudioPathCollection mapping)
    {
        const string category = "one-shot";
        if (!TryResolveCuePath(cueId, mapping, category, out string? path))
            return;

        AudioStream? stream = LoadAudioStream(cueId, category, path);
        if (stream == null)
            return;

        // ── Approach B: Ring-buffer pool ────────────────────────────────────────
        // Round-robin scan for idle slot. Zero GC after init.
        // If pool is full (all slots busy), the sound is silently dropped —
        // correct behavior: never block game thread for audio.
        AudioStreamPlayer? player = null;
        int searchStart = _poolCursor;
        for (int i = 0; i < OneShotPoolMaxSize; i++)
        {
            int idx = (searchStart + i) % OneShotPoolMaxSize;
            if (!_oneShotPool[idx].Playing)
            {
                player = _oneShotPool[idx];
                _poolCursor = (idx + 1) % OneShotPoolMaxSize;
                break;
            }
        }

        if (player == null)
        {
            _diagnostics.Report(
                AudioPlaybackFailureKind.PoolExhausted,
                category,
                cueId,
                AudioPlaybackLog.PoolExhaustedDetail(OneShotPoolMaxSize));
            return;
        }

        if ((_ambientPlayer?.Playing ?? false) && _playingOneShotCount == 0)
            BeginAmbientDuck();

        _playingOneShotCount++;
        player.Stream = stream;
        if (!TryStartPlayback(player, cueId, category))
            _playingOneShotCount = Math.Max(0, _playingOneShotCount - 1);
    }

    private bool TryResolveCuePath(
        string cueId,
        AudioPathCollection mapping,
        string category,
        out string path)
    {
        path = string.Empty;
        if (string.IsNullOrWhiteSpace(cueId))
        {
            _diagnostics.Report(AudioPlaybackFailureKind.InvalidCueId, category, cueId, string.Empty);
            return false;
        }

        if (!mapping.TryGetPath(cueId, out string? resolved) || string.IsNullOrWhiteSpace(resolved))
        {
            _diagnostics.Report(AudioPlaybackFailureKind.CueNotRegistered, category, cueId, string.Empty);
            return false;
        }

        path = resolved;

        if (!CanResolveAudioPath(path))
        {
            _diagnostics.Report(
                AudioPlaybackFailureKind.AssetNotFound,
                category,
                cueId,
                AudioPlaybackLog.AssetNotFoundDetail(path));
            return false;
        }

        return true;
    }

    private bool TryStartPlayback(AudioStreamPlayer player, string cueId, string category)
    {
        if (player.Stream == null)
        {
            _diagnostics.Report(
                AudioPlaybackFailureKind.PlayRejected,
                category,
                cueId,
                "AudioStreamPlayer has no stream assigned");
            return false;
        }

        player.Play();
        if (!player.Playing)
        {
            _diagnostics.Report(
                AudioPlaybackFailureKind.PlayRejected,
                category,
                cueId,
                "AudioStreamPlayer.Play() did not start playback");
            return false;
        }

        return true;
    }

    private void OnOneShotPoolPlayerFinished()
    {
        if (_playingOneShotCount <= 0)
            return;

        _playingOneShotCount--;
        if (_playingOneShotCount == 0)
            EndAmbientDuck();
    }

    private void BeginAmbientDuck()
    {
        if (_ambientDucking)
            return;

        _ambientDucking = true;
        _ambientVolumeBeforeDuck = _ambientVolume;
        _ambientVolume *= 0.5f;
        if (_ambientPlayer != null)
            _ambientPlayer.VolumeDb = GetEffectiveAmbientVolumeDb();
    }

    private void EndAmbientDuck()
    {
        if (!_ambientDucking)
            return;

        _ambientDucking = false;
        _ambientVolume = _ambientVolumeBeforeDuck;
        if (_ambientPlayer != null)
            _ambientPlayer.VolumeDb = GetEffectiveAmbientVolumeDb();
    }

    private static bool CanResolveAudioPath(string path)
    {
        return IsGodotResourcePath(path)
            ? ResourceLoader.Exists(path)
            : File.Exists(path);
    }

    private static bool IsGodotResourcePath(string path)
    {
        return path.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase);
    }

    private AudioStream? LoadAudioStream(string cueId, string category, string path)
    {
        try
        {
            if (IsGodotResourcePath(path))
            {
                AudioStream? resourceStream = ResourceLoader.Load<AudioStream>(path);
                if (resourceStream != null)
                    return resourceStream;

                _diagnostics.Report(
                    AudioPlaybackFailureKind.LoadFailed,
                    category,
                    cueId,
                    AudioPlaybackLog.LoadReturnedNullDetail(path));
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
                _diagnostics.Report(
                    AudioPlaybackFailureKind.LoadFailed,
                    category,
                    cueId,
                    "external wav loading is unavailable in this Godot runtime");
                return null;
            }

            AudioStreamWav? stream = loadFromFileMethod.Invoke(obj: null, parameters: [path]) as AudioStreamWav;
            if (stream != null)
                return stream;

            _diagnostics.Report(
                AudioPlaybackFailureKind.LoadFailed,
                category,
                cueId,
                AudioPlaybackLog.LoadReturnedNullDetail(path));
            return null;
        }
        catch (FileNotFoundException)
        {
            _diagnostics.Report(
                AudioPlaybackFailureKind.AssetNotFound,
                category,
                cueId,
                AudioPlaybackLog.AssetNotFoundDetail(path));
            return null;
        }
        catch (IOException ex)
        {
            _diagnostics.Report(
                AudioPlaybackFailureKind.LoadFailed,
                category,
                cueId,
                $"failed to read file '{path}': {ex.Message}");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _diagnostics.Report(
                AudioPlaybackFailureKind.LoadFailed,
                category,
                cueId,
                $"unexpected error loading '{path}': {ex.Message}");
            return null;
        }
    }
}
