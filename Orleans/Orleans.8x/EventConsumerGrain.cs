namespace Orleans._8x;

using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;
using Tester.AzureUtils.Migration.Grains;

public class EventConsumerGrain : Grain, IEventConsumerGrain,
    IAsyncObserver<TemperatureReading>,
    IAsyncObserver<HumidityReading>
{
    private readonly ILogger<EventConsumerGrain> _logger;
    private StreamSubscriptionHandle<TemperatureReading> _temperatureSubscription;
    private StreamSubscriptionHandle<HumidityReading> _humiditySubscription;

    public EventConsumerGrain(ILogger<EventConsumerGrain> logger)
    {
        _logger = logger;
    }

    public async Task StartConsuming()
    {
        _logger.LogInformation("Starting event consumption...");

        var streamProvider = this.GetStreamProvider("AzureQueueProvider");

        // Subscribe to temperature stream
        var temperatureStreamId = StreamId.Create("SensorData", this.GetPrimaryKeyLong());
        var temperatureStream = streamProvider.GetStream<TemperatureReading>(temperatureStreamId);
        _temperatureSubscription = await temperatureStream.SubscribeAsync(this);

        // Subscribe to humidity stream  
        var humidityStreamId = StreamId.Create("HumidityData", this.GetPrimaryKeyLong());
        var humidityStream = streamProvider.GetStream<HumidityReading>(humidityStreamId);
        _humiditySubscription = await humidityStream.SubscribeAsync(this);

        _logger.LogInformation("Successfully subscribed to streams");
    }

    public async Task StopConsuming()
    {
        _logger.LogInformation("Stopping event consumption...");

        if (_temperatureSubscription != null)
        {
            await _temperatureSubscription.UnsubscribeAsync();
        }

        if (_humiditySubscription != null)
        {
            await _humiditySubscription.UnsubscribeAsync();
        }
    }

    // Handle temperature readings
    Task IAsyncObserver<TemperatureReading>.OnNextAsync(TemperatureReading item, StreamSequenceToken token)
    {
        _logger.LogInformation("Received temperature reading: {SensorId} = {Temperature}°C at {Timestamp}",
            item.SensorId, item.Temperature, item.Timestamp);

        // Process the temperature reading
        if (item.Temperature > 35.0)
        {
            _logger.LogWarning("High temperature alert! {Temperature}°C from {SensorId}",
                item.Temperature, item.SensorId);
        }

        return Task.CompletedTask;
    }

    // Handle humidity readings
    Task IAsyncObserver<HumidityReading>.OnNextAsync(HumidityReading item, StreamSequenceToken token)
    {
        _logger.LogInformation("Received humidity reading: {SensorId} = {Humidity}% at {Timestamp}",
            item.SensorId, item.Humidity, item.Timestamp);

        // Process the humidity reading
        if (item.Humidity > 65.0)
        {
            _logger.LogWarning("High humidity alert! {Humidity}% from {SensorId}",
                item.Humidity, item.SensorId);
        }

        return Task.CompletedTask;
    }

    Task IAsyncObserver<TemperatureReading>.OnCompletedAsync()
    {
        _logger.LogInformation("Temperature stream completed");
        return Task.CompletedTask;
    }

    Task IAsyncObserver<HumidityReading>.OnCompletedAsync()
    {
        _logger.LogInformation("Humidity stream completed");
        return Task.CompletedTask;
    }

    Task IAsyncObserver<TemperatureReading>.OnErrorAsync(Exception ex)
    {
        _logger.LogError(ex, "Error in temperature stream");
        return Task.CompletedTask;
    }

    Task IAsyncObserver<HumidityReading>.OnErrorAsync(Exception ex)
    {
        _logger.LogError(ex, "Error in humidity stream");
        return Task.CompletedTask;
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await StopConsuming();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}
