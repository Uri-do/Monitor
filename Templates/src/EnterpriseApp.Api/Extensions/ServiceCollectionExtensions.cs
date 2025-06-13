using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using FluentValidation;
using FluentValidation.AspNetCore;
using EnterpriseApp.Api.Filters;
using EnterpriseApp.Api.Middleware;
<!--#if (enableAuth)-->
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
<!--#endif-->
<!--#if (enableMonitoring)-->
using Microsoft.Extensions.Diagnostics.HealthChecks;
<!--#endif-->

namespace EnterpriseApp.Api.Extensions;

/// <summary>
/// Extension methods for IServiceCollection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds API versioning configuration
    /// </summary>
    public static IServiceCollection AddApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Version"),
                new QueryStringApiVersionReader("version")
            );
        });

        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    /// <summary>
    /// Adds Swagger documentation configuration
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "EnterpriseApp API",
                Version = "v1",
                Description = "Enterprise Application API with Clean Architecture",
                Contact = new OpenApiContact
                {
                    Name = "API Support",
                    Email = "support@enterpriseapp.com"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // Include XML comments
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

<!--#if (enableAuth)-->
            // Add JWT authentication to Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
<!--#endif-->

            // Add operation filters
            options.OperationFilter<SwaggerDefaultValues>();
            options.DocumentFilter<SwaggerDocumentFilter>();

            // Configure schema generation
            options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
            options.UseInlineDefinitionsForEnums();
        });

        return services;
    }

    /// <summary>
    /// Adds FluentValidation configuration
    /// </summary>
    public static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation(options =>
        {
            options.DisableDataAnnotationsValidation = false;
        });

        services.AddFluentValidationClientsideAdapters();

        // Register validators from the current assembly
        services.AddValidatorsFromAssembly(typeof(Program).Assembly);

        return services;
    }

<!--#if (enableAuth)-->
    /// <summary>
    /// Adds JWT authentication configuration
    /// </summary>
    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is required");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is required");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is required");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(context.Exception, "Authentication failed");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("Token validated for user: {User}", context.Principal?.Identity?.Name);
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Adds authorization policies
    /// </summary>
    public static IServiceCollection AddAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Domain entity policies
            options.AddPolicy("CanCreateDomainEntity", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireAssertion(context =>
                          context.User.IsInRole("Admin") ||
                          context.User.IsInRole("Manager") ||
                          context.User.HasClaim("permission", "DomainEntity:Create")));

            options.AddPolicy("CanUpdateDomainEntity", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireAssertion(context =>
                          context.User.IsInRole("Admin") ||
                          context.User.IsInRole("Manager") ||
                          context.User.HasClaim("permission", "DomainEntity:Update")));

            options.AddPolicy("CanDeleteDomainEntity", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireAssertion(context =>
                          context.User.IsInRole("Admin") ||
                          context.User.HasClaim("permission", "DomainEntity:Delete")));

            options.AddPolicy("CanViewAuditLogs", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireAssertion(context =>
                          context.User.IsInRole("Admin") ||
                          context.User.IsInRole("Auditor") ||
                          context.User.HasClaim("permission", "AuditLog:Read")));

            options.AddPolicy("CanExportData", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireAssertion(context =>
                          context.User.IsInRole("Admin") ||
                          context.User.IsInRole("Manager") ||
                          context.User.HasClaim("permission", "Data:Export")));

            // Admin policies
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("Admin"));

            options.AddPolicy("ManagerOrAdmin", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireAssertion(context =>
                          context.User.IsInRole("Admin") ||
                          context.User.IsInRole("Manager")));
        });

        return services;
    }
<!--#endif-->

<!--#if (enableMonitoring)-->
    /// <summary>
    /// Adds health checks configuration
    /// </summary>
    public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("API is running"))
            .AddDbContextCheck<EnterpriseApp.Infrastructure.Data.ApplicationDbContext>("database");

        // Add Redis health check if configured
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddHealthChecks()
                .AddRedis(redisConnectionString, "redis");
        }

        services.AddHealthChecksUI(options =>
        {
            options.SetEvaluationTimeInSeconds(30);
            options.MaximumHistoryEntriesPerEndpoint(50);
            options.AddHealthCheckEndpoint("API", "/health");
        })
        .AddInMemoryStorage();

        return services;
    }

    /// <summary>
    /// Adds metrics configuration
    /// </summary>
    public static IServiceCollection AddMetrics(this IServiceCollection services)
    {
        services.AddSingleton<IMetricsCollector, MetricsCollector>();
        return services;
    }
<!--#endif-->

    /// <summary>
    /// Adds application-specific services
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register filters
        services.AddScoped<GlobalExceptionFilter>();
        services.AddScoped<ValidationFilter>();
        services.AddScoped<PerformanceFilter>();

        // Register middleware options
        services.Configure<RequestResponseLoggingOptions>(options =>
        {
            options.LogRequestDetails = true;
            options.LogResponseDetails = false;
            options.LogRequestBody = true;
            options.LogResponseBody = false;
            options.SanitizeSensitiveData = true;
            options.MaxBodySizeToLog = 4096;
            options.SlowRequestThresholdMs = 5000;
        });

        // Register custom services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IApiResponseService, ApiResponseService>();

        return services;
    }

    /// <summary>
    /// Configures API behavior options
    /// </summary>
    public static IServiceCollection ConfigureApiBehavior(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            // Customize model validation error response
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );

                var problemDetails = new ValidationProblemDetails(errors)
                {
                    Title = "One or more validation errors occurred",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Instance = context.HttpContext.Request.Path
                };

                problemDetails.Extensions["timestamp"] = DateTime.UtcNow;
                problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                return new UnprocessableEntityObjectResult(problemDetails);
            };
        });

        return services;
    }

    /// <summary>
    /// Adds response compression
    /// </summary>
    public static IServiceCollection AddResponseCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
            options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
        });

        services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
        {
            options.Level = System.IO.Compression.CompressionLevel.Optimal;
        });

        services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
        {
            options.Level = System.IO.Compression.CompressionLevel.Optimal;
        });

        return services;
    }
}
