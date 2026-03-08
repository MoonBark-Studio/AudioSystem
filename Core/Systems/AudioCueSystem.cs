namespace AudioSystem.Core.Systems;

using MoonBark.Core.ECS;

using AudioSystem.Core.Components;
using Friflo.Engine.ECS;

public sealed class AudioCueSystem : IECSSystem
{
    private readonly global::WorldState.WorldState _worldState;

    public AudioCueSystem(global::WorldState.WorldState worldState)
    {
        _worldState = worldState;
    }

    public int Priority => 12;

    public void Update(EntityStore world, float deltaTime)
    {
        bool isNight = _worldState.GetValue("time.is_night") as bool? ?? false;
        string eventKind = _worldState.GetValue("narrative.active_event_kind") as string ?? string.Empty;
        int completedTasks = GetInt(_worldState.GetValue("task.completed_last_frame"));

        ArchetypeQuery<AudioStateComponent> query = world.Query<AudioStateComponent>();
        foreach (Entity entity in query.Entities)
        {
            ref AudioStateComponent audio = ref entity.GetComponent<AudioStateComponent>();
            audio.CurrentAmbientTrack = isNight ? "forest_night" : "forest_day";
            audio.CurrentMusicTrack = string.Equals(eventKind, "ThreatPressure", System.StringComparison.Ordinal)
                ? "tension"
                : string.Equals(eventKind, "Opportunity", System.StringComparison.Ordinal)
                    ? "curious_exploration"
                    : "exploration";

            if (completedTasks > 0)
            {
                EnqueueCue(ref audio, "task_complete");
            }

            if (eventKind != audio.LastObservedEventKind)
            {
                if (string.Equals(eventKind, "ResourceScarcity", System.StringComparison.Ordinal))
                {
                    EnqueueCue(ref audio, "resource_warning");
                }
                else if (string.Equals(eventKind, "ThreatPressure", System.StringComparison.Ordinal))
                {
                    EnqueueCue(ref audio, "danger_sting");
                }
            }

            while (audio.PendingCues.Count > 2)
            {
                audio.PendingCues.Dequeue();
            }

            audio.LastCue = audio.PendingCues.Count > 0 ? audio.PendingCues.Peek() : string.Empty;
            audio.LastObservedEventKind = eventKind;
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

    private static int GetInt(object? value)
    {
        return value switch
        {
            int integer => integer,
            float single => (int)single,
            double dbl => (int)dbl,
            _ => 0
        };
    }
}
