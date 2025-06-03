var builder = DistributedApplication.CreateBuilder(args);

var pg = builder.AddPostgres("weatherdb", port: 5432).AddDatabase("db");

var apiService = builder
    .AddProject<Projects.EF_App_ApiService>("apiservice")
    .WithReference(pg);

builder.AddProject<Projects.EF_App_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

/*
 * Create migration files. spins up snapshot and initial create
 * dotnet ef migrations add InitialCreate --project EF-App.ApiService --output-dir DAL/Migrations
 * 
 * Apply migrations to the actual database
 * dotnet ef database update --project EF-App.ApiService
 * 
 * For example i added a column to the WeatherForecast model
 * dotnet ef migrations add AddLocationToWeatherForecast --project EF-App.ApiService --output-dir DAL/Migrations
 * 
 * Apply the new migration to the database
 * dotnet ef database update --project EF-App.ApiService
 */