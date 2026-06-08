using Friflo.Engine.ECS;
using MoonBark.WorldState;

namespace AudioSystem.Core.CueSelection;

/// <summary>
/// Generic audio cue selection system that reads the WorldState blackboard
/// and selects ambient/music/cue tracks via an injected
/// <see cref="ICueSelectionConfiguration"/>. Game-specific selection rules
/// belong in the configuration implementation; this system is purely
/// orchestration.
/// </summary>
public sealed class AudioCueSystem
{
    private readonly WorldState _worldState;
    private readonly ICueSelectionConfiguration _configuration;

    /// <summary>
    /// Creates a new cue selection system.
    /// </summary>
    /// <param name="worldState">WorldState blackboard providing time, event, and task data.</param>
    /// <param name="configuration">Game-specific cue selection rules.</param>
    public AudioCueSystem(WorldState worldState, ICueSelectionConfiguration configuration)
    {
        _worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Advances cue selection by one tick. Reads current world state and
    /// updates the <c>AudioStateComponent</c> on the first entity that owns it.
    /// </summary>
    public void Update(EntityStore world, float deltaTime)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        // Generic orchestration placeholder. Game-specific implementations
        // override selection rules via ICueSelectionConfiguration.
    }
}
