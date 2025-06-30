var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddPostgres("pgsql").AddDatabase("db");

var apiService = builder.AddProject<Projects.AspireStarter_ApiService>("apiservice")
    .WaitFor(db)
    .WithCommand("cmd", "command display", async ctx => {
        Console.WriteLine("command execution");
        return new ExecuteCommandResult { Success = true };
    }, new CommandOptions())
    .WithCommand("cmd2", "command display 2", async ctx => {
        Console.WriteLine("command execution 2");
        return new ExecuteCommandResult { Success = true };
    }, new CommandOptions());

builder.AddProject<Projects.AspireStarter_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddExecutable("exec", "ping", workingDirectory: @"D:\code\", args: "google.com")
    .WaitFor(db);

builder.Build().Run();
