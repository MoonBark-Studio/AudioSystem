namespace AudioSystem.Core.Configuration;

/// <summary>
/// Configuration for audio cue selection logic.
/// Defines how world state maps to audio cues.
/// </summary>
public interface ICueSelectionConfiguration
{
    /// <summary>
    /// Gets the world state key for time of day (night vs day).
    /// </summary>
    string TimeOfDayKey { get; }

    /// <summary>
    /// Gets the world state key for the current event kind.
    /// </summary>
    string EventKindKey { get; }

    /// <summary>
    /// Gets the world state key for completed tasks count.
    /// </summary>
    string CompletedTasksKey { get; }

    /// <summary>
    /// Gets the world state key for total time (in hours).
    /// </summary>
    string TotalTimeKey { get; }

    /// <summary>
    /// Determines if the current time is night based on world state.
    /// </summary>
    /// <param name="worldState">The world state to check.</param>
    /// <returns>True if it's night, false otherwise.</returns>
    bool IsNight(global::WorldState.WorldState worldState);

    /// <summary>
    /// Gets the current event kind from world state.
    /// </summary>
    /// <param name="worldState">The world state to check.</param>
    /// <returns>The current event kind, or empty string if not available.</returns>
    string GetEventKind(global::WorldState.WorldState worldState);

    /// <summary>
    /// Gets the number of completed tasks from world state.
    /// </summary>
    /// <param name="worldState">The world state to check.</param>
    /// <returns>The number of completed tasks.</returns>
    int GetCompletedTasks(global::WorldState.WorldState worldState);

    /// <summary>
    /// Gets the total time in hours from world state.
    /// </summary>
    /// <param name="worldState">The world state to check.</param>
    /// <returns>Total time in hours.</returns>
    float GetTotalTimeHours(global::WorldState.WorldState worldState);

    /// <summary>
    /// Selects the ambient track based on time of day.
    /// </summary>
    /// <param name="isNight">Whether it's night time.</param>
    /// <returns>The ambient track ID.</returns>
    string SelectAmbientTrack(bool isNight);

    /// <summary>
    /// Selects the music track based on event kind.
    /// </summary>
    /// <param name="eventKind">The current event kind.</param>
    /// <returns>The music track ID.</returns>
    string SelectMusicTrack(string eventKind);

    /// <summary>
    /// Determines if a cue should be enqueued for an event kind change.
    /// </summary>
    /// <param name="eventKind">The new event kind.</param>
    /// <returns>The cue ID to enqueue, or null if no cue should be enqueued.</returns>
    string? SelectEventChangeCue(string eventKind);

    /// <summary>
    /// Gets the cue to enqueue when tasks are completed.
    /// </summary>
    /// <returns>The cue ID, or null if no cue should be enqueued.</returns>
    string? SelectTaskCompleteCue();

    /// <summary>
    /// Gets the maximum number of pending cues to keep in the queue.
    /// </summary>
    int MaxPendingCues { get; }
}
