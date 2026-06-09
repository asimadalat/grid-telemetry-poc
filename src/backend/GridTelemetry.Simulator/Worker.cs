using GridTelemetry.Core.Messaging;
using GridTelemetry.Core.Model;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace GridTelemetry.Simulator;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    private readonly bool isStressTestMode = false;
    private readonly Random _random = new();
    private readonly List<(string Code, double MaxCapacity)> _substations =
    [
        ("BIRMINGHAM-01", 800.0),
        ("WARWICK-02", 380.0),
        ("COVENTRY-03", 500.0),
        ("LONDON-04", 1200.0),
        ("MANCHESTER-05", 650),
        ("LEEDS-06", 600.0)
    ];


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting Substation Sampling Simulator...");

        var factory = RabbitMqConfiguration.GetConnectionFactory();
        using var connection = await factory.CreateConnectionAsync(
            cancellationToken: stoppingToken
        );
        using var channel = await connection.CreateChannelAsync(
            cancellationToken: stoppingToken
        );

        await channel.ExchangeDeclareAsync(
            exchange: RabbitMqConfiguration.ExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var (Code, MaxCapacity) = _substations[_random.Next(_substations.Count)];
            double genLoad;

            if (isStressTestMode)
            {
                int randPct = _random.Next(95, 106);
                genLoad = Math.Round(MaxCapacity * (randPct / 100.0), 2);
            }
            else
            {
                int randPct = _random.Next(35, 66);
                genLoad = Math.Round(MaxCapacity * (randPct / 100.0), 2);
            }

            var sample = new SubstationMetric
            {
                SubstationCode = Code,
                CurrentLoadMw = genLoad,
                MaxCapacityMw = MaxCapacity,
                Timestamp = DateTime.UtcNow
            };

            string jsonPayload = JsonSerializer.Serialize(sample);

            var properties = new BasicProperties { DeliveryMode = DeliveryModes.Persistent };
            await channel.BasicPublishAsync(
                exchange: RabbitMqConfiguration.ExchangeName,
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: properties,
                body: Encoding.UTF8.GetBytes(jsonPayload),
                cancellationToken: stoppingToken
            );

            if (logger.IsEnabled(LogLevel.Information)) logger.LogInformation(
                "Sent Sample Event -> Substation: {Code} | Load: {Load}MW / {Max}MW ({Percent}%",
                sample.SubstationCode,
                sample.CurrentLoadMw,
                sample.MaxCapacityMw,
                Math.Round(sample.PercentageUtilisation, 2)
            );

            await Task.Delay(2000, stoppingToken);
        }
    }
}
