// <configuration>
using Azure.Identity;
using Orleans._8x;
using Orleans.Timers;
using Tester.AzureUtils.Migration.Grains;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(static siloBuilder =>
{
    siloBuilder.UseLocalhostClustering(serviceId: "ce933f40cf2b4824967dd5ea984a6323");

    siloBuilder.AddCosmosGrainStorageAsDefault(options =>
    {
        // options.IsResourceCreationEnabled = true;
        options.ContainerName = $"migration-8";
        options.DatabaseName = "Orleans";
        // options.PartitionKeyPath = "/pk"; // does not work?

        var azureCredentials = new DefaultAzureCredential();
        options.ConfigureCosmosClient("https://dmkorolev-cosmos.documents.azure.com:443/", tokenCredential: azureCredentials);
    });

    siloBuilder.UseAzureTableReminderService("UseDevelopmentStorage=true");
});

using var app = builder.Build();
// </configuration>

// <endpoints>
app.MapGet("/", static () => "Migration orleans test hello!");

app.MapGet("/reminders/migration",
    static (IGrainFactory grains, IReminderService service)
        => ResolveAndOutputReminder(grains, service, 10000)
);
app.MapGet("reminders/{grainId}",
    static (IGrainFactory grains, IReminderService service, int grainId)
        => ResolveAndOutputReminder(grains, service, grainId)
);

app.MapGet("reminders/create/{grainId}", async static (IGrainFactory grains, IReminderRegistry reminders, int grainId) =>
{
    var grain = grains.GetGrain<ISimplePersistentGrain>(grainId);
    await grain.SetA(1);
    await grain.CreateReminder();

    var reminder = await grain.GetReminder();
    return Results.Json(reminder);
});


app.MapGet("/test-migration", static async (IGrainFactory grains) => await ResolveAndOutputGrain(grains, 100001));
app.MapGet("/migration/{grainId}", ResolveAndOutputGrain);
app.MapGet("/migration/create/{grainId}", static async (IGrainFactory grains, int grainId) =>
{
    var grain = grains.GetGrain<ISimplePersistentGrain>(grainId);
    await grain.SetA(1);
    await grain.SetB(2);
    return await ResolveAndOutputGrain(grains, grainId);
});

app.MapGet("/noinheritance/create/{grainId}", static async (IGrainFactory grains, int grainId) =>
{
    var grain = grains.GetGrain<ITestGrain>(grainId, grainClassNamePrefix: "no");
    await grain.SetValue(1);
    return RespondGrain(grain);
});

app.MapGet("/standard/create/{grainId}", static async (IGrainFactory grains, int grainId) =>
{
    var grain = grains.GetGrain<ITestGrain>(grainId);
    await grain.SetValue(1);
    return RespondGrain(grain);
});

app.Run();

static async Task<IResult> ResolveAndOutputReminder(IGrainFactory grains, IReminderService service, int grainId)
{
    var grain = grains.GetGrain<ISimplePersistentGrain>(grainId);
    var reminder = await service.GetReminder(grain.GetGrainId(), "Reminder6e7cbb3794e64ea49f56d4aed0a66179");
    return Results.Json(reminder);
}

static async Task<IResult> ResolveAndOutputGrain(IGrainFactory grains, int grainId)
{
    var grain = grains.GetGrain<ISimplePersistentMigrationGrain>(grainId);

    // id: migrationtestgrain_186A1
    // partitionkey: Tester.AzureUtils.Migration.Grains.MigrationTestGrain

    var a = await grain.GetA();
    var ab = await grain.GetAxB();

    return Results.Json(new
    {
        Grain = grain,
        A = a,
        AxB = ab
    });
}

static IResult RespondGrain(object obj)
{
    return Results.Json(obj);
}