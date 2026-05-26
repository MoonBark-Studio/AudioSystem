using Friflo.Engine.ECS;

namespace MoonBark.AudioSystem.ECS.Components;

public struct AudioSourceComponent : IComponent
{
    public string SoundEventId;
    public float Volume;
    public bool Loop;

    public AudioSourceComponent(string soundEventId, float volume = 1f, bool loop = false)
    {
        SoundEventId = soundEventId;
        Volume = volume;
        Loop = loop;
    }
}
