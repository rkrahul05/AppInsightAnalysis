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
        private Timer _heartbeatTimer;

        public Worker(ILogger<Worker> logger, TelemetryClient telemetryClient, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _telemetryClient = telemetryClient;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Start the heartbeat every 5 minutes
            _heartbeatTimer = new Timer(SendHeartbeat, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

            var environment = _configuration.GetValue<string>("Environment") ?? "Unknown";
            while (!stoppingToken.IsCancellationRequested)
            {
                var startTime = DateTime.UtcNow;
                var correlationId = Guid.NewGuid().ToString();
                try
                {
                    
                    // Execute Calculation Model
                    await ExecuteCalculationModel("Model1");
                   
                    Console.WriteLine(environment);


                    // Hit ASC API
                    await CallAscApi();

                    // Log success
                    //_telemetryClient.TrackEvent("WorkerServiceExecutionSuccess10Minute");
                    //_telemetryClient.GetMetric("FileService.SuccessCount").TrackValue(1);

                    LogSuccess(correlationId, startTime, environment);

                    // Wait before the next execution cycle
                    Console.WriteLine("I got executed after ****************************************************");
                    
                }
                catch (Exception ex)
                {
                    // Log failure and send notification
                    //_telemetryClient.TrackException(ex);
                    //LogException(ex, "File worker service execution failed");
                    LogFailure(ex, correlationId, environment);
                    //_telemetryClient.GetMetric("FileService.FailureCount").TrackValue(1);
                }
                await Task.Delay(36000, stoppingToken);
            }
        }

        private void SendHeartbeat(object state)
        {
            Console.WriteLine($"Sending heartbeat in every 10sec {DateTime.UtcNow}");
            var properties = new Dictionary<string, string>
            {
                { "ServiceName", "AscFileService" },
                { "Environment", _configuration.GetValue<string>("Environment") ?? "Unknown" },
                { "Status", "Running" }
            };

            //_telemetryClient.TrackEvent("AscFileServiceHeartbeat", properties);
            //_telemetryClient.GetMetric("AscFileService.HeartbeatCount").TrackValue(1);
            _telemetryClient.GetMetric("AscHistoricalFileService.HeartbeatCount").TrackValue(1);
            _telemetryClient.GetMetric("AscHistoricalSensorDataService.HeartbeatCount").TrackValue(1);
            _telemetryClient.GetMetric("AscSensorDataService.HeartbeatCount").TrackValue(1);
            _telemetryClient.GetMetric("CmsSensorDataService.HeartbeatCount").TrackValue(1);
            _telemetryClient.GetMetric("DataProcesssor.HeartbeatCount").TrackValue(1);
            _telemetryClient.GetMetric("HistoricalDataProcessor.HeartbeatCount").TrackValue(1);


        }
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker is stopping at: {time}", DateTimeOffset.Now);
            _heartbeatTimer?.Dispose();
            await base.StopAsync(stoppingToken);
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
        private void LogSuccess(string correlationId, DateTime startTime, string environment)
        {
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "Job succeeded: AscFileService | CorrelationId: {CorrelationId} | Environment: {Environment} | Duration: {DurationMs}ms",
                correlationId, environment, duration.TotalMilliseconds);

            _telemetryClient.TrackEvent("AscFileService.Success",
                new System.Collections.Generic.Dictionary<string, string>
                {
                    { "CorrelationId", correlationId },
                    { "JobName", "AscFileService" },
                    { "Environment", environment },
                    { "Status", "Success" },
                    { "ExecutionStartTime", startTime.ToString("o") },
                    { "ExecutionEndTime", DateTime.UtcNow.ToString("o") },
                    { "Duration", duration.TotalMilliseconds.ToString() + "ms" }
                });

            _telemetryClient.GetMetric("AscFileService.SuccessCount").TrackValue(1);
        }
        private void LogFailure(Exception ex, string correlationId, string environment)
        {
            var timestamp = DateTime.UtcNow;

            _logger.LogError(ex,
                "Job failed: AscFileService | CorrelationId: {CorrelationId} | Environment: {Environment} | Error: {ErrorMessage} | Timestamp: {Timestamp}",
                correlationId, environment, ex.Message, timestamp);

            var exceptionTelemetry = new ExceptionTelemetry(ex)
            {
                Message = "Error while fetching or uploading live data from ASC API to Blob Storage",
                SeverityLevel = SeverityLevel.Error,
                Timestamp = timestamp
            };
            exceptionTelemetry.Properties.Add("CorrelationId", correlationId);
            exceptionTelemetry.Properties.Add("JobName", "AscFileService");
            exceptionTelemetry.Properties.Add("Environment", environment);
            exceptionTelemetry.Properties.Add("ErrorMessage", ex.Message);
            exceptionTelemetry.Properties.Add("StackTrace", ex.StackTrace ?? "N/A");

            _telemetryClient.TrackException(exceptionTelemetry);
            _telemetryClient.GetMetric("AscFileService.FailureCount").TrackValue(1);
        }
    }

}
