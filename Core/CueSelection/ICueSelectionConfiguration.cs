using Friflo.Engine.ECS;

namespace AudioSystem.Core.CueSelection;

/// <summary>
/// Normalized read-only state that AudioSystem consumes when selecting cues.
/// The integration layer adapts game or plugin state into this contract.
/// </summary>
public interface IAudioCueStateReader
{
    bool IsNight { get; }
    string EventKind { get; }
    int CompletedTasks { get; }
    float TotalTimeHours { get; }
}

/// <summary>
/// Configuration contract for game-specific audio cue selection logic.
/// Implementations live in the game layer and inject the game-specific rules
/// for which music/ambient/cue to play given the current normalized state.
/// </summary>
public interface ICueSelectionConfiguration
{
    /// <summary>Maximum number of pending cues the system will buffer.</summary>
    int MaxPendingCues { get; }

    /// <summary>Selects an ambient track ID based on time of day.</summary>
    string SelectAmbientTrack(bool isNight);

    /// <summary>Selects a music track ID for the given event kind, or null if none.</summary>
    string? SelectMusicTrack(string eventKind);

    /// <summary>Selects a one-shot cue for event-kind transitions, or null if none.</summary>
    string? SelectEventChangeCue(string eventKind);

    /// <summary>Selects a one-shot cue for task completion, or null if none.</summary>
    string? SelectTaskCompleteCue();
}
