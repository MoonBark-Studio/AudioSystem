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
