using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddAzureChatCompletionsClient("chat")
       .AddChatClient();

// for azure storage setup in AppHost
//builder.AddAzureBlobServiceClient("blobs");
//builder.AddAzureQueueServiceClient("queues");

// Add services to the container.
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapGet("/", () => "Welcome to the AspireDF API Service!");

#region Azure AI Foundry

app.MapGet("/chat", async ([FromServices] IChatClient chatClient) =>
{
    var response = await chatClient.GetResponseAsync(new ChatMessage(ChatRole.User, content: "hello azure ai foundry!"));
    return response.RawRepresentation;
});

#endregion

//#region Azure Storage

//app.MapGet("/blobs", (BlobServiceClient client) =>
//{
//    var res = new List<string>();

//    var containers = client.GetBlobContainers();
//    foreach (var container in containers)
//    {
//        res.Add(JsonSerializer.Serialize(container, new JsonSerializerOptions() { WriteIndented = true }));
//    }

//    return res;
//});

//app.MapGet("/blobs/{id}", (BlobServiceClient client, string id) =>
//{
//    return client.CreateBlobContainer(id);
//});

//app.MapGet("/queues", (QueueServiceClient client) =>
//{
//    var res = new List<string>();

//    var queues = client.GetQueues();
//    foreach (var queue in queues)
//    {
//        res.Add(JsonSerializer.Serialize(queue, new JsonSerializerOptions() { WriteIndented = true }));
//    }

//    return res;
//});

//app.MapGet("/queues/{id}", (QueueServiceClient client, string id) =>
//{
//    return client.CreateQueue(id);
//});

//#endregion

app.MapDefaultEndpoints();

app.Run();