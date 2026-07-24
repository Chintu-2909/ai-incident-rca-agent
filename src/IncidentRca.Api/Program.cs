using System.Text.Json.Serialization;
using IncidentRca.Application.Abstractions;
using IncidentRca.Application.Configuration;
using IncidentRca.Infrastructure.Ai;
using IncidentRca.Infrastructure.Persistence;
using IncidentRca.Infrastructure.Seed;
using IncidentRca.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var dataDirectory = Path.Combine(
    builder.Environment.ContentRootPath,
    "data");

var logsDirectory = Path.Combine(
    builder.Environment.ContentRootPath,
    "logs");

Directory.CreateDirectory(dataDirectory);
Directory.CreateDirectory(logsDirectory);

var logFilePath = Path.Combine(
    logsDirectory,
    "incident-rca-agent-.log");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        logFilePath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting AI Incident Handoff and RCA Agent API");

    SQLitePCL.raw.SetProvider(
        new SQLitePCL.SQLite3Provider_sqlite3());

    SQLitePCL.raw.FreezeProvider();

    var configuredConnectionString =
        builder.Configuration.GetConnectionString("IncidentRca");

    var connectionString =
        string.IsNullOrWhiteSpace(configuredConnectionString)
            ? $"Data Source={Path.Combine(dataDirectory, "incident-rca.db")}"
            : configuredConnectionString.Replace(
                "{DataDirectory}",
                dataDirectory,
                StringComparison.Ordinal);

    builder.Services
        .AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter());
        });

    builder.Services.AddOpenApi();

    builder.Services.AddDbContext<IncidentRcaDbContext>(options =>
        options.UseSqlite(connectionString));

    builder.Services.AddScoped<DatabaseInitializer>();

    builder.Services.AddScoped<
        IIncidentAgentTools,
        IncidentAgentTools>();

    builder.Services.AddScoped<
        IIncidentInvestigationService,
        IncidentInvestigationService>();

    builder.Services
        .AddOptions<OllamaOptions>()
        .Bind(
            builder.Configuration.GetSection(
                OllamaOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddHttpClient<
        IAiAgentClient,
        OllamaAgentClient>(
        (serviceProvider, httpClient) =>
        {
            var options = serviceProvider
                .GetRequiredService<
                    IOptions<OllamaOptions>>()
                .Value;

            httpClient.BaseAddress =
                new Uri(options.BaseUrl);

            httpClient.Timeout =
                TimeSpan.FromSeconds(
                    options.TimeoutSeconds);
        });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<IncidentRcaDbContext>(
            name: "incident-rca-database");

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Dashboard", policy =>
        {
            policy
                .WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
    app.UseCors("Dashboard");

    app.MapControllers();
    app.MapHealthChecks("/health");

    await using (var scope = app.Services.CreateAsyncScope())
    {
        var initializer =
            scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

        await initializer.InitializeAsync();
    }

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(
        exception,
        "The AI Incident Handoff and RCA Agent API stopped unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
