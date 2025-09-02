using System.Text.Json;
using IntegrationTests.ApiClient;

namespace LoadTests.Infrastructure;

/// <summary>
/// Helper class for handling authentication in load tests
/// </summary>
public class AuthHelper : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private bool _disposed;

    public AuthHelper(string baseUrl)
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Gets an authentication token for load testing
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
                Password = "testpassword123" // Default test password
            };

            var response = await authClient.LoginAsync(loginRequest);
            
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

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Response model for authentication token
/// </summary>
public class TokenResponse
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
}