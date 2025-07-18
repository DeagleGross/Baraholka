using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// builder = AzureStorageSetup(builder);
// builder = AzureAiFoundryLocalSetup(builder);
builder = AzureAiFoundrySetup(builder);

builder.Build().Run();

IDistributedApplicationBuilder AzureAiFoundryLocalSetup(IDistributedApplicationBuilder builder)
{
    var foundry = builder.AddAzureAIFoundry("foundry").RunAsFoundryLocal();
    // var chat = foundry.AddDeployment("chat", "qwen2.5-0.5b", "1", "Microsoft");
    var chat = foundry.AddDeployment("chat", "DeepSeek-R1-0528", "1", "Microsoft");

    var apiService = builder
        .AddProject<Projects.AspireDF_ApiService>("apiservice")
        .WithReference(chat)
        .WaitFor(chat);

    return builder;
}
IDistributedApplicationBuilder AzureAiFoundrySetup(IDistributedApplicationBuilder builder)
{
    var foundry = builder.AddAzureAIFoundry("foundry");
    var chat = foundry.AddDeployment("chat", "qwen2.5-0.5b", "1", "Microsoft");

    var apiService = builder
        .AddProject<Projects.AspireDF_ApiService>("apiservice")
        .WithReference(chat)
        .WaitFor(chat);

    return builder;
}


IDistributedApplicationBuilder AzureStorageSetup(IDistributedApplicationBuilder builder)
{
    var storage = builder.AddAzureStorage("eastus");


    var blobs = storage.AddBlobService("blobs");
    var blobTest = storage.AddBlobContainer("testcont", "testcont");

    var queues = storage.AddQueueService("queues");
    var eventsQ = storage.AddQueue("events", "events");

    var apiService = builder.AddProject<Projects.AspireDF_ApiService>("apiservice")
        .WithReference(blobs).WithReference(queues)
        .WaitFor(blobs)
        .WaitFor(queues);

    return builder;
}