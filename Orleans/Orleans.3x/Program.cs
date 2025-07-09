using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans3x.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(static siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("urls");

    siloBuilder
        .AddAzureQueueStreams(
            "AzureQueueProvider",
            configurator =>
            {
                configurator.ConfigureAzureQueue(ob => ob.Configure(options =>
                {
                    // Use Azurite for local development
                    options.ConfigureQueueServiceClient("UseDevelopmentStorage=true");
                    options.QueueNames = new List<string>
                    {
                        "stream-3x"
                    };
                }));

                configurator.ConfigureCacheSize(1024);
                configurator.ConfigurePullingAgent(ob => ob.Configure(options =>
                {
                    options.GetQueueMsgsTimerPeriod = TimeSpan.FromMilliseconds(100);
                }));
            })
        // Add memory grain storage for PubSub subscriptions
        .AddMemoryGrainStorage("PubSubStore");
});

using var app = builder.Build();

app.MapGet("/", static () => "Welcome to the URL shortener, powered by Orleans!");

app.MapGet("/shorten", static async (IGrainFactory grains, HttpRequest request, string url) =>
{
    var host = $"{request.Scheme}://{request.Host.Value}";

    // Validate the URL query string.
    if (string.IsNullOrWhiteSpace(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute) is false)
    {
        return Results.BadRequest($"""
            The URL query string is required and needs to be well formed.
            Consider, ${host}/shorten?url=https://www.microsoft.com.
            """);
    }

    var shortenedRouteSegment = Guid.NewGuid().GetHashCode().ToString("X");

    var shortenerGrain = grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);
    await shortenerGrain.SetUrl(url);

    var resultBuilder = new UriBuilder(host)
    {
        Path = $"/go/{shortenedRouteSegment}"
    };

    return Results.Ok(resultBuilder.Uri);
});

app.MapGet("/go/{shortenedRouteSegment:required}", static async (IGrainFactory grains, string shortenedRouteSegment) =>
{
    // Retrieve the grain using the shortened ID and url to the original URL
    var shortenerGrain =
        grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);

    var url = await shortenerGrain.GetUrl();

    // Handles missing schemes, defaults to "http://".
    var redirectBuilder = new UriBuilder(url);

    return Results.Redirect(redirectBuilder.Uri.ToString());
});

await app.StartAsync();

var streamProvider = app.Services.GetRequiredServiceByName<IStreamProvider>("AzureQueueProvider");
var stream = streamProvider.GetStream<TemperatureReading>(Guid.NewGuid(), "qwe");

// STREAMING
var grainFactory = app.Services.GetRequiredService<IGrainFactory>();

// Create producer and consumer grains
var producerGrain = grainFactory.GetGrain<IEventProducerGrain>(0);
//var consumerGrain = grainFactory.GetGrain<IEventConsumerGrain>(0);

//// Start consumer (it will subscribe to the stream)
//await consumerGrain.StartConsuming();

// Start producing events
await producerGrain.StartProducing();

// Keep the application running
Console.WriteLine("Press any key to stop producing events and exit...");
Console.ReadKey();

await app.StopAsync();

public interface IUrlShortenerGrain : IGrainWithStringKey
{
    Task SetUrl(string fullUrl);

    Task<string> GetUrl();
}

public sealed class UrlShortenerGrain([PersistentState(stateName: "url", storageName: "urls")] IPersistentState<UrlDetails> state)
    : Grain, IUrlShortenerGrain
{
    public async Task SetUrl(string fullUrl)
    {
        state.State = new()
        {
            ShortenedRouteSegment = this.GetPrimaryKeyString(),
            FullUrl = fullUrl
        };

        await state.WriteStateAsync();
    }

    public Task<string> GetUrl() =>
        Task.FromResult(state.State.FullUrl);
}

// [GenerateSerializer(typeof(UrlDetails))]
public sealed record class UrlDetails
{
    // [Id(0)]
    public string FullUrl { get; set; } = "";

    // [Id(1)]
    public string ShortenedRouteSegment { get; set; } = "";
}
