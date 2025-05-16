using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());

// Configure more detailed console logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add HTTP logging to see detailed request information
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.All;
});

// Add services to the container.
builder.Services.AddControllers();

#pragma warning disable CA1416 // Validate platform compatibility
builder.WebHost.UseHttpSys(options =>
{
    options.UrlPrefixes.Add("https://127.0.0.1:5000");
});
#pragma warning restore CA1416 // Validate platform compatibility

var app = builder.Build();

app.Use((ctx, next) =>
{
    Console.WriteLine($"Request: {ctx.Request.Method} {ctx.Request.Path}");
    return next(ctx);
});

// Enable HTTP logging middleware
app.UseHttpLogging();

app.Map("/hello", () => "Hello World!");
app.Run();