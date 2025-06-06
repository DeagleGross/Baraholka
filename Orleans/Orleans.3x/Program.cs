using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans(static siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("urls");
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

app.Run();

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
