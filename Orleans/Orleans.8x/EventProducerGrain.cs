namespace Orleans._8x;

using Orleans;
using Orleans.Streams;
using Microsoft.Extensions.Logging;
using Tester.AzureUtils.Migration.Grains;
using Orleans.Metadata;

public class EventProducerGrain : Grain, IEventProducerGrain
{
    private readonly ILogger<EventProducerGrain> _logger;
    private IAsyncStream<TemperatureReading> _temperatureStream;
    private IAsyncStream<HumidityReading> _humidityStream;
    private IGrainTimer _timer;
    private readonly Random _random = new();

    public EventProducerGrain(ILogger<EventProducerGrain> logger)
    {
        _logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Get stream provider
        var streamProvider = this.GetStreamProvider("AzureQueueProvider");

        // Get streams for different event types
        var streamId = StreamId.Create("SensorData", this.GetPrimaryKeyLong());
        _temperatureStream = streamProvider.GetStream<TemperatureReading>(streamId);

        var humidityStreamId = StreamId.Create("HumidityData", this.GetPrimaryKeyLong());
        _humidityStream = streamProvider.GetStream<HumidityReading>(humidityStreamId);

        _logger.LogInformation("EventProducerGrain {GrainId} activated", this.GetPrimaryKeyLong());

        await base.OnActivateAsync(cancellationToken);
    }       

    public Task StartProducing()
    {
        _logger.LogInformation("Starting event production...");

        _timer = this.RegisterGrainTimer(ProduceEvents, this, new GrainTimerCreationOptions()
        {
            DueTime = TimeSpan.FromSeconds(0),
            Period = TimeSpan.FromSeconds(3)
        });

        return Task.CompletedTask;
    }

    public Task StopProducing()
    {
        _logger.LogInformation("Stopping event production...");
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    private async Task ProduceEvents(object state, CancellationToken token)
    {
        try
        {
            // Produce temperature reading
            var temperatureReading = new TemperatureReading(
                SensorId: $"TEMP_SENSOR_{this.GetPrimaryKeyLong()}",
                Temperature: 20.0 + (_random.NextDouble() * 20.0), // 20-40 degrees
                Timestamp: DateTime.UtcNow
            );

            await _temperatureStream.OnNextAsync(temperatureReading);
            _logger.LogInformation("Produced temperature reading: {Reading}", temperatureReading);

            // Produce humidity reading
            var humidityReading = new HumidityReading(
                SensorId: $"HUMIDITY_SENSOR_{this.GetPrimaryKeyLong()}",
                Humidity: 30.0 + (_random.NextDouble() * 40.0), // 30-70%
                Timestamp: DateTime.UtcNow
            );

            await _humidityStream.OnNextAsync(humidityReading);
            _logger.LogInformation("Produced humidity reading: {Reading}", humidityReading);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error producing events");
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}
