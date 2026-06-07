using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using GridTelemetry.Api.Hub;
using GridTelemetry.Core.Messaging;
using GridTelemetry.Core.Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GridTelemetry.Api;

public class MetricWorker(
    ILogger<MetricWorker> logger,
    IHubContext<MetricHub> hubContext) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Starting API SignalR stream, Connecting to Exchange...");

        var factory = RabbitMqConfiguration.GetConnectionFactory();
        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: RabbitMqConfiguration.ExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: RabbitMqConfiguration.SignalRQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: RabbitMqConfiguration.SignalRQueueName,
            exchange: RabbitMqConfiguration.ExchangeName,
            routingKey: string.Empty,
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
                    await hubContext.Clients.All.SendAsync(
                        "ReceiveTelemetryUpdate",
                        metric,
                        cancellationToken: stoppingToken);
                }

                await _channel.BasicAckAsync(
                    ea.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error broadcasting websocket message.");
                await _channel.BasicNackAsync(
                    ea.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: RabbitMqConfiguration.SignalRQueueName,
            autoAck: false,
            consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}