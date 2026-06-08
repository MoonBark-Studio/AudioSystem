using MoonBark.AudioSystem.Core;
using MoonBark.AudioSystem.Core.Configuration;
using MoonBark.AudioSystem.Core.Diagnostics;
using MoonBark.Framework.Logging;
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MoonBark.AudioSystem.Godot.Adapters.Systems;

public partial class GodotAudioManager : Node, IAudioService
{
    private const string MusicPlayerNodeName = "MusicPlayer";
    private const string AmbientPlayerNodeName = "AmbientPlayer";
    private const float DefaultMusicVolume = 0.0f;
    private const float DefaultAmbientVolume = -10.0f;
    internal const int OneShotPoolMaxSizeConstant = 32;
    private const int OneShotPoolMaxSize = OneShotPoolMaxSizeConstant;
    private const float FadeEpsilon = 0.001f;

    private readonly AudioPathCollection _cuePaths = new();
    private readonly AudioPathCollection _musicPaths = new();
    private readonly AudioPathCollection _ambientPaths = new();
    private AudioStreamPlayer? _musicPlayer;
    private AudioStreamPlayer? _ambientPlayer;

    private AudioStreamPlayer[] _oneShotPool = Array.Empty<AudioStreamPlayer>();
    private int _poolCursor;

    private float _masterVolume = 1.0f;
    private float _musicVolume = 1.0f;
    private float _ambientVolume = 1.0f;

    private float _musicFadeRate;
    private float _ambientFadeRate;
    private bool _musicFadingIn;
    private bool _ambientFadingIn;
    private string? _pendingMusicCueId;
    private string? _pendingAmbientCueId;

    private float _ambientVolumeBeforeDuck = 1.0f;
    private bool _ambientDucking = false;
    private int _playingOneShotCount;

    private string? _activeMusicCueId;

    private PlaylistConfig? _activePlaylist;
    private List<string> _playlistOrder = new();
    private int _playlistIndex;
    private bool _shuffleEnabled;
    private float _interTrackDelaySec;
    private float _delayRemaining;
    private bool _inPlaylistMode;
    private bool _playlistFadingOut;

    private readonly GodotAudioPlaybackDiagnostics _diagnostics;
    private readonly ILogger? _logger;

    private Action? _musicFinishedHandler;
    private Action? _oneShotFinishedHandler;

    private string _musicBus = "Music";
    private string _sfxBus = "SFX";

    public string? ConfigFilePath { get; set; }

    public GodotAudioManager() : this(null) { }

    public GodotAudioManager(ILogger? logger)
    {
        _logger = logger;
        _diagnostics = new GodotAudioPlaybackDiagnostics(logger);
    }

    public override void _Ready()
    {
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

        _oneShotPool = new AudioStreamPlayer[OneShotPoolMaxSize];
        for (int i = 0; i < OneShotPoolMaxSize; i++)
        {
            var player = new AudioStreamPlayer();
            player.Bus = _sfxBus;
            player.VolumeDb = GetEffectiveSoundEffectsVolumeDb();
            player.Finished += _oneShotFinishedHandler;
            AddChild(player);
            _oneShotPool[i] = player;
        }

        UpdateSoundEffectsVolume();

        try
        {
            string configPath = !string.IsNullOrWhiteSpace(ConfigFilePath)
                ? ConfigFilePath
                : AudioConfigLoader.ResolveConfigPath();
            AudioConfigDocument config = AudioConfigLoader.Load(configPath);
            CopyMappings(_cuePaths, AudioConfigLoader.BuildAbsolutePathMap(config, config.Cues, configPath));
            CopyMappings(_musicPaths, AudioConfigLoader.BuildAbsolutePathMap(config, config.Music, configPath));
            CopyMappings(_ambientPaths, AudioConfigLoader.BuildAbsolutePathMap(config, config.Ambient, configPath));

            ValidateCueFiles(_cuePaths, "cue");
            ValidateCueFiles(_musicPaths, "music");
            ValidateCueFiles(_ambientPaths, "ambient");

            foreach (var kvp in config.Playlists)
            {
                kvp.Value.Id = kvp.Key;
            }
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

        if (_inPlaylistMode && _delayRemaining > 0f)
        {
            _delayRemaining -= deltaSec;
            if (_delayRemaining <= 0f)
            {
                _delayRemaining = 0f;
                AdvancePlaylist();
            }
        }

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
                _musicPlayer.VolumeDb = Mathf.MoveToward(_musicPlayer.VolumeDb, targetDb, _musicFadeRate * deltaSec);
            }
        }

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
                _ambientPlayer.VolumeDb = Mathf.MoveToward(_ambientPlayer.VolumeDb, targetDb, _ambientFadeRate * deltaSec);
            }
        }

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

    private float GetEffectiveSoundEffectsVolumeDb()
    {
        return LinearToDb(_masterVolume * _ambientVolume);
    }

    private static float LinearToDb(float linear)
    {
        if (linear <= 0f) return -80f;
        return Mathf.LinearToDb(linear);
    }

    public void TryPlayMusic(string cueId)
    {
        TryPlayMusic(cueId, 0.5f);
    }

    public void PlayMusic(string cueId, float fadeInDurationSec = 0f)
    {
        TryPlayMusic(cueId, fadeInDurationSec);
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
            FadePlayerOut(_musicPlayer, fadeDurationSec);
        }
        else
        {
            PlayLoop(_musicPlayer, cueId, _musicPaths);
            _musicFadingIn = true;
            _musicFadeRate = 1.0f / fadeDurationSec;
            _musicPlayer.VolumeDb = DefaultMusicVolume + LinearToDb(_masterVolume * _musicVolume * FadeEpsilon);
        }
    }

    public void TryPlayAmbient(string cueId)
    {
        TryPlayAmbient(cueId, 0.5f);
    }

    public void PlayAmbient(string cueId, float fadeInDurationSec = 0f)
    {
        TryPlayAmbient(cueId, fadeInDurationSec);
    }

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

    public void TryStopMusic(float fadeOutSec = 0.3f)
    {
        if (_musicPlayer == null) return;

        _pendingMusicCueId = null;
        _activeMusicCueId = null;
        _inPlaylistMode = false;
        _activePlaylist = null;
        if (fadeOutSec <= 0f)
        {
            _musicPlayer.Stop();
            _musicFadingIn = false;
            return;
        }

        FadePlayerOut(_musicPlayer, fadeOutSec);
        Callable.From(() =>
        {
            if (_musicPlayer != null)
            {
                _musicPlayer.Stop();
            }
        }).CallDeferred();
    }

    public void StopMusic(float fadeOutSec = 0.3f)
    {
        TryStopMusic(fadeOutSec);
    }

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

    public void StopAmbient(float fadeOutSec = 0.3f)
    {
        TryStopAmbient(fadeOutSec);
    }

    private void FadePlayerOut(AudioStreamPlayer player, float durationSec)
    {
        if (durationSec <= 0f) return;

        float fadeRate = 1.0f / durationSec;
        if (player == _musicPlayer)
        {
            _musicFadeRate = fadeRate;
        }
        else if (player == _ambientPlayer)
        {
            _ambientFadeRate = fadeRate;
        }
    }

    public void SetMasterVolume(float vol)
    {
        _masterVolume = Mathf.Clamp(vol, 0f, 1f);
        UpdateAllVolumes();
    }

    public void SetMusicVolume(float vol)
    {
        _musicVolume = Mathf.Clamp(vol, 0f, 1f);
        UpdateMusicVolume();
    }

    public void SetSoundEffectsVolume(float vol)
    {
        _ambientVolume = Mathf.Clamp(vol, 0f, 1f);
        UpdateSoundEffectsVolume();
        UpdateAmbientVolume();
    }

    public void SetAmbientVolume(float vol)
    {
        SetSoundEffectsVolume(vol);
    }

    public void PlayOneShot(string cueId)
    {
        TryPlayOneShot(cueId);
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

    private void UpdateSoundEffectsVolume()
    {
        float volumeDb = GetEffectiveSoundEffectsVolumeDb();
        foreach (AudioStreamPlayer player in _oneShotPool)
        {
            player.VolumeDb = volumeDb;
        }
    }

    public void TryPlayOneShot(string cueId)
    {
        PlayCue(cueId, _cuePaths);
    }

    public void SetActivePlaylist(string playlistId, float fadeDurationSec = 0.5f)
    {
        var configPath = !string.IsNullOrWhiteSpace(ConfigFilePath)
            ? ConfigFilePath
            : AudioConfigLoader.ResolveConfigPath();
        AudioConfigDocument config = AudioConfigLoader.Load(configPath);

        if (!config.Playlists.TryGetValue(playlistId, out var playlist))
        {
            _logger?.Warning($"Playlist '{playlistId}' not found in audio config");
            return;
        }

        if (playlist.TrackIds == null || playlist.TrackIds.Count == 0)
        {
            _logger?.Warning($"Playlist '{playlistId}' has no tracks");
            return;
        }

        _activePlaylist = playlist;
        _shuffleEnabled = playlist.Shuffle;
        _interTrackDelaySec = playlist.InterTrackDelaySec;
        _playlistIndex = 0;

        var validTracks = playlist.TrackIds.Where(t => _musicPaths.ContainsCue(t)).ToList();
        if (validTracks.Count == 0)
        {
            _logger?.Warning($"Playlist '{playlistId}' has no valid tracks");
            _activePlaylist = null;
            return;
        }

        _playlistOrder = _shuffleEnabled ? ShuffleList(validTracks) : validTracks;
        _inPlaylistMode = true;

        string firstTrack = _playlistOrder[_playlistIndex];
        TryPlayMusic(firstTrack, fadeDurationSec);
    }

    public void ClearActivePlaylist()
    {
        _inPlaylistMode = false;
        _activePlaylist = null;
        _playlistOrder.Clear();
        _playlistIndex = 0;
        _delayRemaining = 0f;
        TryStopMusic(0.3f);
    }

    public void SetShuffleEnabled(bool enabled)
    {
        _shuffleEnabled = enabled;
        if (_activePlaylist != null && _playlistOrder.Count > 0)
        {
            string currentTrack = _playlistOrder[_playlistIndex];
            _playlistOrder = _shuffleEnabled ? ShuffleList(_playlistOrder) : new List<string>(_activePlaylist.TrackIds.Where(t => _musicPaths.ContainsCue(t)));
            _playlistIndex = _playlistOrder.IndexOf(currentTrack);
            if (_playlistIndex < 0) _playlistIndex = 0;
        }
    }

    public void SetInterTrackDelay(float seconds)
    {
        _interTrackDelaySec = Mathf.Max(0f, seconds);
    }

    public void SkipToNext()
    {
        if (!_inPlaylistMode || _activePlaylist == null || _playlistOrder.Count == 0) return;

        _playlistIndex++;
        if (_playlistIndex >= _playlistOrder.Count)
        {
            if (_activePlaylist.Loop)
            {
                _playlistIndex = 0;
                if (_shuffleEnabled)
                {
                    _playlistOrder = ShuffleList(_playlistOrder);
                }
            }
            else
            {
                _playlistIndex = _playlistOrder.Count - 1;
                _inPlaylistMode = false;
                return;
            }
        }

        string nextTrack = _playlistOrder[_playlistIndex];
        TryPlayMusic(nextTrack, 0.3f);
    }

    public void SkipToPrevious()
    {
        if (!_inPlaylistMode || _activePlaylist == null || _playlistOrder.Count == 0) return;

        _playlistIndex--;
        if (_playlistIndex < 0)
        {
            _playlistIndex = _activePlaylist.Loop ? _playlistOrder.Count - 1 : 0;
        }

        string prevTrack = _playlistOrder[_playlistIndex];
        TryPlayMusic(prevTrack, 0.3f);
    }

    private List<string> ShuffleList(List<string> input)
    {
        var list = new List<string>(input);
        int n = list.Count;
        var rng = new Random();
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
        return list;
    }

    private void AdvancePlaylist()
    {
        if (_activePlaylist == null || _playlistOrder.Count == 0) return;

        _playlistIndex++;
        if (_playlistIndex >= _playlistOrder.Count)
        {
            if (_activePlaylist.Loop)
            {
                _playlistIndex = 0;
                if (_shuffleEnabled)
                {
                    _playlistOrder = ShuffleList(_playlistOrder);
                }
            }
            else
            {
                _inPlaylistMode = false;
                _activePlaylist = null;
                TryStopMusic(0.5f);
                return;
            }
        }

        string nextTrack = _playlistOrder[_playlistIndex];
        TryPlayMusic(nextTrack, 0.3f);
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

        if (_inPlaylistMode)
        {
            if (_interTrackDelaySec > 0f)
            {
                _delayRemaining = _interTrackDelaySec;
            }
            else
            {
                AdvancePlaylist();
            }
            return;
        }

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
        player.VolumeDb = GetEffectiveSoundEffectsVolumeDb();
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
