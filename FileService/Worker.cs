using Microsoft.ApplicationInsights;

namespace FileService
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

                try
                {
                    // Simulate worker service execution
                    await Task.Delay(1000, stoppingToken);

                    // Log telemetry data for a successful run
                    var duration = DateTime.UtcNow - startTime;
                    throw new Exception("This is intendend error throw");
                    _telemetryClient.TrackEvent("FileServiceExecutionSuccess", new Dictionary<string, string>
                    {
                        { "ServiceName", "FileServiceDemo" },
                        { "Timestamp", DateTime.UtcNow.ToString() },
                        { "ExecutionDuration", duration.TotalSeconds.ToString() }
                    });

                    // Track execution success count as a custom metric
                    _telemetryClient.GetMetric("FileServiceSuccessCount").TrackValue(1);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during service execution.");
                    // Optionally, track a failure metric or event here
                    _telemetryClient.GetMetric("FileServiceExecutionFailureCount").TrackValue(1);
                    _telemetryClient.TrackEvent("FileServiceExecutionFailure", new Dictionary<string, string>
                    {
                        { "ServiceName", "FileServiceDemo" },
                        { "Timestamp", DateTime.UtcNow.ToString() },
                        { "ErrorMessage", ex.Message }
                    });
                }

                await Task.Delay(1000, stoppingToken);  // Wait 5 seconds before next execution
            }
        }

    }
}
