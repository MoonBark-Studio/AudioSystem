using MoonBark.Telemetry.Core;

namespace MoonBark.Telemetry.Tests.Core;

public class TelemetryConfigurationTests
{
    [Fact]
    public void DefaultValues_AreSane()
    {
        var config = new TelemetryConfiguration();

        Assert.Equal("F3", config.ToggleKey);
        Assert.Equal(60, config.HistorySize);
        Assert.Equal(4.0, config.CriticalThresholdMs);
        Assert.True(config.AutoShowOnCritical);
        Assert.Equal(10, config.PanelOffsetX);
        Assert.Equal(10, config.PanelOffsetY);
        Assert.Equal(400, config.PanelWidth);
        Assert.Empty(config.Stats);
    }

    [Fact]
    public void Stats_CanBeAddedAndRetrieved()
    {
        var config = new TelemetryConfiguration();
        var stat = new GameStatDefinition
        {
            Id = "fps",
            DisplayName = "FPS",
            Category = "performance",
            ShowInPanel = true,
            DevOnly = false,
            ValueFormat = "{0:F0}"
        };
        config.Stats.Add(stat);

        Assert.Single(config.Stats);
        Assert.Equal("fps", config.Stats[0].Id);
        Assert.Equal("FPS", config.Stats[0].DisplayName);
        Assert.Equal("performance", config.Stats[0].Category);
    }

    [Fact]
    public void CriticalThresholdMs_CanBeSetBelowAverageTickCost()
    {
        var config = new TelemetryConfiguration { CriticalThresholdMs = 0.5 };

        Assert.Equal(0.5, config.CriticalThresholdMs);
    }
}
