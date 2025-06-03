using EF_App.ApiService.DAL;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", async (AppDbContext db) =>
{
    var forecasts = await db.WeatherForecasts.ToListAsync();
    return forecasts.Select(f => new
    {
        f.Id,
        f.Date,
        f.TemperatureC,
        f.Summary,
        TemperatureF = 32 + (int)(f.TemperatureC / 0.5556)
    });
})
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

app.Run();

