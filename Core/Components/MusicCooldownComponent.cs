using Friflo.Engine.ECS;

namespace AudioSystem.Core.Components;

/// <summary>
/// ECS marker component for entities that track music cooldown state.
/// Used to throttle music transitions and prevent rapid back-to-back cue changes.
/// </summary>
public struct MusicCooldownComponent : IComponent
{
    public static MusicCooldownComponent Create()
    {
        return new MusicCooldownComponent();
    }
}
