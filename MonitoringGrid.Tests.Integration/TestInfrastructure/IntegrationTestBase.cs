using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Infrastructure.Data;
using System.Data.Common;
using Testcontainers.MsSql;
using Xunit;

namespace MonitoringGrid.Tests.Integration.TestInfrastructure;

/// <summary>
/// Base class for integration tests with comprehensive test infrastructure
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<IntegrationTestWebApplicationFactory>, IAsyncLifetime
{
    protected readonly IntegrationTestWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected readonly IServiceScope Scope;
    protected readonly MonitoringContext Context;
    protected readonly ILogger Logger;

    protected IntegrationTestBase(IntegrationTestWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        Scope = factory.Services.CreateScope();
        Context = Scope.ServiceProvider.GetRequiredService<MonitoringContext>();
        Logger = Scope.ServiceProvider.GetRequiredService<ILogger<IntegrationTestBase>>();
    }

    public virtual async Task InitializeAsync()
    {
        // Ensure database is created and migrated
        await Context.Database.EnsureCreatedAsync();
        
        // Seed test data if needed
        await SeedTestDataAsync();
    }

    public virtual async Task DisposeAsync()
    {
        // Clean up test data
        await CleanupTestDataAsync();
        
        Scope.Dispose();
        Client.Dispose();
    }

    /// <summary>
    /// Seed test data for the test
    /// </summary>
    protected virtual async Task SeedTestDataAsync()
    {
        // Override in derived classes to seed specific test data
        await Task.CompletedTask;
    }

    /// <summary>
    /// Clean up test data after the test
    /// </summary>
    protected virtual async Task CleanupTestDataAsync()
    {
        // Clean up in reverse order of dependencies
        Context.ChangeTracker.Clear();
        
        // Remove test data
        var entities = Context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .Select(e => e.Entity);

        Context.RemoveRange(entities);
        await Context.SaveChangesAsync();
    }

    /// <summary>
    /// Execute a test with automatic transaction rollback
    /// </summary>
    protected async Task ExecuteInTransactionAsync(Func<Task> testAction)
    {
        using var transaction = await Context.Database.BeginTransactionAsync();
        try
        {
            await testAction();
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// Execute a test with automatic transaction rollback and return value
    /// </summary>
    protected async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> testAction)
    {
        using var transaction = await Context.Database.BeginTransactionAsync();
        try
        {
            return await testAction();
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// Create a test user for authentication tests
    /// </summary>
    protected async Task<string> CreateTestUserAsync(string username = "testuser", string email = "test@example.com")
    {
        var user = new Core.Entities.User
        {
            UserId = Guid.NewGuid().ToString(),
            Username = username,
            Email = email,
            DisplayName = $"Test {username}",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPassword123!"),
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        return user.UserId;
    }

    /// <summary>
    /// Create test indicators for testing
    /// </summary>
    protected async Task<List<int>> CreateTestIndicatorsAsync(int count = 3)
    {
        var indicators = new List<Core.Entities.Indicator>();
        
        for (int i = 0; i < count; i++)
        {
            var indicator = new Core.Entities.Indicator
            {
                IndicatorName = $"Test Indicator {i + 1}",
                IndicatorCode = $"TEST_IND_{i + 1}",
                CollectorID = 1, // Assume collector exists
                CollectorItemName = $"TestItem{i + 1}",
                LastMinutes = 60,
                IsActive = true,
                Priority = i + 1,
                CreatedDate = DateTime.UtcNow
            };
            
            indicators.Add(indicator);
        }

        Context.Indicators.AddRange(indicators);
        await Context.SaveChangesAsync();

        return indicators.Select(i => i.IndicatorID).ToList();
    }

    /// <summary>
    /// Assert that the response contains expected data
    /// </summary>
    protected static void AssertApiResponse<T>(ApiResponse<T> response, bool shouldSucceed = true)
    {
        Assert.NotNull(response);
        Assert.Equal(shouldSucceed, response.IsSuccess);
        
        if (shouldSucceed)
        {
            Assert.NotNull(response.Data);
            Assert.Null(response.ErrorMessage);
        }
        else
        {
            Assert.NotNull(response.ErrorMessage);
        }
    }

    /// <summary>
    /// Get authenticated HTTP client with JWT token
    /// </summary>
    protected async Task<HttpClient> GetAuthenticatedClientAsync(string username = "testuser")
    {
        var userId = await CreateTestUserAsync(username);
        var token = await GenerateJwtTokenAsync(userId);
        
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        return client;
    }

    /// <summary>
    /// Generate JWT token for testing
    /// </summary>
    private async Task<string> GenerateJwtTokenAsync(string userId)
    {
        var authService = Scope.ServiceProvider.GetRequiredService<Core.Interfaces.IAuthenticationService>();
        
        var loginRequest = new Core.Models.LoginRequest
        {
            Username = "testuser",
            Password = "TestPassword123!"
        };

        var result = await authService.AuthenticateAsync(loginRequest, "127.0.0.1");
        
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Token);
        
        return result.Token.AccessToken;
    }
}

/// <summary>
/// Custom web application factory for integration tests
/// </summary>
public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer;

    public IntegrationTestWebApplicationFactory()
    {
        _dbContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("TestPassword123!")
            .WithCleanUp(true)
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _dbContainer.GetConnectionString(),
                ["ConnectionStrings:ProgressPlayConnection"] = _dbContainer.GetConnectionString(),
                ["MonitoringGrid:Security:Jwt:SecretKey"] = "TestSecretKeyForIntegrationTests123456789",
                ["MonitoringGrid:Security:Jwt:Issuer"] = "MonitoringGrid.Tests",
                ["MonitoringGrid:Security:Jwt:Audience"] = "MonitoringGrid.Tests",
                ["MonitoringGrid:Security:Jwt:AccessTokenExpirationMinutes"] = "60",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:MonitoringGrid"] = "Information"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the app DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<MonitoringContext>));
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add test database context
            services.AddDbContext<MonitoringContext>(options =>
            {
                options.UseSqlServer(_dbContainer.GetConnectionString());
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // Override services for testing
            services.AddScoped<ITestDataSeeder, TestDataSeeder>();
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        
        // Run database migrations
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
        await context.Database.MigrateAsync();
        
        // Seed basic test data
        var seeder = scope.ServiceProvider.GetRequiredService<ITestDataSeeder>();
        await seeder.SeedBasicDataAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}

/// <summary>
/// Test data seeder for integration tests
/// </summary>
public interface ITestDataSeeder
{
    Task SeedBasicDataAsync();
}

public class TestDataSeeder : ITestDataSeeder
{
    private readonly MonitoringContext _context;
    private readonly ILogger<TestDataSeeder> _logger;

    public TestDataSeeder(MonitoringContext context, ILogger<TestDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedBasicDataAsync()
    {
        try
        {
            // Seed basic collectors
            if (!await _context.Collectors.AnyAsync())
            {
                var collectors = new[]
                {
                    new Core.Entities.Collector
                    {
                        CollectorCode = "TEST_COLLECTOR_1",
                        CollectorDesc = "Test Collector 1",
                        DisplayName = "Test Collector 1",
                        IsActive = true
                    },
                    new Core.Entities.Collector
                    {
                        CollectorCode = "TEST_COLLECTOR_2", 
                        CollectorDesc = "Test Collector 2",
                        DisplayName = "Test Collector 2",
                        IsActive = true
                    }
                };

                _context.Collectors.AddRange(collectors);
                await _context.SaveChangesAsync();
            }

            // Seed basic configuration
            if (!await _context.Config.AnyAsync())
            {
                var configs = new[]
                {
                    new Core.Entities.Config
                    {
                        Key = "TestConfig1",
                        Value = "TestValue1",
                        Description = "Test configuration 1",
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    }
                };

                _context.Config.AddRange(configs);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Basic test data seeded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding basic test data");
            throw;
        }
    }
}

/// <summary>
/// API response wrapper for testing
/// </summary>
public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CorrelationId { get; set; }
}
