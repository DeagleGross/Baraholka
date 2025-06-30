using MemoryOrleans.Models;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(static siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("urls");

    siloBuilder.Configure<GrainCollectionOptions>(options =>
    {
        options.MemoryPressureGrainCollectionOptions = new()
        {
            MemoryUsageCollectionEnabled = true,
            MemoryUsagePollingPeriod = TimeSpan.FromSeconds(3),
            MemoryUsageLimitPercentage = 75,
            MemoryUsageTargetPercentage = 60
        };
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapGet("/", () => "hello world");

app.MapPost("/stress/{count}", async (IGrainFactory grains, int count = 100, int dataSizeKb = 512) =>
{
    var tasks = new List<Task>();
    var data = new byte[dataSizeKb * 1024];
    Random.Shared.NextBytes(data);

    for (int i = 0; i < count; i++)
    {
        var grain = grains.GetGrain<IDataGrain>($"grain-{Guid.NewGuid()}");
        tasks.Add(grain.SetData(data));
    }

    await Task.WhenAll(tasks);
    return Results.Ok($"Created {count} grains, each with {dataSizeKb} KB of data.");
});

app.Run();