using Azure.Identity;
using Orleans;
using Orleans.Hosting;
using Orleans.Migration.Source;
using Orleans.Persistence.AzureStorage.Migration.Reminders;
using Orleans.Persistence.Migration;
using Orleans.Persistence.AzureStorage.Migration;

var rnd = new Random();
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();

    siloBuilder
        .AddMigrationTools() // once
        .AddDataMigrator("source", "destination", options =>
        {
            options.BackgroundTaskInitialDelay = TimeSpan.FromSeconds(5);
        }, runAsBackgroundService: false)
        .AddMigrationGrainStorageAsDefault(options =>
        {
            options.SourceStorageName = "source";
            options.DestinationStorageName = "destination";

            options.Mode = GrainMigrationMode.ReadDestinationWithFallback_WriteBoth;
        })
        .AddAzureBlobGrainStorage("source", options =>
        {
            options.ConfigureBlobServiceClient(new Uri("https://dmkorolevstorage.blob.core.windows.net/"), new DefaultAzureCredential());
            options.ContainerName = "source1";
        })
        .AddMigrationAzureBlobGrainStorage("destination", options =>
        {
            options.ConfigureBlobServiceClient(new Uri("https://dmkorolevstorage.blob.core.windows.net/"), new DefaultAzureCredential());
            options.ContainerName = "destination1";
        });

    siloBuilder
        .UseAzureTableReminderService("source", options =>
        {
            options.TableName = "sourcereminders";
            options.ConfigureTableServiceClient(new Uri("https://dmkorolevstorage.table.core.windows.net/"), new DefaultAzureCredential());
        })
        .UseMigrationAzureTableReminderStorage("destination", options =>
        {
            options.TableName = "migratedreminders";
            options.ConfigureTableServiceClient(new Uri("https://dmkorolevstorage.table.core.windows.net/"), new DefaultAzureCredential());
        })
        .UseMigrationReminderTable(options =>
        {
            options.SourceReminderTable = "source";
            options.DestinationReminderTable = "destination";

            options.Mode = ReminderMigrationMode.ReadSource_WriteBoth;
        });
});

var app = builder.Build();

app.MapGet("/", static () => "Migration orleans test hello!");

app.MapGet("/reminders/{grainId}", async (IClusterClient grains, int grainId) =>
{
    var grain = grains.GetGrain<ISimplePersistentMigrationGrain>(grainId);

    await grain.RegisterReminder($"reminder-{grainId}");

    var a = await grain.GetA();
    var ab = await grain.GetAxB();

    return Results.Json(new
    {
        Grain = grain,
        A = a,
        AxB = ab
    });
});


app.MapGet("/upd/{grainId}", async (IClusterClient grains, int grainId) =>
{
    var grain = grains.GetGrain<ISimplePersistentMigrationGrain>(grainId);

    await grain.SetA(rnd.Next(0, 100));
    await grain.SetB(rnd.Next(0, 100));

    var a = await grain.GetA();
    var ab = await grain.GetAxB();

    return Results.Json(new
    {
        Grain = grain,
        A = a,
        AxB = ab
    });
});


app.MapGet("/grains/{grainId}", async (IClusterClient grains, int grainId) =>
{
    var grain = grains.GetGrain<ISimplePersistentMigrationGrain>(grainId);

    var a = await grain.GetA();
    var ab = await grain.GetAxB();

    return Results.Json(new
    {
        Grain = grain,
        A = a,
        AxB = ab
    });
});

app.Run();
