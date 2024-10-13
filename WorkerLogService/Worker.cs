using Microsoft.ApplicationInsights;

namespace WorkerLogService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TelemetryClient _telemetryClient;

        public Worker(ILogger<Worker> logger, TelemetryClient telemetryClient)
        {
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var startTime = DateTime.UtcNow;
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                // Simulate worker service execution
                await Task.Delay(1000, stoppingToken);

                // Log telemetry data to Application Insights for a successful run
                var duration = DateTime.UtcNow - startTime;
                _telemetryClient.TrackEvent("WorkerServiceExecutionSuccess", new Dictionary<string, string>
            {
                { "ServiceName", "WorkerServiceDemo" },
                { "Timestamp", DateTime.UtcNow.ToString() },
                { "ExecutionDuration", duration.TotalSeconds.ToString() }
            });

                // Track execution success count as a custom metric
                _telemetryClient.GetMetric("WorkerServiceSuccessCount").TrackValue(1);

                await Task.Delay(500, stoppingToken);  // Wait 5 seconds before next execution
            }
        }
    }
}
