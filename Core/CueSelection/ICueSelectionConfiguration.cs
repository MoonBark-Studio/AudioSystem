using Friflo.Engine.ECS;
using MoonBark.WorldState;

namespace AudioSystem.Core.CueSelection;

/// <summary>
/// Configuration contract for game-specific audio cue selection logic.
/// Implementations live in the game layer (e.g., Thistletide's
/// <c>GameCueSelectionConfiguration</c>) and inject the game-specific rules
/// for which music/ambient/cue to play given the current world state.
/// </summary>
public interface ICueSelectionConfiguration
{
    /// <summary>WorldState key for the current time of day.</summary>
    string TimeOfDayKey { get; }

    /// <summary>WorldState key for the current event kind.</summary>
    string EventKindKey { get; }

    /// <summary>WorldState key for the completed task count.</summary>
    string CompletedTasksKey { get; }

    /// <summary>WorldState key for the total elapsed time.</summary>
    string TotalTimeKey { get; }

    /// <summary>Maximum number of pending cues the system will buffer.</summary>
    int MaxPendingCues { get; }

    /// <summary>Returns true when the current time of day is nighttime.</summary>
    bool IsNight(WorldState worldState);

    /// <summary>Gets the current event kind from world state.</summary>
    string GetEventKind(WorldState worldState);

    /// <summary>Gets the current completed task count from world state.</summary>
    int GetCompletedTasks(WorldState worldState);

    /// <summary>Gets the total elapsed game time in hours.</summary>
    float GetTotalTimeHours(WorldState worldState);

    /// <summary>Selects an ambient track ID based on time of day.</summary>
    string SelectAmbientTrack(bool isNight);

    /// <summary>Selects a music track ID for the given event kind, or null if none.</summary>
    string? SelectMusicTrack(string eventKind);

    /// <summary>Selects a one-shot cue for event-kind transitions, or null if none.</summary>
    string? SelectEventChangeCue(string eventKind);

    /// <summary>Selects a one-shot cue for task completion, or null if none.</summary>
    string? SelectTaskCompleteCue();
}
