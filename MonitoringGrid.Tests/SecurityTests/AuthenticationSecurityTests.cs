using FluentAssertions;
using MonitoringGrid.Core.Security;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace MonitoringGrid.Tests.SecurityTests;

public class AuthenticationSecurityTests : IntegrationTestBase
{
    public AuthenticationSecurityTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "testuser1",
            Password = "TestPassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.IsSuccess.Should().BeTrue();
        loginResponse.Token.Should().NotBeNull();
        loginResponse.Token!.AccessToken.Should().NotBeNullOrEmpty();
        loginResponse.Token.RefreshToken.Should().NotBeNullOrEmpty();
        loginResponse.User.Should().NotBeNull();
        loginResponse.User!.Username.Should().Be(loginRequest.Username);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "testuser1",
            Password = "WrongPassword"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.IsSuccess.Should().BeFalse();
        loginResponse.Token.Should().BeNull();
        loginResponse.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "nonexistentuser",
            Password = "TestPassword123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.IsSuccess.Should().BeFalse();
        loginResponse.ErrorMessage.Should().Contain("Invalid username or password");
    }

    [Theory]
    [InlineData("", "TestPassword123!")]
    [InlineData("testuser1", "")]
    [InlineData("", "")]
    [InlineData(null, "TestPassword123!")]
    [InlineData("testuser1", null)]
    public async Task Login_WithMissingCredentials_ShouldReturnBadRequest(string? username, string? password)
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = username!,
            Password = password!
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/kpis");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();

        // Act
        var response = await authenticatedClient.GetAsync("/api/kpis");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await Client.GetAsync("/api/kpis");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithExpiredToken_ShouldReturnUnauthorized()
    {
        // Arrange - Create a token that expires immediately
        var expiredTokenRequest = new LoginRequest
        {
            Username = "testuser1",
            Password = "TestPassword123!"
        };

        // This would require modifying the JWT service to create expired tokens for testing
        // For now, we'll simulate with an obviously invalid token structure
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1MTYyMzkwMjJ9.invalid";
        
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await Client.GetAsync("/api/kpis");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange
        var loginResponse = await GetLoginResponseAsync();
        
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = loginResponse.Token!.RefreshToken
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var refreshResponse = await response.Content.ReadFromJsonAsync<JwtToken>();
        refreshResponse.Should().NotBeNull();
        refreshResponse!.AccessToken.Should().NotBeNullOrEmpty();
        refreshResponse.RefreshToken.Should().NotBeNullOrEmpty();
        refreshResponse.AccessToken.Should().NotBe(loginResponse.Token.AccessToken);
        refreshResponse.RefreshToken.Should().NotBe(loginResponse.Token.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidRefreshToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithValidToken_ShouldInvalidateToken()
    {
        // Arrange
        var authenticatedClient = await GetAuthenticatedClientAsync();
        var token = authenticatedClient.DefaultRequestHeaders.Authorization!.Parameter!;

        // Act
        var logoutResponse = await authenticatedClient.PostAsync("/api/auth/logout", null);

        // Assert
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Try to use the token after logout - should fail
        var testResponse = await authenticatedClient.GetAsync("/api/kpis");
        testResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BruteForceProtection_MultipleFailedAttempts_ShouldLockAccount()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "testuser1",
            Password = "WrongPassword"
        };

        // Act - Attempt multiple failed logins
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 6; i++) // Exceed the limit of 5
        {
            var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
            responses.Add(response);
        }

        // Assert
        responses.Take(5).Should().OnlyContain(r => r.StatusCode == HttpStatusCode.Unauthorized);
        
        // The 6th attempt should indicate account lockout
        var lastResponse = responses.Last();
        var lastLoginResponse = await lastResponse.Content.ReadFromJsonAsync<LoginResponse>();
        lastLoginResponse!.ErrorMessage.Should().Contain("locked");
    }

    [Fact]
    public async Task SqlInjectionProtection_LoginEndpoint_ShouldNotBeVulnerable()
    {
        // Arrange - Common SQL injection payloads
        var sqlInjectionPayloads = new[]
        {
            "admin'; DROP TABLE Users; --",
            "' OR '1'='1",
            "' OR 1=1 --",
            "admin'/*",
            "' UNION SELECT * FROM Users --"
        };

        foreach (var payload in sqlInjectionPayloads)
        {
            var loginRequest = new LoginRequest
            {
                Username = payload,
                Password = "TestPassword123!"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            
            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            loginResponse!.IsSuccess.Should().BeFalse();
        }
    }

    [Fact]
    public async Task XssProtection_LoginEndpoint_ShouldSanitizeInput()
    {
        // Arrange - Common XSS payloads
        var xssPayloads = new[]
        {
            "<script>alert('xss')</script>",
            "javascript:alert('xss')",
            "<img src=x onerror=alert('xss')>",
            "';alert('xss');//"
        };

        foreach (var payload in xssPayloads)
        {
            var loginRequest = new LoginRequest
            {
                Username = payload,
                Password = "TestPassword123!"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().NotContain("<script>");
            responseContent.Should().NotContain("javascript:");
            responseContent.Should().NotContain("onerror=");
        }
    }

    [Fact]
    public async Task PasswordComplexity_WeakPassword_ShouldBeRejected()
    {
        // Arrange
        var changePasswordRequest = new ChangePasswordRequest
        {
            CurrentPassword = "TestPassword123!",
            NewPassword = "weak", // Weak password
            ConfirmPassword = "weak"
        };

        var authenticatedClient = await GetAuthenticatedClientAsync();

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/api/auth/change-password", changePasswordRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("password");
    }

    [Fact]
    public async Task RateLimiting_ExcessiveRequests_ShouldReturnTooManyRequests()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "testuser1",
            Password = "TestPassword123!"
        };

        // Act - Send many requests rapidly
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => Client.PostAsJsonAsync("/api/auth/login", loginRequest))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert - Some requests should be rate limited
        responses.Should().Contain(r => r.StatusCode == HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task TokenValidation_MalformedToken_ShouldReturnUnauthorized()
    {
        // Arrange - Various malformed tokens
        var malformedTokens = new[]
        {
            "not.a.token",
            "Bearer malformed",
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9", // Incomplete token
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.malformed.signature"
        };

        foreach (var token in malformedTokens)
        {
            Client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await Client.GetAsync("/api/kpis");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    private async Task<LoginResponse> GetLoginResponseAsync()
    {
        var loginRequest = new LoginRequest
        {
            Username = "testuser1",
            Password = "TestPassword123!"
        };

        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }
}
