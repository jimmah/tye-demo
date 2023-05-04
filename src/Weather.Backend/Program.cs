using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Exceptions;


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, loggerConfig) =>
    {
        loggerConfig
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
            .Enrich.WithEnvironmentName()
            .Enrich.WithExceptionDetails()
            .Enrich.WithSpan()
            .ReadFrom.Configuration(builder.Configuration)
            .WriteTo.Console();

        var seq = builder.Configuration.GetServiceUri("seq");
        if (seq != null)
        {
            Log.Information($"Logging to Seq at: {seq.AbsoluteUri}");

            loggerConfig.WriteTo.Seq(seq.AbsoluteUri);
        }
    });

    // Add services to the container.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddStackExchangeRedisCache(x =>
    {
        x.Configuration = builder.Configuration.GetConnectionString("redis");
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    var summaries = new[]
    {
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

    app.MapGet("/weatherforecast", async (IDistributedCache cache) =>
    {
        var forecastJson = await cache.GetStringAsync("weather");

        WeatherForecast[]? forecast;

        if (forecastJson == null)
        {
            forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();

            forecastJson = JsonSerializer.Serialize(forecast);

            await cache.SetStringAsync("weather", forecastJson, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
            });
        }
        else
        {
            forecast = JsonSerializer.Deserialize<WeatherForecast[]>(forecastJson);
        }

        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception occurred");
}
finally
{
    Log.Information("Shutdown complete");
    Log.CloseAndFlush();
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
