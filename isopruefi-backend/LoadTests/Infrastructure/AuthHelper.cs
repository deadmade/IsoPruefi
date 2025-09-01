using System.Net.Http.Headers;
using IntegrationTests.ApiClient;

namespace LoadTests.Infrastructure;

public class AuthHelper
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public AuthHelper(string baseUrl)
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> GetAuthTokenAsync()
    {
        try
        {
            var authClient = new AuthenticationClient(_baseUrl, _httpClient);
            
            // Using a test user - you might need to adjust these credentials
            var loginRequest = new Login
            {
                UserName = "loadtest@example.com",
                Password = "LoadTest123!"
            };

            var response = await authClient.LoginAsync(loginRequest);
            
            // The response is a FileResponse, we need to read the content as JSON
            using var reader = new StreamReader(response.Stream);
            var jsonContent = await reader.ReadToEndAsync();
            
            // Parse the JSON to extract the token
            var tokenData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
            if (tokenData != null && tokenData.TryGetValue("accessToken", out var token))
            {
                return token.ToString() ?? throw new InvalidOperationException("Token is null");
            }
            
            throw new InvalidOperationException("No token found in response");
        }
        catch (ApiException ex)
        {
            throw new InvalidOperationException($"Authentication failed: {ex.Message}", ex);
        }
    }

    public HttpClient CreateAuthenticatedHttpClient(string token)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}