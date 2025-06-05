using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api;
using MonitoringGrid.Core.Security;
using MonitoringGrid.Infrastructure.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace MonitoringGrid.Tests.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// </summary>
public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    private DbConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<MonitoringContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Create in-memory database for testing
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<MonitoringContext>(options =>
            {
                options.UseSqlite(_connection);
                options.EnableSensitiveDataLogging();
            });

            // Override security configuration for testing
            services.Configure<SecurityConfiguration>(config =>
            {
                config.Jwt = new JwtSettings
                {
                    SecretKey = "test-secret-key-that-is-long-enough-for-hmac-sha256-algorithm",
                    Issuer = "test-issuer",
                    Audience = "test-audience",
                    AccessTokenExpirationMinutes = 60,
                    RefreshTokenExpirationDays = 30
                };
                config.PasswordPolicy = new PasswordPolicy
                {
                    MinimumLength = 8,
                    RequireUppercase = true,
                    RequireLowercase = true,
                    RequireDigit = true,
                    RequireSpecialCharacter = true,
                    MaxFailedAttempts = 5,
                    LockoutDurationMinutes = 30
                };
                config.Encryption = new EncryptionSettings
                {
                    EncryptionKey = "test-encryption-key-32-characters",
                    HashingSalt = "test-hashing-salt-32-characters"
                };
            });

            // Build the service provider
            var serviceProvider = services.BuildServiceProvider();

            // Create the database and seed test data
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
            
            try
            {
                context.Database.EnsureCreated();
                SeedTestData(context);
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<TestWebApplicationFactory<TStartup>>>();
                logger.LogError(ex, "An error occurred seeding the database with test data");
            }
        });

        builder.UseEnvironment("Testing");
    }

    private static void SeedTestData(MonitoringContext context)
    {
        // Seed test KPIs
        if (!context.KPIs.Any())
        {
            var testKpis = new[]
            {
                new KPI
                {
                    Indicator = "Test KPI 1",
                    Owner = "Test Owner 1",
                    Query = "SELECT COUNT(*) FROM TestTable1",
                    HistoricalQuery = "SELECT COUNT(*) FROM TestTable1 WHERE Date = DATEADD(day, -1, GETDATE())",
                    Threshold = 10.0m,
                    Priority = 1,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    SubjectTemplate = "Alert: {{indicator}} threshold exceeded",
                    DescriptionTemplate = "Current value: {{current}}, Historical: {{historical}}, Deviation: {{deviation}}%"
                },
                new KPI
                {
                    Indicator = "Test KPI 2",
                    Owner = "Test Owner 2",
                    Query = "SELECT AVG(Value) FROM TestTable2",
                    HistoricalQuery = "SELECT AVG(Value) FROM TestTable2 WHERE Date = DATEADD(day, -1, GETDATE())",
                    Threshold = 5.0m,
                    Priority = 2,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    SubjectTemplate = "Warning: {{indicator}} deviation detected",
                    DescriptionTemplate = "Current value: {{current}}, Historical: {{historical}}, Deviation: {{deviation}}%"
                }
            };

            context.KPIs.AddRange(testKpis);
        }

        // Seed test contacts
        if (!context.Contacts.Any())
        {
            var testContacts = new[]
            {
                new Contact
                {
                    Name = "Test Contact 1",
                    Email = "test1@example.com",
                    Phone = "+1234567890",
                    IsActive = true
                },
                new Contact
                {
                    Name = "Test Contact 2",
                    Email = "test2@example.com",
                    Phone = "+1234567891",
                    IsActive = true
                }
            };

            context.Contacts.AddRange(testContacts);
        }

        // Seed test users
        if (!context.Set<AuthUser>().Any())
        {
            var testUsers = new[]
            {
                new AuthUser
                {
                    UserId = "test-user-1",
                    Username = "testuser1",
                    Email = "testuser1@example.com",
                    DisplayName = "Test User 1",
                    IsActive = true,
                    Roles = new List<string> { "Admin" },
                    Permissions = new List<string> { "read:kpis", "write:kpis", "delete:kpis" },
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                },
                new AuthUser
                {
                    UserId = "test-user-2",
                    Username = "testuser2",
                    Email = "testuser2@example.com",
                    DisplayName = "Test User 2",
                    IsActive = true,
                    Roles = new List<string> { "User" },
                    Permissions = new List<string> { "read:kpis" },
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                }
            };

            context.Set<AuthUser>().AddRange(testUsers);
        }

        // Seed test passwords
        if (!context.Set<UserPassword>().Any())
        {
            var testPasswords = new[]
            {
                new UserPassword
                {
                    UserId = "test-user-1",
                    PasswordHash = "hashed-password-1", // In real tests, use proper hashing
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new UserPassword
                {
                    UserId = "test-user-2",
                    PasswordHash = "hashed-password-2", // In real tests, use proper hashing
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                }
            };

            context.Set<UserPassword>().AddRange(testPasswords);
        }

        context.SaveChanges();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Base class for integration tests
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<TestWebApplicationFactory<Program>>
{
    protected readonly TestWebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(TestWebApplicationFactory<Program> factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Get authenticated HTTP client with JWT token
    /// </summary>
    protected async Task<HttpClient> GetAuthenticatedClientAsync(string username = "testuser1", string password = "TestPassword123!")
    {
        var loginRequest = new
        {
            Username = username,
            Password = password
        };

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        
        var authenticatedClient = Factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Token!.AccessToken);

        return authenticatedClient;
    }

    /// <summary>
    /// Get database context for direct data manipulation in tests
    /// </summary>
    protected MonitoringContext GetDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<MonitoringContext>();
    }

    /// <summary>
    /// Execute action within a database transaction
    /// </summary>
    protected async Task ExecuteInTransactionAsync(Func<MonitoringContext, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
        
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await action(context);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Execute function within a database transaction and return result
    /// </summary>
    protected async Task<T> ExecuteInTransactionAsync<T>(Func<MonitoringContext, Task<T>> func)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
        
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await func(context);
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
