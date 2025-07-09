using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;

Console.WriteLine($".NET Runtime Version: {RuntimeInformation.FrameworkDescription}");
Console.WriteLine($".NET Runtime Identifier: {RuntimeInformation.RuntimeIdentifier}");
Console.WriteLine($"Process Architecture: {RuntimeInformation.ProcessArchitecture}");
Console.WriteLine($"OS Architecture: {RuntimeInformation.OSArchitecture}");
Console.WriteLine("---");

var configurationManager = new ConfigurationManager();
configurationManager.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
configurationManager.AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionString", "expected value")]);

var appSettings = new AppSettings();
configurationManager.Bind(appSettings);

var builderConfigConnectionString = configurationManager.AsEnumerable()
    .First(x => x.Key == "ConnectionString");

Console.WriteLine("configurationManager[\"ConnectionString\"]: " + builderConfigConnectionString.Value);
Console.WriteLine("AppSettings.ConnectionString: " + appSettings.ConnectionString);


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
    configurationManager["ConnectionString"]: expected value
    AppSettings.ConnectionString: expected value
 */