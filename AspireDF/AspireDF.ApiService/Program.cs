using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddAzureBlobServiceClient("blobs");
builder.AddAzureQueueServiceClient("queues");

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Welcome to the AspireDF API Service!");

app.MapGet("/blobs", (BlobServiceClient client) =>
{
    var res = new List<string>();

    var containers = client.GetBlobContainers();
    foreach (var container in containers)
    {
        res.Add(JsonSerializer.Serialize(container, new JsonSerializerOptions() { WriteIndented = true }));
    }

    return res;
});

app.MapGet("/blobs/{id}", (BlobServiceClient client, string id) =>
{
    return client.CreateBlobContainer(id);
});

app.MapGet("/queues", (QueueServiceClient client) =>
{
    var res = new List<string>();

    var queues = client.GetQueues();
    foreach (var queue in queues)
    {
        res.Add(JsonSerializer.Serialize(queue, new JsonSerializerOptions() { WriteIndented = true }));
    }

    return res;
});

app.MapGet("/queues/{id}", (QueueServiceClient client, string id) =>
{
    return client.CreateQueue(id);
});


string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];
app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
