using System.Text;
using System.Net;
using System.Net.Security;
using Newtonsoft.Json;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;

namespace WorkerEndpointTests;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static readonly string baseUrl = "https://localhost:57652";
    private static HubConnection? hubConnection;
    private static readonly List<string> signalREvents = new List<string>();
    private static readonly List<TestResult> testResults = new List<TestResult>();
    private static int testCounter = 0;

    static async Task Main(string[] args)
    {
        Console.WriteLine("🔧 COMPREHENSIVE WORKER API & SIGNALR TESTS");
        Console.WriteLine("=============================================");
        Console.WriteLine($"🎯 Target API: {baseUrl}");
        Console.WriteLine($"⏰ Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        var stopwatch = Stopwatch.StartNew();

        // Ignore SSL certificate errors for testing
        httpClient.DefaultRequestHeaders.Add("User-Agent", "WorkerEndpointTests/1.0");
        ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

        try
        {
            // Phase 1: Basic API Health Tests
            Console.WriteLine("🏥 PHASE 1: API HEALTH & CONNECTIVITY TESTS");
            Console.WriteLine("============================================");
            await RunTest("API Health Check", TestApiHealth);
            await RunTest("Worker Status Endpoint", TestGetWorkerStatus);

            // Phase 2: SignalR Connection Tests
            Console.WriteLine("\n📡 PHASE 2: SIGNALR CONNECTION TESTS");
            Console.WriteLine("====================================");
            await RunTest("SignalR Connection Establishment", InitializeSignalRConnection);
            await RunTest("SignalR Group Operations", TestSignalRGroupOperations);

            // Phase 3: Worker Integration Tests
            Console.WriteLine("\n🧪 PHASE 3: WORKER INTEGRATION TESTS");
            Console.WriteLine("====================================");
            await RunTest("Worker Process Management Test", TestWorkerProcessManagementTest);
            await RunTest("Indicator Execution Test", TestIndicatorExecutionTest);
            await RunTest("Stress Test", TestStressTest);
            await RunTest("Real-time Monitoring Test", TestRealTimeMonitoringTest);
            await RunTest("Worker Lifecycle Test", TestWorkerLifecycleTest);

            // Phase 4: Test Management Tests
            Console.WriteLine("\n🛠️ PHASE 4: TEST MANAGEMENT TESTS");
            Console.WriteLine("==================================");
            await RunTest("Test Results Retrieval", TestGetTestResults);
            await RunTest("Individual Test Stopping", TestStopSpecificTest);
            await RunTest("All Tests Stopping", TestStopAllTests);

            // Phase 5: SignalR Cleanup Tests
            Console.WriteLine("\n🔌 PHASE 5: SIGNALR CLEANUP TESTS");
            Console.WriteLine("==================================");
            await RunTest("SignalR Event Analysis", AnalyzeSignalREvents);
            await RunTest("SignalR Disconnection", TestSignalRDisconnection);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Critical test execution failure: {ex.Message}");
            Console.WriteLine($"📍 Stack trace: {ex.StackTrace}");
            RecordTestResult("Critical Error", false, ex.Message);
        }
        finally
        {
            stopwatch.Stop();
            await CleanupSignalRConnection();
            httpClient.Dispose();
            PrintTestSummary(stopwatch.Elapsed);
        }

        Console.WriteLine("\n⌨️  Press any key to exit...");
        Console.ReadKey();
    }

    // Helper classes
    public class TestResult
    {
        public string TestName { get; set; } = "";
        public bool Passed { get; set; }
        public string Message { get; set; } = "";
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Helper methods
    static async Task RunTest(string testName, Func<Task> testAction)
    {
        testCounter++;
        Console.WriteLine($"\n🧪 Test {testCounter}: {testName}");
        Console.WriteLine(new string('-', 50));

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await testAction();
            stopwatch.Stop();
            RecordTestResult(testName, true, "Passed", stopwatch.Elapsed);
            Console.WriteLine($"✅ {testName} - PASSED ({stopwatch.ElapsedMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordTestResult(testName, false, ex.Message, stopwatch.Elapsed);
            Console.WriteLine($"❌ {testName} - FAILED: {ex.Message} ({stopwatch.ElapsedMilliseconds}ms)");
        }
    }

    static void RecordTestResult(string testName, bool passed, string message, TimeSpan? duration = null)
    {
        testResults.Add(new TestResult
        {
            TestName = testName,
            Passed = passed,
            Message = message,
            Duration = duration ?? TimeSpan.Zero,
            Timestamp = DateTime.Now
        });
    }

    static void PrintTestSummary(TimeSpan totalDuration)
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("📊 COMPREHENSIVE TEST SUMMARY");
        Console.WriteLine(new string('=', 60));

        var passed = testResults.Count(r => r.Passed);
        var failed = testResults.Count(r => !r.Passed);
        var total = testResults.Count;

        Console.WriteLine($"📈 Total Tests: {total}");
        Console.WriteLine($"✅ Passed: {passed}");
        Console.WriteLine($"❌ Failed: {failed}");
        Console.WriteLine($"📊 Success Rate: {(passed * 100.0 / total):F1}%");
        Console.WriteLine($"⏱️ Total Duration: {totalDuration.TotalSeconds:F2} seconds");
        Console.WriteLine($"📡 SignalR Events Received: {signalREvents.Count}");

        if (failed > 0)
        {
            Console.WriteLine("\n❌ FAILED TESTS:");
            foreach (var result in testResults.Where(r => !r.Passed))
            {
                Console.WriteLine($"   • {result.TestName}: {result.Message}");
            }
        }

        if (signalREvents.Count > 0)
        {
            Console.WriteLine("\n📡 SIGNALR EVENTS SUMMARY:");
            var eventGroups = signalREvents.GroupBy(e => e.Split(':')[0]).ToList();
            foreach (var group in eventGroups)
            {
                Console.WriteLine($"   • {group.Key}: {group.Count()} events");
            }
        }
    }

    static async Task InitializeSignalRConnection()
    {
        Console.WriteLine("🔌 Establishing SignalR connection...");

        hubConnection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/worker-integration-test", options =>
            {
                options.HttpMessageHandlerFactory = _ => new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
            })
            .WithAutomaticReconnect()
            .Build();

        // Set up comprehensive event handlers
        hubConnection.On<object>("WorkerTestStarted", (data) =>
        {
            var eventMsg = $"WorkerTestStarted: {JsonConvert.SerializeObject(data)}";
            signalREvents.Add(eventMsg);
            Console.WriteLine($"📡 {eventMsg}");
        });

        hubConnection.On<object>("WorkerTestProgress", (data) =>
        {
            var eventMsg = $"WorkerTestProgress: {JsonConvert.SerializeObject(data)}";
            signalREvents.Add(eventMsg);
            Console.WriteLine($"📡 {eventMsg}");
        });

        hubConnection.On<object>("WorkerTestCompleted", (data) =>
        {
            var eventMsg = $"WorkerTestCompleted: {JsonConvert.SerializeObject(data)}";
            signalREvents.Add(eventMsg);
            Console.WriteLine($"📡 {eventMsg}");
        });

        hubConnection.On<object>("WorkerTestStopped", (data) =>
        {
            var eventMsg = $"WorkerTestStopped: {JsonConvert.SerializeObject(data)}";
            signalREvents.Add(eventMsg);
            Console.WriteLine($"📡 {eventMsg}");
        });

        hubConnection.On<object>("IndicatorTestResult", (data) =>
        {
            var eventMsg = $"IndicatorTestResult: {JsonConvert.SerializeObject(data)}";
            signalREvents.Add(eventMsg);
            Console.WriteLine($"📡 {eventMsg}");
        });

        // Connection state event handlers
        hubConnection.Closed += async (error) =>
        {
            var eventMsg = $"Connection Closed: {error?.Message ?? "Normal closure"}";
            signalREvents.Add(eventMsg);
            Console.WriteLine($"📡 {eventMsg}");
        };

        hubConnection.Reconnecting += async (error) =>
        {
            var eventMsg = $"Reconnecting: {error?.Message ?? "Unknown reason"}";
            signalREvents.Add(eventMsg);
            Console.WriteLine($"📡 {eventMsg}");
        };

        hubConnection.Reconnected += async (connectionId) =>
        {
            var eventMsg = $"Reconnected: {connectionId}";
            signalREvents.Add(eventMsg);
            Console.WriteLine($"📡 {eventMsg}");
        };

        await hubConnection.StartAsync();
        Console.WriteLine($"✅ SignalR connected (State: {hubConnection.State})");
    }

    static async Task TestApiHealth()
    {
        Console.WriteLine("🏥 Testing API health endpoint...");
        var response = await httpClient.GetAsync($"{baseUrl}/api/health");
        var content = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Response: {FormatJson(content)}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"API health check failed: {response.StatusCode}");
        }
    }

    static async Task TestSignalREventSubscription()
    {
        Console.WriteLine("📡 Testing SignalR event subscription...");

        if (hubConnection?.State != HubConnectionState.Connected)
        {
            throw new Exception("SignalR connection not established");
        }

        // Subscribe to worker test events
        await hubConnection.InvokeAsync("SubscribeToWorkerTests");
        Console.WriteLine("✅ Subscribed to worker test events");

        // Test group joining
        await hubConnection.InvokeAsync("JoinGroup", "TestGroup");
        Console.WriteLine("✅ Joined test group");
    }

    static async Task TestSignalRGroupOperations()
    {
        Console.WriteLine("👥 Testing SignalR group operations...");

        if (hubConnection?.State != HubConnectionState.Connected)
        {
            throw new Exception("SignalR connection not established");
        }

        // Test joining multiple groups
        var testGroups = new[] { "Group1", "Group2", "Group3" };

        foreach (var group in testGroups)
        {
            await hubConnection.InvokeAsync("JoinGroup", group);
            Console.WriteLine($"✅ Joined group: {group}");
        }

        // Test leaving groups
        foreach (var group in testGroups)
        {
            await hubConnection.InvokeAsync("LeaveGroup", group);
            Console.WriteLine($"✅ Left group: {group}");
        }
    }

    static async Task CleanupSignalRConnection()
    {
        if (hubConnection != null)
        {
            try
            {
                // Unsubscribe from events
                if (hubConnection.State == HubConnectionState.Connected)
                {
                    await hubConnection.InvokeAsync("UnsubscribeFromWorkerTests");
                    Console.WriteLine("📡 Unsubscribed from worker test events");
                }

                await hubConnection.StopAsync();
                await hubConnection.DisposeAsync();
                Console.WriteLine("🔌 SignalR connection closed gracefully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error closing SignalR connection: {ex.Message}");
            }
        }
    }

    static async Task TestSignalRDisconnection()
    {
        Console.WriteLine("🔌 Testing SignalR disconnection...");

        if (hubConnection?.State == HubConnectionState.Connected)
        {
            await hubConnection.StopAsync();
            Console.WriteLine($"✅ SignalR disconnected (State: {hubConnection.State})");
        }
        else
        {
            Console.WriteLine("⚠️ SignalR was not connected");
        }
    }

    static async Task AnalyzeSignalREvents()
    {
        Console.WriteLine("📊 Analyzing SignalR events...");

        Console.WriteLine($"📡 Total events received: {signalREvents.Count}");

        if (signalREvents.Count == 0)
        {
            Console.WriteLine("⚠️ No SignalR events were received during testing");
            return;
        }

        var eventTypes = signalREvents
            .Select(e => e.Split(':')[0])
            .GroupBy(e => e)
            .ToDictionary(g => g.Key, g => g.Count());

        Console.WriteLine("📈 Event breakdown:");
        foreach (var kvp in eventTypes)
        {
            Console.WriteLine($"   • {kvp.Key}: {kvp.Value} events");
        }

        // Check for expected events
        var expectedEvents = new[] { "WorkerTestStarted", "WorkerTestProgress", "WorkerTestCompleted" };
        var missingEvents = expectedEvents.Where(e => !eventTypes.ContainsKey(e)).ToList();

        if (missingEvents.Any())
        {
            Console.WriteLine($"⚠️ Missing expected events: {string.Join(", ", missingEvents)}");
        }
        else
        {
            Console.WriteLine("✅ All expected event types were received");
        }
    }

    static async Task TestWorkerProcessManagementTest()
    {
        Console.WriteLine("🔧 Starting Worker Process Management Test...");

        var testRequest = new
        {
            TestType = "worker-process-management",
            DurationMinutes = 2,
            WorkerCount = 3,
            IndicatorIds = new int[] { },
            ConcurrentWorkers = 3
        };

        var testId = await StartTestAndGetId(testRequest);
        Console.WriteLine($"✅ Test started with ID: {testId}");

        // Monitor for 45 seconds
        await MonitorTestProgress(testId, TimeSpan.FromSeconds(45));
    }

    static async Task TestIndicatorExecutionTest()
    {
        Console.WriteLine("📊 Starting Indicator Execution Test...");

        var testRequest = new
        {
            TestType = "indicator-execution",
            DurationMinutes = 1,
            WorkerCount = 1,
            IndicatorIds = new int[] { },
            ConcurrentWorkers = 1
        };

        var testId = await StartTestAndGetId(testRequest);
        Console.WriteLine($"✅ Test started with ID: {testId}");

        // Monitor for 30 seconds
        await MonitorTestProgress(testId, TimeSpan.FromSeconds(30));
    }

    static async Task TestStressTest()
    {
        Console.WriteLine("💪 Starting Stress Test...");

        var testRequest = new
        {
            TestType = "stress-test",
            DurationMinutes = 1,
            WorkerCount = 5,
            IndicatorIds = new int[] { },
            ConcurrentWorkers = 5
        };

        var testId = await StartTestAndGetId(testRequest);
        Console.WriteLine($"✅ Test started with ID: {testId}");

        // Monitor for 30 seconds
        await MonitorTestProgress(testId, TimeSpan.FromSeconds(30));
    }

    static async Task TestRealTimeMonitoringTest()
    {
        Console.WriteLine("📡 Starting Real-time Monitoring Test...");

        var testRequest = new
        {
            TestType = "real-time-monitoring",
            DurationMinutes = 1,
            WorkerCount = 2,
            IndicatorIds = new int[] { },
            ConcurrentWorkers = 2
        };

        var testId = await StartTestAndGetId(testRequest);
        Console.WriteLine($"✅ Test started with ID: {testId}");

        // Monitor for 30 seconds
        await MonitorTestProgress(testId, TimeSpan.FromSeconds(30));
    }

    static async Task TestWorkerLifecycleTest()
    {
        Console.WriteLine("🔄 Starting Worker Lifecycle Test...");

        var testRequest = new
        {
            TestType = "worker-lifecycle",
            DurationMinutes = 1,
            WorkerCount = 2,
            IndicatorIds = new int[] { },
            ConcurrentWorkers = 2
        };

        var testId = await StartTestAndGetId(testRequest);
        Console.WriteLine($"✅ Test started with ID: {testId}");

        // Monitor for 30 seconds
        await MonitorTestProgress(testId, TimeSpan.FromSeconds(30));
    }

    static async Task<string> StartTestAndGetId(object testRequest)
    {
        var json = JsonConvert.SerializeObject(testRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync($"{baseUrl}/api/worker-integration-test/start", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Response: {FormatJson(responseContent)}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to start test: {response.StatusCode}");
        }

        dynamic responseObj = JsonConvert.DeserializeObject(responseContent)!;
        return responseObj.data.testId;
    }

    static async Task MonitorTestProgress(string testId, TimeSpan duration)
    {
        Console.WriteLine($"⏱️ Monitoring test {testId} for {duration.TotalSeconds} seconds...");

        var endTime = DateTime.Now.Add(duration);
        var lastProgress = -1;

        while (DateTime.Now < endTime)
        {
            try
            {
                // Get current status
                var response = await httpClient.GetAsync($"{baseUrl}/api/worker-integration-test/status");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    dynamic statusObj = JsonConvert.DeserializeObject(content)!;

                    // Find our test in recent executions
                    if (statusObj.data.recentExecutions != null)
                    {
                        foreach (var execution in statusObj.data.recentExecutions)
                        {
                            if (execution.id == testId)
                            {
                                var progress = (int)execution.progress;
                                if (progress != lastProgress)
                                {
                                    Console.WriteLine($"📈 Test {testId} progress: {progress}% (Status: {execution.status})");
                                    lastProgress = progress;
                                }

                                // Fix JSON parsing issue with boolean values
                                var isRunning = execution.isRunning;
                                bool isTestRunning = false;
                                if (isRunning != null)
                                {
                                    if (isRunning is bool boolValue)
                                    {
                                        isTestRunning = boolValue;
                                    }
                                    else if (bool.TryParse(isRunning.ToString(), out bool parsedValue))
                                    {
                                        isTestRunning = parsedValue;
                                    }
                                }

                                if (!isTestRunning)
                                {
                                    Console.WriteLine($"🏁 Test {testId} completed with status: {execution.status}");
                                    return;
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error monitoring test: {ex.Message}");
            }

            await Task.Delay(2000); // Check every 2 seconds
        }

        Console.WriteLine($"⏰ Monitoring timeout reached for test {testId}");
    }

    static async Task TestGetWorkerStatus()
    {
        Console.WriteLine("📊 Testing Worker Status endpoint...");

        var response = await httpClient.GetAsync($"{baseUrl}/api/worker/status");
        var content = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Response: {FormatJson(content)}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Worker status request failed: {response.StatusCode}");
        }

        // Validate response structure
        dynamic statusObj = JsonConvert.DeserializeObject(content)!;
        if (statusObj.data == null)
        {
            throw new Exception("Response missing data field");
        }

        Console.WriteLine($"✅ Worker Mode: {statusObj.data.mode}");
        Console.WriteLine($"✅ Worker Running: {statusObj.data.isRunning}");
        Console.WriteLine($"✅ Services Count: {statusObj.data.services?.Count ?? 0}");
    }

    static async Task TestGetWorkerIntegrationTestStatus()
    {
        Console.WriteLine("📊 Testing Worker Integration Test Status endpoint...");

        var response = await httpClient.GetAsync($"{baseUrl}/api/worker-integration-test/status");
        var content = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Response: {FormatJson(content)}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Worker integration test status request failed: {response.StatusCode}");
        }

        // Validate response structure
        dynamic statusObj = JsonConvert.DeserializeObject(content)!;
        if (statusObj.data == null)
        {
            throw new Exception("Response missing data field");
        }

        Console.WriteLine($"✅ Active Tests: {statusObj.data.activeTests}");
        Console.WriteLine($"✅ Total Indicators: {statusObj.data.totalIndicators}");
        Console.WriteLine($"✅ Available Test Types: {statusObj.data.availableTestTypes?.Count ?? 0}");
        Console.WriteLine($"✅ Recent Executions: {statusObj.data.recentExecutions?.Count ?? 0}");

        // Validate available test types
        if (statusObj.data.availableTestTypes != null)
        {
            var testTypes = new List<string>();
            foreach (var testType in statusObj.data.availableTestTypes)
            {
                testTypes.Add(testType.ToString());
            }
            Console.WriteLine($"📋 Test Types: {string.Join(", ", testTypes)}");
        }
    }

    static async Task TestStopSpecificTest()
    {
        Console.WriteLine("🛑 Testing specific test stopping...");

        // First, get current running tests
        var statusResponse = await httpClient.GetAsync($"{baseUrl}/api/worker-integration-test/status");
        if (!statusResponse.IsSuccessStatusCode)
        {
            throw new Exception("Failed to get current status");
        }

        var statusContent = await statusResponse.Content.ReadAsStringAsync();
        dynamic statusObj = JsonConvert.DeserializeObject(statusContent)!;

        // Find a running test to stop
        string? testIdToStop = null;
        if (statusObj.data.recentExecutions != null)
        {
            foreach (var execution in statusObj.data.recentExecutions)
            {
                if (execution.isRunning == true)
                {
                    testIdToStop = execution.id;
                    break;
                }
            }
        }

        if (testIdToStop != null)
        {
            var stopRequest = new { testId = testIdToStop };
            var json = JsonConvert.SerializeObject(stopRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{baseUrl}/api/worker-integration-test/stop", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Response: {FormatJson(responseContent)}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to stop test {testIdToStop}: {response.StatusCode}");
            }

            Console.WriteLine($"✅ Successfully stopped test: {testIdToStop}");
        }
        else
        {
            Console.WriteLine("ℹ️ No running tests found to stop");
        }
    }

    static async Task TestStopAllTests()
    {
        Console.WriteLine("🛑 Testing stop all tests...");

        var response = await httpClient.PostAsync($"{baseUrl}/api/worker-integration-test/stop", null);
        var content = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Response: {FormatJson(content)}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to stop all tests: {response.StatusCode}");
        }

        dynamic responseObj = JsonConvert.DeserializeObject(content)!;
        Console.WriteLine($"✅ Stopped tests: {responseObj.data.message}");
    }

    static async Task TestGetTestResults()
    {
        Console.WriteLine("📋 Testing test results retrieval...");

        var response = await httpClient.GetAsync($"{baseUrl}/api/worker-integration-test/results");
        var content = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Response: {FormatJson(content)}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get test results: {response.StatusCode}");
        }

        dynamic responseObj = JsonConvert.DeserializeObject(content)!;
        if (responseObj.data != null)
        {
            var resultsCount = responseObj.data.Count ?? 0;
            Console.WriteLine($"✅ Retrieved {resultsCount} test results");

            // Analyze results
            var completedTests = 0;
            var successfulTests = 0;
            var failedTests = 0;

            foreach (var result in responseObj.data)
            {
                // Fix JSON parsing issue with boolean values
                var isRunning = result.isRunning;
                bool isTestRunning = true; // Default to running
                if (isRunning != null)
                {
                    if (isRunning is bool boolValue)
                    {
                        isTestRunning = boolValue;
                    }
                    else if (bool.TryParse(isRunning.ToString(), out bool parsedValue))
                    {
                        isTestRunning = parsedValue;
                    }
                }

                if (!isTestRunning)
                {
                    completedTests++;

                    var success = result.success;
                    bool isSuccessful = false;
                    if (success != null)
                    {
                        if (success is bool successBool)
                        {
                            isSuccessful = successBool;
                        }
                        else if (bool.TryParse(success.ToString(), out bool successParsed))
                        {
                            isSuccessful = successParsed;
                        }
                    }

                    if (isSuccessful)
                    {
                        successfulTests++;
                    }
                    else
                    {
                        failedTests++;
                    }
                }
            }

            Console.WriteLine($"📊 Completed: {completedTests}, Successful: {successfulTests}, Failed: {failedTests}");
        }
    }

    static string FormatJson(string json)
    {
        try
        {
            var parsed = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsed, Formatting.Indented);
        }
        catch
        {
            return json;
        }
    }
}