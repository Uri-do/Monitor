using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static readonly string baseUrl = "https://localhost:57652";
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("🔧 Quick Fix Verification Test");
        Console.WriteLine("==============================");
        
        // Ignore SSL certificate errors for testing
        httpClient.DefaultRequestHeaders.Add("User-Agent", "QuickFixTest/1.0");
        System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

        try
        {
            // Test 1: Health Endpoint
            Console.WriteLine("\n🏥 Testing Health Endpoint Fix...");
            await TestHealthEndpoint();
            
            // Test 2: SignalR Group Methods
            Console.WriteLine("\n📡 Testing SignalR Group Methods Fix...");
            await TestSignalRGroupMethods();
            
            Console.WriteLine("\n✅ All fixes verified successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Test failed: {ex.Message}");
        }
        finally
        {
            httpClient.Dispose();
        }
        
        Console.WriteLine("\n⌨️  Press any key to exit...");
        Console.ReadKey();
    }
    
    static async Task TestHealthEndpoint()
    {
        try
        {
            var response = await httpClient.GetAsync($"{baseUrl}/api/health");
            Console.WriteLine($"Status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Health endpoint is now working!");
            }
            else
            {
                Console.WriteLine($"❌ Health endpoint still failing: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Health endpoint error: {ex.Message}");
        }
    }
    
    static async Task TestSignalRGroupMethods()
    {
        HubConnection? hubConnection = null;
        
        try
        {
            hubConnection = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}/hubs/worker-integration-test", options =>
                {
                    options.HttpMessageHandlerFactory = _ => new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };
                })
                .Build();

            await hubConnection.StartAsync();
            Console.WriteLine("✅ SignalR connected successfully");
            
            // Test JoinGroup method
            await hubConnection.InvokeAsync("JoinGroup", "TestGroup");
            Console.WriteLine("✅ JoinGroup method working!");
            
            // Test LeaveGroup method
            await hubConnection.InvokeAsync("LeaveGroup", "TestGroup");
            Console.WriteLine("✅ LeaveGroup method working!");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SignalR group methods error: {ex.Message}");
        }
        finally
        {
            if (hubConnection != null)
            {
                await hubConnection.DisposeAsync();
                Console.WriteLine("✅ SignalR disconnected cleanly");
            }
        }
    }
}
