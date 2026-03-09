namespace AudioSystem.Core.Components;

using Friflo.Engine.ECS;
using System.Collections.Generic;

/// <summary>
/// Stores the current audio playback state and pending cues for a runtime.
/// </summary>
public struct AudioStateComponent : IComponent
{
    /// <summary>
    /// The current ambient track ID.
    /// </summary>
    public string CurrentAmbientTrack;

    /// <summary>
    /// The current music track ID.
    /// </summary>
    public string CurrentMusicTrack;

    /// <summary>
    /// Queue of pending audio cues to play.
    /// </summary>
    public Queue<string> PendingCues;

    /// <summary>
    /// The last cue that was played.
    /// </summary>
    public string LastCue;

    /// <summary>
    /// Version counter that increments when cues are enqueued.
    /// </summary>
    public int CueVersion;

    /// <summary>
    /// The last observed event kind from world state.
    /// </summary>
    public string LastObservedEventKind;

    /// <summary>
    /// Creates a new AudioStateComponent.
    /// </summary>
    /// <param name="currentAmbientTrack">The initial ambient track ID.</param>
    /// <param name="currentMusicTrack">The initial music track ID.</param>
    /// <returns>A new AudioStateComponent instance.</returns>
    public static AudioStateComponent Create(string currentAmbientTrack = "ambient_default", string currentMusicTrack = "music_default")
    {
        return new AudioStateComponent
        {
            CurrentAmbientTrack = currentAmbientTrack,
            CurrentMusicTrack = currentMusicTrack,
            PendingCues = new Queue<string>(),
            LastCue = string.Empty,
            CueVersion = 0,
            LastObservedEventKind = string.Empty
        };
    }
}
