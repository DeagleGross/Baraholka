// requirements:
// run azurite for local emulator

using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("eastus");


var blobs = storage.AddBlobService("blobs");
var blobTest = storage.AddBlobContainer("testcont", "testcont");

var queues = storage.AddQueueService("queues");
var eventsQ = storage.AddQueue("events", "events");

var apiService = builder.AddProject<Projects.AspireDF_ApiService>("apiservice")
    .WithReference(blobs).WithReference(queues)
    .WaitFor(blobs)
    .WaitFor(queues);

//builder.AddProject<Projects.AspireDF_Web>("webfrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService)
//    .WaitFor(apiService);

builder.Build().Run();
