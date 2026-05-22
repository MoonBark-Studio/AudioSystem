using MoonBark.AudioSystem.Core.Diagnostics;
using Xunit;

namespace MoonBark.AudioSystem.Tests;

public class AudioPlaybackLogTests
{
    [Fact]
    public void Format_CueNotRegistered_IncludesCueAndCategory()
    {
        string message = AudioPlaybackLog.Format(
            AudioPlaybackFailureKind.CueNotRegistered,
            "one-shot",
            "water_fill",
            "");

        Assert.Contains("one-shot", message);
        Assert.Contains("water_fill", message);
        Assert.Contains("not registered", message);
    }

    [Fact]
    public void Format_LoadFailed_IncludesPathDetail()
    {
        string detail = AudioPlaybackLog.LoadReturnedNullDetail("res://assets/audio/cues/ui/click.wav");
        string message = AudioPlaybackLog.Format(
            AudioPlaybackFailureKind.LoadFailed,
            "music",
            "main_theme",
            detail);

        Assert.Contains("main_theme", message);
        Assert.Contains("loader returned null", message);
    }
}
