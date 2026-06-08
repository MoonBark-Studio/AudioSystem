using MoonBark.AudioSystem.Core;
using MoonBark.Framework.Core;
using Xunit;

namespace MoonBark.AudioSystem.Tests;

public class AudioSystemModuleTests
{
    [Fact]
    public void ConfigureServices_RegistersAllAudioContracts()
    {
        var service = new StubAudioService();
        var registry = new ServiceRegistry();

        new AudioSystemModule(service).ConfigureServices(registry);

        Assert.Same(service, registry.Resolve<IAudioService>());
        Assert.Same(service, registry.Resolve<IAudioPlayback>());
        Assert.Same(service, registry.Resolve<IAudioVolumeControl>());
    }

    private sealed class StubAudioService : IAudioService
    {
        public void PlayOneShot(string cueId) { }
        public void PlayMusic(string cueId, float fadeInDurationSec = 0f) { }
        public void PlayAmbient(string cueId, float fadeInDurationSec = 0f) { }
        public void StopMusic(float fadeOutSec = 0.3f) { }
        public void StopAmbient(float fadeOutSec = 0.3f) { }
        public void SetMasterVolume(float volume) { }
        public void SetMusicVolume(float volume) { }
        public void SetSoundEffectsVolume(float volume) { }
    }
}
