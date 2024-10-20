using MonitoringDemoApp;

var builder = Host.CreateApplicationBuilder(args);
// Register HttpClientFactory
//IConfiguration configuration = hostContext.Configuration;
builder.Services.AddHttpClient();
// Add Application Insights
builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
