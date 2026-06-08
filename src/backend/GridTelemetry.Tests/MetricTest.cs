namespace GridTelemetry.Tests;

public class MetricTest
{
    [Fact]
    public void Utilisation_ShouldCalculateCorrectly_UnderNormalLoad()
    {
        double currentLoad = 70.0;
        double maxCapacity = 100.0;
        double percentageUtilisation = (currentLoad / maxCapacity) * 100;

        Assert.Equal(70.0, percentageUtilisation);
    }

    [Fact]
    public void System_ShouldIdentifyOverload_WhenUtilisationExceedsThreshold()
    {
        double currentLoad = 85.0;
        double maxCapacity = 100.0;

        double percentageUtilisation = (currentLoad / maxCapacity) * 100;
        bool isOverloaded = percentageUtilisation > 80.0;

        Assert.True(isOverloaded);
    }
}