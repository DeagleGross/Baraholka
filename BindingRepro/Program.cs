using System.Runtime.InteropServices;

Console.WriteLine($".NET Runtime Version: {RuntimeInformation.FrameworkDescription}");
Console.WriteLine($".NET Runtime Identifier: {RuntimeInformation.RuntimeIdentifier}");
Console.WriteLine($"Process Architecture: {RuntimeInformation.ProcessArchitecture}");
Console.WriteLine($"OS Architecture: {RuntimeInformation.OSArchitecture}");
Console.WriteLine("---");

Environment.SetEnvironmentVariable("ConnectionString", "Server=127.0.0.1;Database=test;Password=test");

var builder = WebApplication.CreateBuilder(args);

// Load custom configuration
var appSettings = new AppSettings();
builder.Configuration.Bind(appSettings);

var app = builder.Build();

var builderConfigConnectionString = builder.Configuration.AsEnumerable()
    .First(x => x.Key == "ConnectionString");

Console.WriteLine("builder.Configuration[\"ConnectionString\"]: " + builderConfigConnectionString.Value);
Console.WriteLine("AppSettings.ConnectionString: " + appSettings.ConnectionString);

app.MapGet("/", () => Results.Ok("OK!"));
app.Run();

public class AppSettings
{
    public string? ConnectionString { get; set; }
}

/*
 * Output :
   ---
    .NET Runtime Version: .NET 10.0.0-preview.7.25358.104
    .NET Runtime Identifier: win-x64
    Process Architecture: X64
    OS Architecture: X64
    ---
    builder.Configuration["ConnectionString"]: Server=127.0.0.1;Database=test;Password=test
    AppSettings.ConnectionString: this should be replaced!
 */