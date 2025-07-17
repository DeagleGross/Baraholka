// requirements:
// run azurite for local emulator

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("eastus");
var blobs = storage.AddBlobService("blobs");

var apiService = builder.AddProject<Projects.AspireDF_ApiService>("apiservice")
    .WaitFor(blobs);

//builder.AddProject<Projects.AspireDF_Web>("webfrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService)
//    .WaitFor(apiService);

builder.Build().Run();
