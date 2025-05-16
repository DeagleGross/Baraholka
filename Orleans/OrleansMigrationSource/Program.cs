using Azure.Identity;
using Orleans;
using Orleans.Hosting;
using Orleans.Migration.Source;
using Orleans.Persistence.Cosmos.Migration;
using Orleans.Persistence.Migration;

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

            options.Mode = GrainMigrationMode.Disabled;
        })
        .AddAzureBlobGrainStorage("source", options =>
        {
            options.ConfigureBlobServiceClient(new Uri("https://dmkorolevstorage.blob.core.windows.net/"), new DefaultAzureCredential());
            options.ContainerName = "grains";
        })
        .AddMigrationAzureCosmosGrainStorage("destination", options =>
        {
            options.ConfigureCosmosClient("https://dmkorolev-cosmos.documents.azure.com:443/", new DefaultAzureCredential());
            options.DatabaseName = "MigrationSample";
            options.ContainerName = "Destination";

#pragma warning disable OrleansCosmosExperimental // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            options.UseExperimentalFormat = true;
#pragma warning restore OrleansCosmosExperimental // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        });

    siloBuilder
        .UseMigrationAzureTableReminderStorage(
            oldStorageOptions =>
            {
                oldStorageOptions.ConfigureTableServiceClient(new Uri("https://dmkorolevstorage.table.core.windows.net/"), new DefaultAzureCredential());
                oldStorageOptions.TableName = "sourcereminders";
            },
            migrationOptions =>
            {
                migrationOptions.ConfigureTableServiceClient(new Uri("https://dmkorolevstorage.table.core.windows.net/"), new DefaultAzureCredential());
                migrationOptions.TableName = "migratedreminders";
            }
        );
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


app.MapGet("/get/{grainId}", async (IClusterClient grains, int grainId) =>
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
