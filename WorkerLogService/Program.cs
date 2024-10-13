using WorkerLogService;


var builder = Host.CreateApplicationBuilder(args);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetryWorkerService(config =>
{
    config.EnableDebugLogger = true; // Enable debug logging for Application Insights
});
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
