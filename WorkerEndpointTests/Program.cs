using System.Text;
using System.Net;
using System.Net.Security;
using Newtonsoft.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace WorkerEndpointTests;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static readonly string baseUrl = "https://localhost:57652";
    private static HubConnection? hubConnection;

    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Worker Endpoint Tests Starting...");
        Console.WriteLine("=====================================");

        // Ignore SSL certificate errors for testing
        httpClient.DefaultRequestHeaders.Add("User-Agent", "WorkerEndpointTests/1.0");
        ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

        try
        {
            // Initialize SignalR connection
            await InitializeSignalRConnection();

            // Run all tests
            await RunAllTests();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test execution failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            // Cleanup
            await CleanupSignalRConnection();
            httpClient.Dispose();
        }

        Console.WriteLine("\n🏁 Tests completed. Press any key to exit...");
        Console.ReadKey();
    }

    static async Task InitializeSignalRConnection()
    {
        Console.WriteLine("\n🔌 Initializing SignalR Connection...");
        
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

        // Set up event handlers
        hubConnection.On<object>("WorkerTestStarted", (data) =>
        {
            Console.WriteLine($"📡 SignalR: WorkerTestStarted - {JsonConvert.SerializeObject(data)}");
        });

        hubConnection.On<object>("WorkerTestProgress", (data) =>
        {
            Console.WriteLine($"📡 SignalR: WorkerTestProgress - {JsonConvert.SerializeObject(data)}");
        });

        hubConnection.On<object>("WorkerTestCompleted", (data) =>
        {
            Console.WriteLine($"📡 SignalR: WorkerTestCompleted - {JsonConvert.SerializeObject(data)}");
        });

        hubConnection.On<object>("WorkerTestStopped", (data) =>
        {
            Console.WriteLine($"📡 SignalR: WorkerTestStopped - {JsonConvert.SerializeObject(data)}");
        });

        try
        {
            await hubConnection.StartAsync();
            Console.WriteLine("✅ SignalR connection established");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ SignalR connection failed: {ex.Message}");
        }
    }

    static async Task CleanupSignalRConnection()
    {
        if (hubConnection != null)
        {
            try
            {
                await hubConnection.StopAsync();
                await hubConnection.DisposeAsync();
                Console.WriteLine("🔌 SignalR connection closed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error closing SignalR connection: {ex.Message}");
            }
        }
    }

    static async Task RunAllTests()
    {
        Console.WriteLine("\n🧪 Running Worker Endpoint Tests...");
        
        // Test 1: Get Worker Status
        await TestGetWorkerStatus();
        
        // Test 2: Get Worker Integration Test Status
        await TestGetWorkerIntegrationTestStatus();
        
        // Test 3: Start Worker Process Management Test
        await TestStartWorkerProcessManagementTest();
        
        // Test 4: Start Indicator Execution Test
        await TestStartIndicatorExecutionTest();
        
        // Test 5: Start Stress Test
        await TestStartStressTest();
        
        // Test 6: Stop Test
        await TestStopTest();
        
        // Test 7: Get Test Results
        await TestGetTestResults();
    }

    static async Task TestGetWorkerStatus()
    {
        Console.WriteLine("\n📊 Test 1: GET /api/worker/status");
        try
        {
            var response = await httpClient.GetAsync($"{baseUrl}/api/worker/status");
            var content = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response: {FormatJson(content)}");
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Worker status retrieved successfully");
            }
            else
            {
                Console.WriteLine("❌ Failed to get worker status");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception: {ex.Message}");
        }
    }

    static async Task TestGetWorkerIntegrationTestStatus()
    {
        Console.WriteLine("\n📊 Test 2: GET /api/worker-integration-test/status");
        try
        {
            var response = await httpClient.GetAsync($"{baseUrl}/api/worker-integration-test/status");
            var content = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response: {FormatJson(content)}");
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Worker integration test status retrieved successfully");
            }
            else
            {
                Console.WriteLine("❌ Failed to get worker integration test status");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception: {ex.Message}");
        }
    }

    static async Task TestStartWorkerProcessManagementTest()
    {
        Console.WriteLine("\n🚀 Test 3: POST /api/worker-integration-test/start - Worker Process Management Test");
        try
        {
            var testRequest = new
            {
                TestType = "worker-process-management",
                DurationMinutes = 2,
                WorkerCount = 2,
                IndicatorIds = new int[] { },
                ConcurrentWorkers = 2
            };

            var json = JsonConvert.SerializeObject(testRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync($"{baseUrl}/api/worker-integration-test/start", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response: {FormatJson(responseContent)}");
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Worker Process Management Test started successfully");
                
                // Wait a bit to see some progress
                Console.WriteLine("⏳ Waiting 30 seconds to observe test progress...");
                await Task.Delay(30000);
            }
            else
            {
                Console.WriteLine("❌ Failed to start Worker Process Management Test");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception: {ex.Message}");
        }
    }

    static async Task TestStartIndicatorExecutionTest()
    {
        Console.WriteLine("\n🚀 Test 4: POST /api/worker-integration-test/start - Indicator Execution Test");
        try
        {
            var testRequest = new
            {
                TestType = "indicator-execution",
                DurationMinutes = 1,
                WorkerCount = 1,
                IndicatorIds = new int[] { },
                ConcurrentWorkers = 1
            };

            var json = JsonConvert.SerializeObject(testRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync($"{baseUrl}/api/worker-integration-test/start", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response: {FormatJson(responseContent)}");
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Indicator Execution Test started successfully");
                
                // Wait a bit to see some progress
                Console.WriteLine("⏳ Waiting 20 seconds to observe test progress...");
                await Task.Delay(20000);
            }
            else
            {
                Console.WriteLine("❌ Failed to start Indicator Execution Test");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception: {ex.Message}");
        }
    }

    static async Task TestStartStressTest()
    {
        Console.WriteLine("\n🚀 Test 5: POST /api/worker-integration-test/start - Stress Test");
        try
        {
            var testRequest = new
            {
                TestType = "stress-test",
                DurationMinutes = 1,
                WorkerCount = 3,
                IndicatorIds = new int[] { },
                ConcurrentWorkers = 3
            };

            var json = JsonConvert.SerializeObject(testRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync($"{baseUrl}/api/worker-integration-test/start", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response: {FormatJson(responseContent)}");
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Stress Test started successfully");
                
                // Wait a bit to see some progress
                Console.WriteLine("⏳ Waiting 20 seconds to observe test progress...");
                await Task.Delay(20000);
            }
            else
            {
                Console.WriteLine("❌ Failed to start Stress Test");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception: {ex.Message}");
        }
    }

    static async Task TestStopTest()
    {
        Console.WriteLine("\n🛑 Test 6: POST /api/worker-integration-test/stop");
        try
        {
            var response = await httpClient.PostAsync($"{baseUrl}/api/worker-integration-test/stop", null);
            var content = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response: {FormatJson(content)}");
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Test stopped successfully");
            }
            else
            {
                Console.WriteLine("❌ Failed to stop test");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception: {ex.Message}");
        }
    }

    static async Task TestGetTestResults()
    {
        Console.WriteLine("\n📋 Test 7: GET /api/worker-integration-test/results");
        try
        {
            var response = await httpClient.GetAsync($"{baseUrl}/api/worker-integration-test/results");
            var content = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response: {FormatJson(content)}");
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Test results retrieved successfully");
            }
            else
            {
                Console.WriteLine("❌ Failed to get test results");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception: {ex.Message}");
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


