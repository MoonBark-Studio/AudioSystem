using MoonBark.AudioSystem.Core;

namespace MoonBark.AudioSystem;

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
        services.Register<IAudioService>(_audioService);
        services.Register<IAudioPlayback>(_audioService);
        services.Register<IAudioVolumeControl>(_audioService);
  }
}
