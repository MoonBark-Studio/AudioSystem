namespace AudioSystem.Core.Components;

using Friflo.Engine.ECS;

/// <summary>
/// Tracks music change cooldown to prevent rapid track switching.
/// </summary>
public struct MusicCooldownComponent : IComponent
{
    /// <summary>
    /// The time (in seconds) when the last music change occurred.
    /// </summary>
    public float LastMusicChangeTime;

    /// <summary>
    /// The minimum time (in seconds) between music changes.
    /// </summary>
    public float MusicChangeCooldown;

    /// <summary>
    /// Creates a new MusicCooldownComponent.
    /// </summary>
    /// <param name="musicChangeCooldown">The minimum time between music changes in seconds.</param>
    /// <returns>A new MusicCooldownComponent instance.</returns>
    public static MusicCooldownComponent Create(float musicChangeCooldown = 3.0f)
    {
        return new MusicCooldownComponent
        {
            LastMusicChangeTime = 0.0f,
            MusicChangeCooldown = musicChangeCooldown
        };
    }
}
