namespace GridTelemetry.Core.Model;

public record SubstationMetric
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string SubstationCode { get; set; } = string.Empty;

    public double CurrentLoadMw { get; set; }

    public double MaxCapacityMw { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public double PercentageUtilisation => (CurrentLoadMw / MaxCapacityMw) * 100;
}