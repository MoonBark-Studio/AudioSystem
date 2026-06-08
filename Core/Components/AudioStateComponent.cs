using Friflo.Engine.ECS;

namespace AudioSystem.Core.Components;

/// <summary>
/// ECS component that holds the current audio playback state for an entity.
/// Tracks the active music/ambient tracks, the last played cue, and a
/// monotonically increasing cue version for change detection.
/// </summary>
public struct AudioStateComponent : IComponent
{
    /// <summary>ID of the currently playing music track.</summary>
    public string CurrentMusicTrack;

    /// <summary>ID of the currently playing ambient track.</summary>
    public string CurrentAmbientTrack;

    /// <summary>ID of the most recently played one-shot cue.</summary>
    public string LastCue;

    /// <summary>Monotonic version counter incremented on each cue change.</summary>
    public int CueVersion;

    /// <summary>
    /// Creates a new audio state component with the given initial tracks.
    /// CueVersion starts at 0 and LastCue is empty.
    /// </summary>
    public static AudioStateComponent Create(string musicTrack, string ambientTrack)
    {
        return new AudioStateComponent
        {
            CurrentMusicTrack = musicTrack,
            CurrentAmbientTrack = ambientTrack,
            LastCue = string.Empty,
            CueVersion = 0
        };
    }
}
