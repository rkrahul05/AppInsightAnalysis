using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace MonitoringDemoApp
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TelemetryClient _telemetryClient;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, TelemetryClient telemetryClient, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _telemetryClient = telemetryClient;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {

                    // Execute Calculation Model
                    await ExecuteCalculationModel("Model1");
                    var environment = _configuration.GetValue<string>("Environment") ?? "Unknown";
                    Console.WriteLine(environment);


                    // Hit ASC API
                    await CallAscApi();

                    // Log success
                    _telemetryClient.TrackEvent("WorkerServiceExecutionSuccess10Minute");
                    _telemetryClient.GetMetric("WorkerServiceSuccessCount10Minute").TrackValue(1);

                    // Wait before the next execution cycle
                    await Task.Delay(3000, stoppingToken);
                }
                catch (Exception ex)
                {
                    // Log failure and send notification
                    //_telemetryClient.TrackException(ex);
                    //LogException(ex, "File worker service execution failed");
                    _telemetryClient.GetMetric("WorkerServiceFailCount10Minute").TrackValue(1);
                }
            }
        }
        private async Task ExecuteCalculationModel(string modelName)
        {
            try
            {
                //int a = 1;
                //int b = 0;
                //var temp = a / b;
                // Simulate model execution
                _telemetryClient.TrackEvent("CalculationModelExecutionSuccess", properties: new Dictionary<string, string>{{ "ModelName", modelName }});

                Console.WriteLine($"Calculation Model '{modelName}' executed successfully.");
            }
            catch (Exception ex)
            {
                //LogException(ex, "Calculation Model execution failed.", new Dictionary<string, string> { { "ModelName", modelName } });
                throw; // Rethrow to handle in ExecuteAsync
            }
        }

        private async Task CallAscApi()
        {
            var correlationId = Guid.NewGuid().ToString();
            // Start tracking the operation
            using (var operation = _telemetryClient.StartOperation<DependencyTelemetry>("ASC API Call"))
            {
                operation.Telemetry.Type = "HTTP"; // Specify the type of dependency
                operation.Telemetry.Target = "http://localhost:5167"; // The target service
                operation.Telemetry.Data = "GET /api/Data/AdditionalTag.json"; // The specific call made
                operation.Telemetry.Timestamp = DateTimeOffset.UtcNow;
                operation.Telemetry.Properties["StartDate"] = DateTimeOffset.UtcNow.AddDays(-1).ToString("o");
                operation.Telemetry.Properties["EndDate"] = DateTimeOffset.UtcNow.AddDays(2).ToString("o");

                try
                {
                    // Start time for tracking
                    var startTime = DateTimeOffset.UtcNow;
                    _telemetryClient.GetMetric("AscApiHitCount").TrackValue(1);
                    // Simulate ASC API call
                    var response = await _httpClient.GetAsync("http://localhost:5167/api/Data/AdditionalTag.json");

                    // Set the duration
                    operation.Telemetry.Duration = DateTimeOffset.UtcNow - startTime;

                    // Set the success status and include the response code
                    operation.Telemetry.Success = response.IsSuccessStatusCode;
                    operation.Telemetry.ResultCode = response.StatusCode.ToString();
                    
                    // Check if the response indicates failure
                    if (response.IsSuccessStatusCode)
                    {
                        _telemetryClient.TrackEvent(
                           "AscApiCallSuccess",
                           new System.Collections.Generic.Dictionary<string, string>
                           {
                                { "ApiUrl", "http://localhost:5167/api/Data/AdditionalTag.json" },
                                { "CorrelationId", correlationId },
                                { "Status", "Success" },
                                { "DurationMs", "10ms" },
                                { "ApiStatusCode", response.StatusCode.ToString() },
                           });

                        _telemetryClient.GetMetric("AscApiSuccessCount").TrackValue(1);
                        
                    }
                    _telemetryClient.TrackEvent(
                       "AscApiCallEmptyResponse",
                       new System.Collections.Generic.Dictionary<string, string>
                       {
                             { "ApiUrl", "http://localhost:5167/api/Data/AdditionalTag.json" },
                             { "CorrelationId", correlationId },
                             { "Status", "Empty Response" },
                             { "StatusCode", ((int)response.StatusCode).ToString() },
                             { "DurationMs", "100ms" },
                       });

                }
                catch (Exception ex)
                {
                    // Log the exception with the telemetry client
                    _telemetryClient.GetMetric("AscApiFailureCount").TrackValue(1);
                    //LogException(ex, "ASC API call failed.", new Dictionary<string, string> { { "API", "ASC" } });
                    var timestamp = DateTime.UtcNow;
                    var fullUrl = "http://localhost:5167/api/Data/AdditionalTag.json";
                    var exceptionTelemetry = new ExceptionTelemetry(ex)
                    {
                        Message = $"Error calling ASC API at {fullUrl}",
                        SeverityLevel = SeverityLevel.Error,
                        Timestamp = timestamp,
                    };
                    exceptionTelemetry.Exception = ex;  
                    exceptionTelemetry.Properties.Add("ApiUrl", fullUrl);
                    exceptionTelemetry.Properties.Add("CorrelationId", correlationId);
                    exceptionTelemetry.Properties.Add("StartDate", DateTimeOffset.UtcNow.AddDays(-1).ToString("o"));
                    exceptionTelemetry.Properties.Add("EndDate", DateTimeOffset.UtcNow.AddDays(1).ToString("o"));
                    exceptionTelemetry.Properties.Add("FrameSerialNumbers", string.Join(", ", "F12345"));
                    exceptionTelemetry.Properties.Add("StackTrace", ex.StackTrace ?? "N/A");

                    _telemetryClient.TrackException(exceptionTelemetry);
                    operation.Telemetry.Success = false; // Mark the operation as failed
                    throw; // Rethrow to handle in ExecuteAsync
                }
            } // The operation will be automatically tracked here
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
