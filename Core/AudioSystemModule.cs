using AudioSystem.Core;
using MoonBark.Framework.Core;

namespace AudioSystem;

/// <summary>
/// Registers AudioSystem's services with the Framework module registry.
/// </summary>
public sealed class AudioSystemModule : IFrameworkModule
{
    private readonly IAudioService _audioService;

    public AudioSystemModule(IAudioService audioService)
    {
        _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
    }

    public void ConfigureServices(IServiceRegistry services)
    {
        services.Register(_audioService);
    }
}
