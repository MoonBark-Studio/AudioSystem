namespace MoonBark.AudioSystem.Core;

/// <summary>
/// Audio playback control contract — pure C#, no engine dependencies.
/// </summary>
public interface IAudioPlayback
{
    void PlayOneShot(string cueId);
    void PlayMusic(string cueId, float fadeInDurationSec = 0f);
    void PlayAmbient(string cueId, float fadeInDurationSec = 0f);
    void StopMusic(float fadeOutSec = 0.3f);
    void StopAmbient(float fadeOutSec = 0.3f);
}

/// <summary>
/// Audio volume control contract — pure C#, no engine dependencies.
/// </summary>
public interface IAudioVolumeControl
{
    void SetMasterVolume(float volume);
    void SetMusicVolume(float volume);
    void SetSoundEffectsVolume(float volume);
}

/// <summary>
/// Full audio service interface — pure C#, no engine dependencies.
/// Implemented by the Godot-side adapter (GodotAudioManager).
/// </summary>
public interface IAudioService : IAudioPlayback, IAudioVolumeControl
{
}

/// <summary>
/// Playlist control contract — pure C#, no engine dependencies.
/// Enables multi-track playback with optional delay between tracks and shuffle support.
/// </summary>
public interface IAudioPlaylistControl
{
    /// <summary>
    /// Activates a playlist by its registered ID and begins playback of the first track.
    /// </summary>
    /// <param name="playlistId">The playlist identifier registered in the audio config.</param>
    /// <param name="fadeDurationSec">Fade duration when transitioning to the first track.</param>
    void SetActivePlaylist(string playlistId, float fadeDurationSec = 0.5f);

    /// <summary>
    /// Clears the active playlist and stops all music playback.
    /// </summary>
    void ClearActivePlaylist();

    /// <summary>
    /// Enables or disables shuffle mode for the active playlist.
    /// </summary>
    /// <param name="enabled">True to shuffle; false for sequential playback.</param>
    void SetShuffleEnabled(bool enabled);

    /// <summary>
    /// Sets the silence gap between consecutive tracks in the active playlist.
    /// </summary>
    /// <param name="seconds">Delay in seconds. Set to 0 to disable delay.</param>
    void SetInterTrackDelay(float seconds);

    /// <summary>
    /// Skips to the next track in the playlist.
    /// </summary>
    void SkipToNext();

    /// <summary>
    /// Returns to the previous track in the playlist.
    /// </summary>
    void SkipToPrevious();
}
