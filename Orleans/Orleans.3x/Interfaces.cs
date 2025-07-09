// Event data models
using Orleans;
using Orleans.CodeGeneration;
using Orleans.Concurrency;

namespace Orleans3x.Interfaces;

[Immutable]
[Serializable]
public record TemperatureReading(
    string SensorId,
    double Temperature,
    DateTime Timestamp
);

// Grain interfaces
public interface IEventProducerGrain : IGrainWithIntegerKey
{
    Task StartProducing();
    Task StopProducing();
}

public interface IEventConsumerGrain : IGrainWithIntegerKey
{
    Task StartConsuming();
    Task StopConsuming();
}