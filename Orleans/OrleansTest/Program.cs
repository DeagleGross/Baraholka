using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Services;
using Orleans.Timers;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();

    // builder.AddAzureTableGrainStorageAsDefault(opt => opt.);
    siloBuilder.UseAzureTableReminderService("UseDevelopmentStorage=true");
});

var app = builder.Build();

app.MapGet("/grain/{grainId}", (IGrainFactory grains, int grainId) =>
{
    var grain = grains.GetGrain<IPingGrain>(grainId);
    return Results.Ok(grain);
});

app.MapGet("/ping/{grainId}", async (IGrainFactory grains, int grainId) =>
{
    var grain = grains.GetGrain<IPingGrain>(grainId);
    await grain.Ping();
});

app.Run();

public interface IPingGrain : IGrainWithIntegerKey
{
    Task Ping();
}

public sealed class PingGrain : IGrainBase, IPingGrain, IDisposable
{
    private readonly ITimerRegistry _timerRegistry;
    private readonly IReminderRegistry _reminderRegistry;

    private IGrainReminder? _reminder;

    public IGrainContext GrainContext { get; }

    public PingGrain(
        ITimerRegistry timerRegistry,
        IReminderRegistry reminderRegistry,
        IGrainContext grainContext)
    {
        _timerRegistry = timerRegistry;
        _reminderRegistry = reminderRegistry;
        GrainContext = grainContext;
    }

    public async Task Ping()
    {
        // Register timer
        _timerRegistry.RegisterGrainTimer(
            GrainContext,
            callback: static async (state, cancellationToken) =>
            {
                Console.WriteLine($"({DateTime.Now}) Invoked reminder!");
                await Task.CompletedTask;
            },
            state: this,
            options: new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(3),
                Period = TimeSpan.FromSeconds(10)
            });

        _reminder = await _reminderRegistry.RegisterOrUpdateReminder(
            callingGrainId: GrainContext.GrainId,
            reminderName: GrainContext.GrainId.ToString(),
            dueTime: TimeSpan.Zero,
            period: TimeSpan.FromHours(1));
    }

    void IDisposable.Dispose()
    {
        if (_reminder is not null)
        {
            _reminderRegistry.UnregisterReminder(
                GrainContext.GrainId, _reminder);
        }
    }
}