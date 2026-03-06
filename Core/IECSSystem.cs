namespace AudioSystem.Core.ECS;

using Friflo.Engine.ECS;

/// <summary>
/// Interface for ECS systems that process entities in the world.
/// </summary>
public interface IECSSystem
{
    /// <summary>
    /// Priority for system execution order (lower = earlier).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Updates the system with the given world and delta time.
    /// </summary>
    /// <param name="world">The ECS world containing entities to process.</param>
    /// <param name="deltaTime">Time elapsed since the last update in seconds.</param>
    void Update(EntityStore world, float deltaTime);
}
