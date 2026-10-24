
using Microsoft.EntityFrameworkCore;
using JobQueue.Data;
using JobQueue.Services;
using JobQueue.Workers;
using Serilog;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;


var builder = WebApplication.CreateBuilder(args);

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

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<JobQueueDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IJobsService, JobsService>();

builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddSingleton<RabbitMqConsumer>();
builder.Services.AddSingleton<JobNotificationChannel>();

builder.Services.AddHostedService<JobProcessingWorker>();

builder.Services.AddHealthChecks();
builder.Services.AddMetrics();

var app = builder.Build();



builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter();
    });

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
app.MapPrometheusScrapingEndpoint();
app.MapControllers();


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
