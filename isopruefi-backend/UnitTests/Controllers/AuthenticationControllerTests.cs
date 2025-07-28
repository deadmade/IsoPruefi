using System.Security.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rest_API.Controllers;
using Rest_API.Models;
using Rest_API.Services.Auth;

namespace UnitTests.Controllers;

[TestFixture]
public class AuthenticationControllerTests
{
    private Mock<IAuthenticationService> _mockAuthService;
    private Mock<ILogger<AuthenticationController>> _mockLogger;
    private AuthenticationController _controller;

    [SetUp]
    public void Setup()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockLogger = new Mock<ILogger<AuthenticationController>>();

        _controller = new AuthenticationController(_mockAuthService.Object, _mockLogger.Object);
    }

    #region Login Tests

    [Test]
    public async Task Login_WithValidCredentials_ShouldReturnOkWithJwtToken()
    {
        // Arrange
        var loginInput = new Login { UserName = "testuser", Password = "Test123!" };
        var expectedToken = new JwtToken
        {
            Token = "jwt-token",
            RefreshToken = "refresh-token",
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            CreatedDate = DateTime.UtcNow
        };

        _mockAuthService.Setup(x => x.Login(loginInput)).ReturnsAsync(expectedToken);

        // Act
        var result = await _controller.Login(loginInput);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(expectedToken);

        _mockAuthService.Verify(x => x.Login(loginInput), Times.Once);
    }

    [Test]
    public async Task Login_WithInvalidModelState_ShouldReturnBadRequestWithValidationProblemDetails()
    {
        // Arrange
        var loginInput = new Login { UserName = "testuser", Password = "" };
        _controller.ModelState.AddModelError("Password", "Password is required");

        // Act
        var result = await _controller.Login(loginInput);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>();

        var problemDetails = (ValidationProblemDetails)badRequestResult.Value!;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");

        _mockAuthService.Verify(x => x.Login(It.IsAny<Login>()), Times.Never);
    }

    [Test]
    public async Task Login_WithAuthenticationException_ShouldReturnUnauthorizedWithProblemDetails()
    {
        // Arrange
        var loginInput = new Login { UserName = "testuser", Password = "wrongpassword" };
        var authException = new AuthenticationException("Invalid credentials");

        _mockAuthService.Setup(x => x.Login(loginInput)).ThrowsAsync(authException);

        // Act
        var result = await _controller.Login(loginInput);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        unauthorizedResult.Value.Should().BeOfType<ProblemDetails>();

        var problemDetails = (ProblemDetails)unauthorizedResult.Value!;
        problemDetails.Status.Should().Be(StatusCodes.Status401Unauthorized);
        problemDetails.Detail.Should().Be("Invalid credentials");
        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.6.1");
    }

    [Test]
    public async Task Login_WithInvalidOperationException_ShouldReturnInternalServerErrorWithProblemDetails()
    {
        // Arrange
        var loginInput = new Login { UserName = "testuser", Password = "Test123!" };
        var invalidOpException = new InvalidOperationException("Database connection failed");

        _mockAuthService.Setup(x => x.Login(loginInput)).ThrowsAsync(invalidOpException);

        // Act
        var result = await _controller.Login(loginInput);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeOfType<ProblemDetails>();

        var problemDetails = (ProblemDetails)objectResult.Value!;
        problemDetails.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problemDetails.Detail.Should().Be("Database connection failed");
    }

    [Test]
    public async Task Login_WithGenericException_ShouldReturnInternalServerErrorWithProblemDetails()
    {
        // Arrange
        var loginInput = new Login { UserName = "testuser", Password = "Test123!" };
        var genericException = new Exception("Unexpected error");

        _mockAuthService.Setup(x => x.Login(loginInput)).ThrowsAsync(genericException);

        // Act
        var result = await _controller.Login(loginInput);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeOfType<ProblemDetails>();

        var problemDetails = (ProblemDetails)objectResult.Value!;
        problemDetails.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problemDetails.Detail.Should().Be("Unexpected error");
    }

    #endregion

    #region Register Tests

    [Test]
    public async Task Register_WithValidInput_ShouldReturnOk()
    {
        // Arrange
        var registerInput = new Register { UserName = "newuser", Password = "Test123!" };
        _mockAuthService.Setup(x => x.Register(registerInput)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Register(registerInput);

        // Assert
        result.Should().BeOfType<OkResult>();
        _mockAuthService.Verify(x => x.Register(registerInput), Times.Once);
    }

    [Test]
    public async Task Register_WithInvalidModelState_ShouldReturnBadRequestWithValidationProblemDetails()
    {
        // Arrange
        var registerInput = new Register { UserName = "", Password = "Test123!" };
        _controller.ModelState.AddModelError("UserName", "UserName is required");

        // Act
        var result = await _controller.Register(registerInput);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>();

        var problemDetails = (ValidationProblemDetails)badRequestResult.Value!;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");

        _mockAuthService.Verify(x => x.Register(It.IsAny<Register>()), Times.Never);
    }

    [Test]
    public async Task Register_WithException_ShouldReturnInternalServerErrorWithProblemDetails()
    {
        // Arrange
        var registerInput = new Register { UserName = "newuser", Password = "Test123!" };
        var exception = new Exception("Registration failed");

        _mockAuthService.Setup(x => x.Register(registerInput)).ThrowsAsync(exception);

        // Act
        var result = await _controller.Register(registerInput);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeOfType<ProblemDetails>();

        var problemDetails = (ProblemDetails)objectResult.Value!;
        problemDetails.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problemDetails.Detail.Should().Be("Registration failed");
    }

    #endregion

    #region Refresh Tests

    [Test]
    public async Task Refresh_WithValidToken_ShouldReturnOkWithNewJwtToken()
    {
        // Arrange
        var tokenInput = new JwtToken
        {
            Token = "expired-jwt-token",
            RefreshToken = "valid-refresh-token"
        };
        var newToken = new JwtToken
        {
            Token = "new-jwt-token",
            RefreshToken = "new-refresh-token",
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            CreatedDate = DateTime.UtcNow
        };

        _mockAuthService.Setup(x => x.RefreshToken(tokenInput)).ReturnsAsync(newToken);

        // Act
        var result = await _controller.Refresh(tokenInput);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(newToken);

        _mockAuthService.Verify(x => x.RefreshToken(tokenInput), Times.Once);
    }

    [Test]
    public async Task Refresh_WithInvalidModelState_ShouldReturnBadRequestWithValidationProblemDetails()
    {
        // Arrange
        var tokenInput = new JwtToken { Token = "", RefreshToken = "refresh-token" };
        _controller.ModelState.AddModelError("Token", "Token is required");

        // Act
        var result = await _controller.Refresh(tokenInput);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>();

        var problemDetails = (ValidationProblemDetails)badRequestResult.Value!;
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");

        _mockAuthService.Verify(x => x.RefreshToken(It.IsAny<JwtToken>()), Times.Never);
    }

    [Test]
    public async Task Refresh_WithException_ShouldReturnInternalServerErrorWithProblemDetails()
    {
        // Arrange
        var tokenInput = new JwtToken
        {
            Token = "expired-jwt-token",
            RefreshToken = "invalid-refresh-token"
        };
        var exception = new Exception("Token refresh failed");

        _mockAuthService.Setup(x => x.RefreshToken(tokenInput)).ThrowsAsync(exception);

        // Act
        var result = await _controller.Refresh(tokenInput);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeOfType<ProblemDetails>();

        var problemDetails = (ProblemDetails)objectResult.Value!;
        problemDetails.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problemDetails.Detail.Should().Be("Token refresh failed");
    }

    #endregion

    #region Logging Tests

    [Test]
    public async Task Login_ShouldLogInformationMessages()
    {
        // Arrange
        var loginInput = new Login { UserName = "testuser", Password = "Test123!" };
        var expectedToken = new JwtToken { Token = "jwt-token", RefreshToken = "refresh-token" };

        _mockAuthService.Setup(x => x.Login(loginInput)).ReturnsAsync(expectedToken);

        // Act
        await _controller.Login(loginInput);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Login attempt for user: testuser")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Login successful for user: testuser")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Register_ShouldLogInformationMessages()
    {
        // Arrange
        var registerInput = new Register { UserName = "newuser", Password = "Test123!" };
        _mockAuthService.Setup(x => x.Register(registerInput)).Returns(Task.CompletedTask);

        // Act
        await _controller.Register(registerInput);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Register attempt for user: newuser")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Registration successful for user: newuser")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}