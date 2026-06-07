using GridTelemetry.Simulator;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

foreach (var line in File.ReadAllLines("../../../.env"))
{
    var pair = line.Split('=', 2);

    if (pair.Length == 2)
        Environment.SetEnvironmentVariable(pair[0], pair[1]);
}

var host = builder.Build();
host.Run();
