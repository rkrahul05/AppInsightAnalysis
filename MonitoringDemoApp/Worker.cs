using Microsoft.ApplicationInsights;

namespace MonitoringDemoApp
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TelemetryClient _telemetryClient;
        private readonly HttpClient _httpClient;

        public Worker(ILogger<Worker> logger, TelemetryClient telemetryClient, HttpClient httpClient)
        {
            _logger = logger;
            _telemetryClient = telemetryClient;
            _httpClient = httpClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Execute Calculation Model
                    await ExecuteCalculationModel("Model1");

                    // Hit ASC API
                    await CallAscApi();

                    // Log success
                    _telemetryClient.TrackEvent("WorkerServiceExecutionSuccess");
                    _telemetryClient.GetMetric("WorkerServiceSuccessCount").TrackValue(1);

                    // Wait before the next execution cycle
                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    // Log failure and send notification
                    //_telemetryClient.TrackException(ex);
                    LogException(ex, "File worker service execution failed");
                    _telemetryClient.GetMetric("WorkerServiceFailCount").TrackValue(1);
                }
            }
        }
        private async Task ExecuteCalculationModel(string modelName)
        {
            try
            {
                // Simulate model execution
                _telemetryClient.TrackEvent("CalculationModelExecutionSuccess", properties: new Dictionary<string, string>{{ "ModelName", modelName }});

                Console.WriteLine($"Calculation Model '{modelName}' executed successfully.");
            }
            catch (Exception ex)
            {
                LogException(ex, "Calculation Model execution failed.", new Dictionary<string, string> { { "ModelName", modelName } });
                throw; // Rethrow to handle in ExecuteAsync
            }
        }

        private async Task CallAscApi()
        {
            try
            {
                // Simulate ASC API call
                var response = await _httpClient.GetAsync("http://localhost:5167/api/Data/AdditionalTag.json");

                _telemetryClient.TrackRequest("ASC API Call", DateTimeOffset.Now,
                    TimeSpan.FromMilliseconds(200), response.StatusCode.ToString(), response.IsSuccessStatusCode);

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"API Call failed with status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                LogException(ex, "ASC API call failed.", new Dictionary<string, string> { { "API", "ASC" } });
                throw; // Rethrow to handle in ExecuteAsync
            }
        }
        private void LogException(Exception ex, string message, IDictionary<string, string> additionalProperties = null)
        {
            var properties = new Dictionary<string, string>
            {
                { "Message", message },
                { "ExceptionType", ex.GetType().ToString() },
                { "ExceptionMessage", ex.Message },
                { "StackTrace", ex.StackTrace },
                { "TimeStamp", DateTime.UtcNow.ToString("o") }
            };

            // Include additional properties if any
            if (additionalProperties != null)
            {
                foreach (var kvp in additionalProperties)
                {
                    properties[kvp.Key] = kvp.Value;
                }
            }

            // Log to Application Insights
            _telemetryClient.TrackException(ex, properties);
            // Log to Console or other loggers as needed
            _logger.LogError(ex, message);
        }
    }

}
