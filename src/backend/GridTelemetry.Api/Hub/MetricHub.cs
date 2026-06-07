namespace GridTelemetry.Api.Hub;

public class MetricHub : Microsoft.AspNetCore.SignalR.Hub
{
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Client connection [{Context.ConnectionId}] established");
        await base.OnConnectedAsync();
    }
}