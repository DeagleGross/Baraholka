using System.Diagnostics.Metrics;
using Orleans;
using Orleans.Streams;
using Orleans3x.Interfaces;

namespace Orleans3x;

public class EventProducerGrain : Grain, IEventProducerGrain
{
    private readonly ILogger<EventProducerGrain> _logger;
    private IAsyncStream<TemperatureReading> _temperatureStream;
    private IDisposable _timer;
    private readonly Random _random = new();

    public EventProducerGrain(ILogger<EventProducerGrain> logger)
    {
        _logger = logger;
    }

    public override async Task OnActivateAsync()
    {
        // Get stream provider
        var streamProvider = this.GetStreamProvider("AzureQueueProvider");

        _temperatureStream = streamProvider.GetStream<TemperatureReading>(Guid.NewGuid(), "temperature");

        // ORLEANS 7+ VERSION
        //var streamId = StreamId.Create("SensorData", this.GetPrimaryKeyLong());
        //_temperatureStream = streamProvider.GetStream<TemperatureReading>(streamId);

        _logger.LogInformation("EventProducerGrain {GrainId} activated", this.GetPrimaryKeyLong());
        await base.OnActivateAsync();
    }

    public Task StartProducing()
    {
        _logger.LogInformation("Starting event production...");

        _timer = this.RegisterTimer(ProduceEvents, this, dueTime: TimeSpan.FromSeconds(0), period: TimeSpan.FromSeconds(3));
        return Task.CompletedTask;
    }

    public Task StopProducing()
    {
        _logger.LogInformation("Stopping event production...");
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    private async Task ProduceEvents(object state)
    {
        try
        {
            var temperatureReading = new TemperatureReading(
                SensorId: $"TEMP_SENSOR_{this.GetPrimaryKeyLong()}",
                Temperature: 20.0 + (_random.NextDouble() * 20.0), // 20-40 degrees
                Timestamp: DateTime.UtcNow
            );

            await _temperatureStream.OnNextAsync(temperatureReading);
            _logger.LogInformation("Produced temperature reading: {Reading}", temperatureReading);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error producing events");
        }
    }

    public override Task OnDeactivateAsync()
    {
        _timer?.Dispose();
        return base.OnDeactivateAsync();
    }
}
