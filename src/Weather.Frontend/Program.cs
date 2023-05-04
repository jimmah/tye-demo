using Weather.Frontend;
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
    builder.Services.AddRazorPages();

    builder.Services.AddHttpClient<WeatherService>(x =>
    {
        x.BaseAddress = builder.Configuration.GetServiceUri("backend", "https");
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthorization();

    app.MapRazorPages();

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
