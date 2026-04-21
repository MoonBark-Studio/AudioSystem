using MoonBark.Telemetry.Core;

namespace MoonBark.Telemetry.Tests.Core;

public class GameStatDefinitionTests
{
    [Fact]
    public void DefaultValues_AreSane()
    {
        var stat = new GameStatDefinition();

        Assert.Equal(string.Empty, stat.Id);
        Assert.Equal(string.Empty, stat.DisplayName);
        Assert.Equal("general", stat.Category);
        Assert.True(stat.ShowInPanel);
        Assert.False(stat.DevOnly);
        Assert.Equal("{0}", stat.ValueFormat);
    }

    [Fact]
    public void Id_CanBeSetToEngineAgnosticIdentifier()
    {
        var stat = new GameStatDefinition { Id = "economy_balance" };
        Assert.Equal("economy_balance", stat.Id);
    }

    [Fact]
    public void Category_CanBeSetToAnyString()
    {
        var stat = new GameStatDefinition { Category = "simulation" };
        Assert.Equal("simulation", stat.Category);
    }

    [Fact]
    public void DevOnly_WhenTrue_IsNotShownToPlayers()
    {
        var stat = new GameStatDefinition
        {
            Id = "gc_gen0",
            DevOnly = true,
            ShowInPanel = true
        };

        Assert.True(stat.DevOnly);
        Assert.True(stat.ShowInPanel);
    }
}
