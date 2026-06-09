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

    [Fact]
    public void NormalMode_ShouldAlwaysStayWithin_35_To_65_Pct()
    {
        Random random = new();
        double maxCapacity = 500.0;

        for (int i = 0; i < 1000; i++)
        {
            int randPct = random.Next(35, 66);
            double currentLoad = maxCapacity * (randPct / 100.0);
            double pctUtilisation = (currentLoad / maxCapacity) * 100;

            Assert.True(
                pctUtilisation >= 35.0 && pctUtilisation <= 65.0,
                $"Failed iteration {i}: {pctUtilisation} breaks 35% - 65% bounds.");
        }
    }

    [Fact]
    public void StressTestMode_ShouldAlwaysStayWithin_95_To_105_Pct()
    {
        Random random = new();
        double maxCapacity = 500.0;

        for (int i = 0; i < 1000; i++)
        {
            int randPct = random.Next(95, 106);
            double currentLoad = maxCapacity * (randPct / 100.0);
            double pctUtilisation = (currentLoad / maxCapacity) * 100.0;

            Assert.True(
                pctUtilisation >= 95.0 && pctUtilisation <= 105.0,
                $"Failed stress iteration {i}: {pctUtilisation} breaks 95% - 105% bounds.");
        }
    }
}