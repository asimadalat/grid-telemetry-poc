using RabbitMQ.Client;

namespace GridTelemetry.Core.Messaging;

public static class RabbitMqConfiguration
{
    public const string QueueName = "substation_sampling_queue";

    public static ConnectionFactory GetConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = "localhost",
            UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER")!,
            Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")!
        };
    }
}