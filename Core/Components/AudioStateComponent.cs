namespace AudioSystem.Core.Components;

using Friflo.Engine.ECS;
using System.Collections.Generic;

/// <summary>
/// Stores the current audio playback state and pending cues for a runtime.
/// </summary>
public struct AudioStateComponent : IComponent
{
    public string CurrentAmbientTrack;
    public string CurrentMusicTrack;
    public Queue<string> PendingCues;
    public string LastCue;
    public int CueVersion;
    public string LastObservedEventKind;

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
