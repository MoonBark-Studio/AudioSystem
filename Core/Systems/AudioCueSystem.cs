namespace AudioSystem.Core.Systems;

using AudioSystem.Core.Components;
using AudioSystem.Core.Configuration;
using Friflo.Engine.ECS;

/// <summary>
/// Generic audio cue selection system that uses configuration to map world state to audio cues.
/// </summary>
public sealed class AudioCueSystem
{
    private readonly global::WorldState.WorldState _worldState;
    private readonly ICueSelectionConfiguration _configuration;

    /// <summary>
    /// Creates a new AudioCueSystem with the specified configuration.
    /// </summary>
    /// <param name="worldState">The world state to read from.</param>
    /// <param name="configuration">The configuration for cue selection logic.</param>
    public AudioCueSystem(global::WorldState.WorldState worldState, ICueSelectionConfiguration configuration)
    {
        _worldState = worldState;
        _configuration = configuration;
    }

    /// <summary>
    /// Gets the system priority. Lower numbers execute first.
    /// </summary>
    public int Priority => 12;

    /// <summary>
    /// Updates audio state for all entities with AudioStateComponent.
    /// </summary>
    /// <param name="world">The entity store to process.</param>
    /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
    public void Update(EntityStore world, float deltaTime)
    {
        bool isNight = _configuration.IsNight(_worldState);
        string eventKind = _configuration.GetEventKind(_worldState);
        int completedTasks = _configuration.GetCompletedTasks(_worldState);
        float currentTime = _configuration.GetTotalTimeHours(_worldState) * 3600.0f; // Convert hours to seconds

        // Query for entities with AudioStateComponent
        var audioQuery = world.Query<AudioStateComponent>();
        foreach (Entity entity in audioQuery.Entities)
        {
            ref AudioStateComponent audio = ref entity.GetComponent<AudioStateComponent>();

            // Update ambient track immediately (no cooldown)
            audio.CurrentAmbientTrack = _configuration.SelectAmbientTrack(isNight);

            // Check if this entity has a music cooldown component
            bool hasCooldown = entity.HasComponent<MusicCooldownComponent>();

            // Determine desired music track
            string desiredMusicTrack = _configuration.SelectMusicTrack(eventKind);

            if (hasCooldown)
            {
                // Use cooldown logic
                ref MusicCooldownComponent cooldown = ref entity.GetComponent<MusicCooldownComponent>();
                bool canChangeMusic = (currentTime - cooldown.LastMusicChangeTime) >= cooldown.MusicChangeCooldown;

                // Only change music if cooldown has passed and track is different
                if (canChangeMusic && audio.CurrentMusicTrack != desiredMusicTrack)
                {
                    audio.CurrentMusicTrack = desiredMusicTrack;
                    cooldown.LastMusicChangeTime = currentTime;
                }
                // During cooldown, keep current music track (don't update CurrentMusicTrack)
            }
            else
            {
                // No cooldown - change immediately
                audio.CurrentMusicTrack = desiredMusicTrack;
            }

            // Enqueue task completion cue
            string? taskCompleteCue = _configuration.SelectTaskCompleteCue();
            if (completedTasks > 0 && taskCompleteCue != null)
            {
                EnqueueCue(ref audio, taskCompleteCue);
            }

            // Enqueue event change cue
            if (eventKind != audio.LastObservedEventKind)
            {
                string? eventChangeCue = _configuration.SelectEventChangeCue(eventKind);
                if (eventChangeCue != null)
                {
                    EnqueueCue(ref audio, eventChangeCue);
                }
            }

            // Limit pending cues
            while (audio.PendingCues.Count > _configuration.MaxPendingCues)
            {
                audio.PendingCues.Dequeue();
            }

            // Update last cue and event kind
            audio.LastCue = audio.PendingCues.Count > 0 ? audio.PendingCues.Peek() : string.Empty;
            audio.LastObservedEventKind = eventKind;

            // Publish audio state to world state
            _worldState.SetValue("audio.ambient", audio.CurrentAmbientTrack);
            _worldState.SetValue("audio.music", audio.CurrentMusicTrack);
            _worldState.SetValue("audio.last_cue", audio.LastCue);
            _worldState.SetValue("audio.cue_version", audio.CueVersion);
        }
    }

    private static void EnqueueCue(ref AudioStateComponent audio, string cueId)
    {
        audio.PendingCues.Enqueue(cueId);
        audio.CueVersion++;
    }
}
