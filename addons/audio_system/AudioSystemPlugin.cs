using Godot;
using MoonBark.AudioSystem.Core;

namespace MoonBark.AudioSystem.Godot.Plugin.Plugin;

[GlobalClass]
public partial class AudioSystemPlugin : RefCounted
{
    public static readonly string PluginId = "MoonBark.AudioSystem";

    public static void Install(IAudioService audioService, IServiceRegistry registry)
    {
        var module = new AudioSystemModule(audioService);
        module.ConfigureServices(registry);
    }
}