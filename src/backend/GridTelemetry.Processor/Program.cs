using GridTelemetry.Core.Data;
using GridTelemetry.Processor;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

foreach (var line in File.ReadAllLines("../../../.env"))
{
    var pair = line.Split('=', 2);

    if (pair.Length == 2)
        Environment.SetEnvironmentVariable(pair[0], pair[1]);
}

var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB");
var dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER");
var dbPass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

var connectionString = $"Host=localhost;Database={dbName};Username={dbUser};Password={dbPass}";
Console.WriteLine(connectionString);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
}

await host.RunAsync();
