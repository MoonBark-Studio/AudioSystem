using System.Text.Json;
using MoonBark.AudioSystem.Core.Configuration;
using Xunit;

namespace MoonBark.AudioSystem.Tests;

public class AudioConfigTierFlattenerTests
{
    [Fact]
    public void Load_MoonBarkIdleConfig_FlattensWaterFillCue()
    {
        string configPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "..",
            "games", "moonbark-idle", "Godot", "assets", "audio", "audio_config.json");
        configPath = Path.GetFullPath(configPath);
        if (!File.Exists(configPath))
            return;

        AudioConfigDocument config = AudioConfigLoader.Load(configPath);

        Assert.True(config.Cues.TryGetPath("water_fill", out string? path), "tier liquid.water_fill should flatten to water_fill");
        Assert.Contains("automation/water_fill", path!, StringComparison.OrdinalIgnoreCase);
        Assert.True(config.Cues.TryGetPath("harvest_pop", out _));
        Assert.True(config.Music.TryGetPath("main_theme", out _));
    }

    [Fact]
    public void MergeTierPathsInto_AddsLeafCuesAndAmbientLoops()
    {
        const string json = """
            {
              "ambient_automation": {
                "liquid": { "water_fill": "cues/automation/water_fill.wav" },
                "ambient_loops": { "wind_breeze": "ambient/wind.wav" }
              }
            }
            """;

        using JsonDocument doc = JsonDocument.Parse(json);
        var cues = new Dictionary<string, string>();
        var ambient = new Dictionary<string, string>();

        AudioConfigTierFlattener.MergeTierPathsInto(cues, ambient, doc.RootElement);

        Assert.Equal("cues/automation/water_fill.wav", cues["water_fill"]);
        Assert.Equal("ambient/wind.wav", ambient["wind_breeze"]);
    }
}
