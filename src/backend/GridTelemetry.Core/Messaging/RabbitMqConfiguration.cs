using RabbitMQ.Client;

namespace GridTelemetry.Core.Messaging;

public static class RabbitMqConfiguration
{
    public const string ExchangeName = "grid_telemetry_exchange";
    public const string DbQueueName = "substation_db_persist_queue";
    public const string SignalRQueueName = "substation_sr_stream_queue";

    public static ConnectionFactory GetConnectionFactory() => new()
    {
        HostName = "localhost",
        UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER")!,
        Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")!
    };
}