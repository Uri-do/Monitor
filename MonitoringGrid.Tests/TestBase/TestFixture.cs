using AutoFixture;
using AutoFixture.Xunit2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Security;
using MonitoringGrid.Infrastructure.Data;
using Moq;

namespace MonitoringGrid.Tests.TestBase;

/// <summary>
/// Base test fixture for setting up common test dependencies
/// </summary>
public class TestFixture : IDisposable
{
    public IFixture Fixture { get; }
    public Mock<ILogger> MockLogger { get; }
    public MonitoringContext Context { get; }
    public IServiceProvider ServiceProvider { get; }

    public TestFixture()
    {
        Fixture = new Fixture();
        MockLogger = new Mock<ILogger>();
        
        // Configure AutoFixture
        ConfigureAutoFixture();
        
        // Setup in-memory database
        Context = CreateInMemoryContext();
        
        // Setup service provider
        ServiceProvider = CreateServiceProvider();
    }

    private void ConfigureAutoFixture()
    {
        // Configure AutoFixture to handle circular references
        Fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => Fixture.Behaviors.Remove(b));
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Custom configurations for entities
        Fixture.Customize<KPI>(composer => composer
            .With(k => k.KpiId, () => Fixture.Create<int>())
            .With(k => k.Indicator, () => Fixture.Create<string>()[..Math.Min(100, Fixture.Create<string>().Length)])
            .With(k => k.Owner, () => Fixture.Create<string>()[..Math.Min(100, Fixture.Create<string>().Length)])
            .With(k => k.IsActive, true)
            .With(k => k.CreatedDate, DateTime.UtcNow)
            .With(k => k.ModifiedDate, DateTime.UtcNow)
            .Without(k => k.KpiContacts)
            .Without(k => k.HistoricalData)
            .Without(k => k.AlertLogs));

        Fixture.Customize<Contact>(composer => composer
            .With(c => c.ContactId, () => Fixture.Create<int>())
            .With(c => c.Name, () => Fixture.Create<string>()[..Math.Min(100, Fixture.Create<string>().Length)])
            .With(c => c.Email, () => $"{Fixture.Create<string>()[..10]}@test.com")
            .With(c => c.IsActive, true)
            .Without(c => c.KpiContacts));

        Fixture.Customize<AlertLog>(composer => composer
            .With(a => a.AlertLogId, () => Fixture.Create<int>())
            .With(a => a.TriggerTime, DateTime.UtcNow)
            .With(a => a.IsResolved, false)
            .Without(a => a.Kpi));

        Fixture.Customize<HistoricalData>(composer => composer
            .With(h => h.HistoricalDataId, () => Fixture.Create<int>())
            .With(h => h.ExecutionTime, DateTime.UtcNow)
            .With(h => h.IsSuccessful, true)
            .Without(h => h.Kpi));
    }

    private MonitoringContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<MonitoringContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        var context = new MonitoringContext(options);
        
        // Ensure database is created
        context.Database.EnsureCreated();
        
        return context;
    }

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add configuration
        var securityConfig = new SecurityConfiguration
        {
            Jwt = new JwtSettings
            {
                SecretKey = "test-secret-key-that-is-long-enough-for-hmac-sha256",
                Issuer = "test-issuer",
                Audience = "test-audience",
                AccessTokenExpirationMinutes = 60,
                RefreshTokenExpirationDays = 30
            },
            PasswordPolicy = new PasswordPolicy
            {
                MinimumLength = 8,
                RequireUppercase = true,
                RequireLowercase = true,
                RequireDigit = true,
                RequireSpecialCharacter = true,
                MaxFailedAttempts = 5,
                LockoutDurationMinutes = 30
            },
            Encryption = new EncryptionSettings
            {
                EncryptionKey = "test-encryption-key-32-characters",
                HashingSalt = "test-hashing-salt-32-characters"
            }
        };
        
        services.Configure<SecurityConfiguration>(config =>
        {
            config.Jwt = securityConfig.Jwt;
            config.PasswordPolicy = securityConfig.PasswordPolicy;
            config.Encryption = securityConfig.Encryption;
        });
        
        // Add DbContext
        services.AddDbContext<MonitoringContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        return services.BuildServiceProvider();
    }

    public void SeedTestData()
    {
        var kpis = Fixture.CreateMany<KPI>(5).ToList();
        var contacts = Fixture.CreateMany<Contact>(3).ToList();
        
        Context.KPIs.AddRange(kpis);
        Context.Contacts.AddRange(contacts);
        Context.SaveChanges();
    }

    public Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    public Mock<IOptions<T>> CreateMockOptions<T>(T value) where T : class
    {
        var mock = new Mock<IOptions<T>>();
        mock.Setup(x => x.Value).Returns(value);
        return mock;
    }

    public void Dispose()
    {
        Context?.Dispose();
        (ServiceProvider as ServiceProvider)?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// AutoData attribute for xUnit tests with custom fixture
/// </summary>
public class AutoMoqDataAttribute : AutoDataAttribute
{
    public AutoMoqDataAttribute() : base(() =>
    {
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization { ConfigureMembers = true });
        return fixture;
    })
    {
    }
}

/// <summary>
/// InlineAutoData attribute for xUnit tests with custom fixture
/// </summary>
public class InlineAutoMoqDataAttribute : InlineAutoDataAttribute
{
    public InlineAutoMoqDataAttribute(params object[] values) : base(new AutoMoqDataAttribute(), values)
    {
    }
}

/// <summary>
/// Base class for unit tests
/// </summary>
public abstract class UnitTestBase : IClassFixture<TestFixture>
{
    protected readonly TestFixture TestFixture;
    protected readonly IFixture Fixture;
    protected readonly MonitoringContext Context;

    protected UnitTestBase(TestFixture testFixture)
    {
        TestFixture = testFixture;
        Fixture = testFixture.Fixture;
        Context = testFixture.Context;
    }

    protected Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return TestFixture.CreateMockLogger<T>();
    }

    protected Mock<IOptions<T>> CreateMockOptions<T>(T value) where T : class
    {
        return TestFixture.CreateMockOptions(value);
    }

    protected void SeedTestData()
    {
        TestFixture.SeedTestData();
    }
}

/// <summary>
/// Base class for integration tests
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<TestFixture>
{
    protected readonly TestFixture TestFixture;
    protected readonly MonitoringContext Context;

    protected IntegrationTestBase(TestFixture testFixture)
    {
        TestFixture = testFixture;
        Context = testFixture.Context;
    }

    protected async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
    {
        using var transaction = await Context.Database.BeginTransactionAsync();
        try
        {
            var result = await action();
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    protected async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        using var transaction = await Context.Database.BeginTransactionAsync();
        try
        {
            await action();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
