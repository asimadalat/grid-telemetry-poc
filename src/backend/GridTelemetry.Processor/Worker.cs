using System.Text;
using System.Text.Json;
using GridTelemetry.Core.Data;
using GridTelemetry.Core.Messaging;
using GridTelemetry.Core.Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GridTelemetry.Processor;

public class Worker(
    ILogger<Worker> logger,
    IServiceProvider serviceProvider
) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Starting Substation Sample Processor, Connecting to Queue...");

        var factory = RabbitMqConfiguration.GetConnectionFactory();
        _connection = await factory.CreateConnectionAsync(
            cancellationToken: stoppingToken);
        _channel = await _connection.CreateChannelAsync(
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: RabbitMqConfiguration.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 10,
            global: false,
            cancellationToken: stoppingToken);
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var rawJson = Encoding.UTF8.GetString(ea.Body.ToArray());
                var metric = JsonSerializer.Deserialize<SubstationMetric>(rawJson);

                if (metric != null)
                {
                    using var scope = serviceProvider.CreateScope();
                    var dbContext = scope
                        .ServiceProvider
                        .GetRequiredService<ApplicationDbContext>();

                    metric.Id = Guid.NewGuid();
                    dbContext.SubstationMetrics.Add(metric);
                    await dbContext.SaveChangesAsync(stoppingToken);

                    if (logger.IsEnabled(LogLevel.Information)) logger.LogInformation(
                        "Processed & Saved -> Substation: {Code} | Utilisation: {Pct}%",
                        metric.SubstationCode,
                        Math.Round(metric.PercentageUtilisation, 2));

                    await _channel.BasicAckAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error processing sampling message from queue, {}", ex);
                await _channel.BasicNackAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: RabbitMqConfiguration.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync(cancellationToken);
        if (_connection != null) await _connection.CloseAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}
