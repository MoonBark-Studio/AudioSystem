using Friflo.Engine.ECS;

namespace AudioSystem.Core.CueSelection;

/// <summary>
/// Generic audio cue selection system that reads normalized cue-selection state
/// and selects ambient/music/cue tracks via an injected
/// <see cref="ICueSelectionConfiguration"/>. Game-specific selection rules
/// belong in the configuration implementation; this system is purely
/// orchestration.
/// </summary>
public sealed class AudioCueSystem
{
    private readonly IAudioCueStateReader _stateReader;
    private readonly ICueSelectionConfiguration _configuration;

    /// <summary>
    /// Creates a new cue selection system.
    /// </summary>
    /// <param name="stateReader">Normalized cue-selection state provided by the integration layer.</param>
    /// <param name="configuration">Game-specific cue selection rules.</param>
    public AudioCueSystem(IAudioCueStateReader stateReader, ICueSelectionConfiguration configuration)
    {
        _stateReader = stateReader ?? throw new ArgumentNullException(nameof(stateReader));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Advances cue selection by one tick. Reads current world state and
    /// updates the <c>AudioStateComponent</c> on the first entity that owns it.
    /// </summary>
    public void Update(EntityStore world, float deltaTime)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        // Generic orchestration placeholder. Game-specific integrations provide
        // normalized state via IAudioCueStateReader and selection rules via
        // ICueSelectionConfiguration.
    }
}
