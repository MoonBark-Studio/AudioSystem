using Friflo.Engine.ECS;
using MoonBark.AudioSystem.ECS.Components;

namespace MoonBark.AudioSystem.ECS.Systems;

public class AudioPlaybackSystem
{
    public void Process(EntityStore store)
    {
        // Simple placeholder processing audio triggers
        foreach (var entity in store.Query<AudioSourceComponent>().Entities)
        {
            // Trigger audio logic
        }
    }
}
