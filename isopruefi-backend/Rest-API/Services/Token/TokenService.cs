using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

namespace Rest_API.Services.Token;

/// <inheritdoc />
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenService"/> class.
    /// </summary>
    /// <param name="configuration">Application configuration for JWT settings.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }


    /// <inheritdoc />
    public string GenerateAccessToken(IEnumerable<Claim> claims)
    {
        _logger.LogInformation("Generating access token for claims: {Claims}",
            claims.Select(c => c.Type + ":" + c.Value));
        var tokenHandler = new JwtSecurityTokenHandler();

        // Create a symmetric security key using the secret key from the configuration.
        var secret = _configuration["JWT:Secret"] ??
                     throw new InvalidOperationException("JWT:Secret configuration is missing");
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _configuration["JWT:ValidIssuer"],
            Audience = _configuration["JWT:ValidAudience"],
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddMinutes(15),
            SigningCredentials = new SigningCredentials
                (authSigningKey, SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);
        _logger.LogInformation("Access token generated successfully.");
        return tokenString;
    }


    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        _logger.LogInformation("Generating refresh token.");
        // Create a 32-byte array to hold cryptographically secure random bytes
        var randomNumber = new byte[32];

        // Use a cryptographically secure random number generator
        // to fill the byte array with random values
        using var randomNumberGenerator = RandomNumberGenerator.Create();
        randomNumberGenerator.GetBytes(randomNumber);
        var refreshToken = Convert.ToBase64String(randomNumber);
        _logger.LogInformation("Refresh token generated successfully.");
        return refreshToken;
    }


    /// <inheritdoc />
    public ClaimsPrincipal GetPrincipalFromExpiredToken(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
            throw new ArgumentException("Access token cannot be null or empty", nameof(accessToken));

        try
        {
            // Define the token validation parameters used to validate the token.
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudience = _configuration["JWT:ValidAudience"],
                ValidIssuer = _configuration["JWT:ValidIssuer"],
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey
                (Encoding.UTF8.GetBytes(_configuration["JWT:Secret"] ??
                                        throw new InvalidOperationException("JWT:Secret configuration is missing")))
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            // Validate the token and extract the claims principal and the security token.
            var principal = tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out var securityToken);

            // Cast the security token to a JwtSecurityToken for further validation.
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            // Ensure the token is a valid JWT and uses the HmacSha256 signing algorithm.
            // If no throw new SecurityTokenException
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals
                    (SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            // return the principal
            return principal;
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            throw new SecurityTokenException("Invalid token", ex);
        }
    }
}