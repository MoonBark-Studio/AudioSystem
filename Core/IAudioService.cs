namespace MoonBark.AudioSystem.Core;

/// <summary>
/// Core audio service interface — pure C#, no engine dependencies.
/// Implemented by the Godot-side adapter (GodotAudioManager).
/// </summary>
public interface IAudioService
{
    /// <summary>Plays a one-shot sound effect by cue ID.</summary>
    void PlayOneShot(string cueId);

    /// <summary>Starts looping background music by cue ID with optional fade-in.</summary>
    void PlayMusic(string cueId, float fadeInDurationSec = 0f);

    /// <summary>Starts looping ambient audio by cue ID with optional fade-in.</summary>
    void PlayAmbient(string cueId, float fadeInDurationSec = 0f);

    /// <summary>Stops the music player with optional fade-out.</summary>
    void StopMusic(float fadeOutSec = 0.3f);

    /// <summary>Stops the ambient player with optional fade-out.</summary>
    void StopAmbient(float fadeOutSec = 0.3f);

    /// <summary>Sets the master volume (0.0 to 1.0).</summary>
    void SetMasterVolume(float volume);

    /// <summary>Sets the music volume (0.0 to 1.0).</summary>
    void SetMusicVolume(float volume);

    /// <summary>Sets the sound effects volume (0.0 to 1.0).</summary>
    void SetSoundEffectsVolume(float volume);
}
