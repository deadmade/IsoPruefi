using System.Text.Json;
using IntegrationTests.ApiClient;

namespace LoadTests.Infrastructure;

/// <summary>
///     Helper class for handling authentication in load tests
/// </summary>
public class AuthHelper : IDisposable
{
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the AuthHelper class
    /// </summary>
    /// <param name="baseUrl">Base URL for the authentication API</param>
    public AuthHelper(string baseUrl)
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
    }

    /// <summary>
    ///     Disposes of the HTTP client resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Gets an authentication token for load testing
    /// </summary>
    /// <returns>JWT token string or null if authentication fails</returns>
    public async Task<string?> GetAuthTokenAsync()
    {
        try
        {
            var authClient = new AuthenticationClient(_baseUrl, _httpClient);

            // Use default test credentials - these should be configured in your test environment
            var loginRequest = new Login
            {
                UserName = "admin", // Default test user
                Password = "LoadTestAdmin123!" // Default test password
            };

            var response = await authClient.LoginAsync(loginRequest);

            if (response.StatusCode != 200) Assert.Fail($"Unexpected status code: {response.StatusCode}");

            if (response?.Stream != null)
            {
                using var reader = new StreamReader(response.Stream);
                var content = await reader.ReadToEndAsync();

                // Parse the JWT token from response
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return tokenResponse?.AccessToken;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Authentication failed: {ex.Message}");
            return null;
        }
    }
}

/// <summary>
///     Response model for authentication token
/// </summary>
public class TokenResponse
{
    /// <summary>
    ///     JWT access token for authentication
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    ///     Refresh token for renewing access tokens
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    ///     Token expiration time in seconds
    /// </summary>
    public int ExpiresIn { get; set; }
}