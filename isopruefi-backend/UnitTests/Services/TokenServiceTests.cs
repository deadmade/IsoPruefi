using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Rest_API.Services.Token;

namespace UnitTests.Services;

[TestFixture]
public class TokenServiceTests
{
    private Mock<IConfiguration> _mockConfiguration;
    private Mock<ILogger<TokenService>> _mockLogger;
    private TokenService _tokenService;
    private const string TestSecret = "ThisIsATestSecretKeyForJWTTokenGenerationThatMustBeAtLeast32Characters";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    [SetUp]
    public void Setup()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<TokenService>>();

        _mockConfiguration.Setup(x => x["JWT:Secret"]).Returns(TestSecret);
        _mockConfiguration.Setup(x => x["JWT:ValidIssuer"]).Returns(TestIssuer);
        _mockConfiguration.Setup(x => x["JWT:ValidAudience"]).Returns(TestAudience);

        _tokenService = new TokenService(_mockConfiguration.Object, _mockLogger.Object);
    }

    [Test]
    public void GenerateAccessToken_WithValidClaims_ShouldReturnValidJwtToken()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "testuser"),
            new(ClaimTypes.Role, "User"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Act
        var token = _tokenService.GenerateAccessToken(claims);

        // Assert
        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();

        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Issuer.Should().Be(TestIssuer);
        jwtToken.Audiences.Should().Contain(TestAudience);
        jwtToken.Claims.Should().Contain(c => c.Type == "unique_name" && c.Value == "testuser");
        jwtToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == "User");

        jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow);
        jwtToken.ValidTo.Should().BeBefore(DateTime.UtcNow.AddMinutes(16));
    }

    [Test]
    public void GenerateAccessToken_WithEmptyClaims_ShouldReturnValidJwtToken()
    {
        // Arrange
        var claims = new List<Claim>();

        // Act
        var token = _tokenService.GenerateAccessToken(claims);

        // Assert
        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();

        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Issuer.Should().Be(TestIssuer);
        jwtToken.Audiences.Should().Contain(TestAudience);
    }

    [Test]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var refreshToken1 = _tokenService.GenerateRefreshToken();
        var refreshToken2 = _tokenService.GenerateRefreshToken();

        // Assert
        refreshToken1.Should().NotBeNullOrEmpty();
        refreshToken2.Should().NotBeNullOrEmpty();
        refreshToken1.Should().NotBe(refreshToken2); // Each call should generate unique token

        // Should be valid base64
        var bytes1 = Convert.FromBase64String(refreshToken1);
        var bytes2 = Convert.FromBase64String(refreshToken2);
        bytes1.Length.Should().Be(32);
        bytes2.Length.Should().Be(32);
    }

    [Test]
    public void GetPrincipalFromExpiredToken_WithValidToken_ShouldReturnClaimsPrincipal()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "testuser"),
            new(ClaimTypes.Role, "User")
        };
        var token = _tokenService.GenerateAccessToken(claims);

        // Act
        var principal = _tokenService.GetPrincipalFromExpiredToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal.Identity.Should().NotBeNull();
        principal.Identity!.Name.Should().Be("testuser");
        principal.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "testuser");
        principal.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "User");
    }

    [Test]
    public void GetPrincipalFromExpiredToken_WithInvalidToken_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act & Assert
        var act = () => _tokenService.GetPrincipalFromExpiredToken(invalidToken);
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void GetPrincipalFromExpiredToken_WithTokenSignedWithDifferentKey_ShouldThrowSecurityTokenException()
    {
        // Arrange
        var differentSecret = "DifferentSecretKeyForTestingPurposesThatIsAtLeast32Characters";
        var mockConfigWithDifferentSecret = new Mock<IConfiguration>();
        mockConfigWithDifferentSecret.Setup(x => x["JWT:Secret"]).Returns(differentSecret);
        mockConfigWithDifferentSecret.Setup(x => x["JWT:ValidIssuer"]).Returns(TestIssuer);
        mockConfigWithDifferentSecret.Setup(x => x["JWT:ValidAudience"]).Returns(TestAudience);

        var differentTokenService = new TokenService(mockConfigWithDifferentSecret.Object, _mockLogger.Object);
        var claims = new List<Claim> { new(ClaimTypes.Name, "testuser") };
        var tokenWithDifferentKey = differentTokenService.GenerateAccessToken(claims);

        // Act & Assert
        var act = () => _tokenService.GetPrincipalFromExpiredToken(tokenWithDifferentKey);
        act.Should().Throw<SecurityTokenException>();
    }

    [Test]
    public void GetPrincipalFromExpiredToken_WithNullToken_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => _tokenService.GetPrincipalFromExpiredToken(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void GetPrincipalFromExpiredToken_WithEmptyToken_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => _tokenService.GetPrincipalFromExpiredToken(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void GenerateAccessToken_ShouldLogInformation()
    {
        // Arrange
        var claims = new List<Claim> { new(ClaimTypes.Name, "testuser") };

        // Act
        _tokenService.GenerateAccessToken(claims);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generating access token")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Access token generated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void GenerateRefreshToken_ShouldLogInformation()
    {
        // Act
        _tokenService.GenerateRefreshToken();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generating refresh token")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Refresh token generated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}