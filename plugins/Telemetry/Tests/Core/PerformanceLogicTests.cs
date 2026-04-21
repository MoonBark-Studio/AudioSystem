using MoonBark.Framework.Core;
using MoonBark.Telemetry.Core;
using Xunit;

namespace MoonBark.Telemetry.Tests.Core;

public class PerformanceLogicTests
{
    [Fact]
    public void ThresholdDetection_IdentifiesHeavySystems()
    {
        var config = new TelemetryConfiguration { CriticalThresholdMs = 2.0 };
        var metrics = new SystemMetrics("HeavySystem", 2.5, 2.1, 3.0, 100);
        
        bool isCritical = metrics.LatestMs >= config.CriticalThresholdMs;
        
        Assert.True(isCritical, "System exceeding 2.0ms should be flagged as critical.");
    }
}
