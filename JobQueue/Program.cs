
using Microsoft.EntityFrameworkCore;
using JobQueue.Data;
using JobQueue.Services;
using JobQueue.Workers;
using Serilog;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "JobQueue")
        .WriteTo.Console(
            outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] " +
                "{Application} {Message:lj} {Properties:j}{NewLine}{Exception}"
        )
);

// Database configuration
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<JobQueueDbContext>(options =>
    options.UseNpgsql(connectionString));

// API services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IJobsService, JobsService>();

// RabbitMQ services
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddSingleton<RabbitMqConsumer>();
builder.Services.AddSingleton<JobNotificationChannel>();

// Background worker
builder.Services.AddHostedService<JobProcessingWorker>();

// Health checks and metrics
builder.Services.AddHealthChecks();
builder.Services.AddMetrics();

// OpenTelemetry — must be registered before builder.Build()
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter();
    });

// Build application after all service registrations
var app = builder.Build();

// HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseAuthorization();

// Endpoints
app.MapPrometheusScrapingEndpoint();
app.MapControllers();

// Basic health check
app.MapHealthChecks("/health");

// Readiness check — verifies database connectivity
app.MapGet("/health/ready", async (JobQueueDbContext db) =>
{
    try
    {
        var connected = await db.Database.CanConnectAsync();

        return Results.Json(
            new
            {
                status = connected ? "Healthy" : "Unhealthy",
                database = connected ? "Connected" : "Unavailable"
            },
            statusCode: connected
                ? StatusCodes.Status200OK
                : StatusCodes.Status503ServiceUnavailable
        );
    }
    catch
    {
        return Results.Json(
            new
            {
                status = "Unhealthy",
                database = "Unavailable"
            },
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
    }
});

app.Run();
