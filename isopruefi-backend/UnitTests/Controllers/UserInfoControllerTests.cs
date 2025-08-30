using Database.EntityFramework.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rest_API.Controllers;
using Rest_API.Models;
using Rest_API.Services.User;

namespace UnitTests.Controllers;

/// <summary>
///     Unit tests for the UserInfoController class, verifying user management functionality.
/// </summary>
[TestFixture]
public class UserInfoControllerTests
{
    /// <summary>
    ///     Sets up test fixtures and initializes mocks before each test execution.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<UserInfoController>>();
        _controller = new UserInfoController(_mockUserService.Object, _mockLogger.Object);
    }

    private Mock<IUserService> _mockUserService;
    private Mock<ILogger<UserInfoController>> _mockLogger;
    private UserInfoController _controller;

    /// <summary>
    ///     Tests that the constructor creates a valid instance when provided with valid parameters.
    /// </summary>
    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        Action act = () => new UserInfoController(_mockUserService.Object, _mockLogger.Object);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    ///     Tests that GetAllUsers returns OK with users when service returns data.
    /// </summary>
    [Test]
    public async Task GetAllUsers_WithValidData_ShouldReturnOkWithUsers()
    {
        // Arrange
        var expectedUsers = new List<ApiUser>
        {
            new() { Id = "1", UserName = "user1" },
            new() { Id = "2", UserName = "user2" }
        };

        _mockUserService.Setup(x => x.GetUserInformations())
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(expectedUsers);

        _mockUserService.Verify(x => x.GetUserInformations(), Times.Once);
    }

    /// <summary>
    ///     Tests that GetAllUsers returns InternalServerError when service throws exception.
    /// </summary>
    [Test]
    public async Task GetAllUsers_WithException_ShouldReturnInternalServerError()
    {
        // Arrange
        var exception = new Exception("Database connection failed");
        _mockUserService.Setup(x => x.GetUserInformations())
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeOfType<ProblemDetails>();

        var problemDetails = (ProblemDetails)objectResult.Value!;
        problemDetails.Detail.Should().Be("Database connection failed");
    }

    /// <summary>
    ///     Tests that GetAllUsers logs information when fetching users.
    /// </summary>
    [Test]
    public async Task GetAllUsers_ShouldLogInformation()
    {
        // Arrange
        var expectedUsers = new List<ApiUser>();
        _mockUserService.Setup(x => x.GetUserInformations())
            .ReturnsAsync(expectedUsers);

        // Act
        await _controller.GetAllUsers();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetching all users")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Tests that GetUserById returns OK with user when user exists.
    /// </summary>
    [Test]
    public async Task GetUserById_WithExistingUser_ShouldReturnOkWithUser()
    {
        // Arrange
        const string userId = "test-user-id";
        var expectedUser = new ApiUser { Id = userId, UserName = "testuser" };

        _mockUserService.Setup(x => x.GetUserById(userId))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.GetUserById(userId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(expectedUser);

        _mockUserService.Verify(x => x.GetUserById(userId), Times.Once);
    }

    /// <summary>
    ///     Tests that GetUserById returns NotFound when user does not exist.
    /// </summary>
    [Test]
    public async Task GetUserById_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        const string userId = "non-existent-id";
        _mockUserService.Setup(x => x.GetUserById(userId))
            .ReturnsAsync((ApiUser?)null);

        // Act
        var result = await _controller.GetUserById(userId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _mockUserService.Verify(x => x.GetUserById(userId), Times.Once);
    }

    /// <summary>
    ///     Tests that GetUserById returns InternalServerError when service throws exception.
    /// </summary>
    [Test]
    public async Task GetUserById_WithException_ShouldReturnInternalServerError()
    {
        // Arrange
        const string userId = "test-user-id";
        var exception = new Exception("Database connection failed");
        _mockUserService.Setup(x => x.GetUserById(userId))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetUserById(userId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeOfType<ProblemDetails>();

        var problemDetails = (ProblemDetails)objectResult.Value!;
        problemDetails.Detail.Should().Be("Database connection failed");
    }

    /// <summary>
    ///     Tests that ChangePassword returns OK when password is changed successfully.
    /// </summary>
    [Test]
    public async Task ChangePassword_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var changePasswordInput = new ChangePassword
        {
            UserId = "test-user-id",
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        var user = new ApiUser { Id = changePasswordInput.UserId, UserName = "testuser" };

        _mockUserService.Setup(x => x.GetUserById(changePasswordInput.UserId))
            .ReturnsAsync(user);
        _mockUserService.Setup(x =>
                x.ChangePassword(user, changePasswordInput.CurrentPassword, changePasswordInput.NewPassword))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ChangePassword(changePasswordInput);

        // Assert
        result.Should().BeOfType<OkResult>();

        _mockUserService.Verify(x => x.GetUserById(changePasswordInput.UserId), Times.Once);
        _mockUserService.Verify(
            x => x.ChangePassword(user, changePasswordInput.CurrentPassword, changePasswordInput.NewPassword),
            Times.Once);
    }

    /// <summary>
    ///     Tests that ChangePassword returns NotFound when user does not exist.
    /// </summary>
    [Test]
    public async Task ChangePassword_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var changePasswordInput = new ChangePassword
        {
            UserId = "non-existent-id",
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        _mockUserService.Setup(x => x.GetUserById(changePasswordInput.UserId))
            .ReturnsAsync((ApiUser?)null);

        // Act
        var result = await _controller.ChangePassword(changePasswordInput);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _mockUserService.Verify(x => x.GetUserById(changePasswordInput.UserId), Times.Once);
        _mockUserService.Verify(x => x.ChangePassword(It.IsAny<ApiUser>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    /// <summary>
    ///     Tests that ChangePassword returns BadRequest when model state is invalid.
    /// </summary>
    [Test]
    public async Task ChangePassword_WithInvalidModelState_ShouldReturnBadRequest()
    {
        // Arrange
        var changePasswordInput = new ChangePassword
        {
            UserId = "test-user-id",
            CurrentPassword = "",
            NewPassword = "NewPassword123!"
        };

        _controller.ModelState.AddModelError("CurrentPassword", "Current password is required");

        // Act
        var result = await _controller.ChangePassword(changePasswordInput);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>();

        _mockUserService.Verify(x => x.GetUserById(It.IsAny<string>()), Times.Never);
        _mockUserService.Verify(x => x.ChangePassword(It.IsAny<ApiUser>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    /// <summary>
    ///     Tests that ChangePassword returns InternalServerError when service throws exception.
    /// </summary>
    [Test]
    public async Task ChangePassword_WithException_ShouldReturnInternalServerError()
    {
        // Arrange
        var changePasswordInput = new ChangePassword
        {
            UserId = "test-user-id",
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        var user = new ApiUser { Id = changePasswordInput.UserId, UserName = "testuser" };
        var exception = new Exception("Password change failed");

        _mockUserService.Setup(x => x.GetUserById(changePasswordInput.UserId))
            .ReturnsAsync(user);
        _mockUserService.Setup(x =>
                x.ChangePassword(user, changePasswordInput.CurrentPassword, changePasswordInput.NewPassword))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.ChangePassword(changePasswordInput);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeOfType<ProblemDetails>();

        var problemDetails = (ProblemDetails)objectResult.Value!;
        problemDetails.Detail.Should().Be("Password change failed");
    }

    /// <summary>
    ///     Tests that ChangeUser returns OK when user is updated successfully.
    /// </summary>
    [Test]
    public async Task ChangeUser_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var user = new ApiUser { Id = "test-user-id", UserName = "updateduser" };

        _mockUserService.Setup(x => x.ChangeUser(user))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ChangeUser(user);

        // Assert
        result.Should().BeOfType<OkResult>();
        _mockUserService.Verify(x => x.ChangeUser(user), Times.Once);
    }

    /// <summary>
    ///     Tests that ChangeUser returns BadRequest when model state is invalid.
    /// </summary>
    [Test]
    public async Task ChangeUser_WithInvalidModelState_ShouldReturnBadRequest()
    {
        // Arrange
        var user = new ApiUser { Id = "test-user-id", UserName = "" };
        _controller.ModelState.AddModelError("UserName", "UserName is required");

        // Act
        var result = await _controller.ChangeUser(user);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>();

        _mockUserService.Verify(x => x.ChangeUser(It.IsAny<ApiUser>()), Times.Never);
    }

    /// <summary>
    ///     Tests that ChangeUser returns InternalServerError when service throws exception.
    /// </summary>
    [Test]
    public async Task ChangeUser_WithException_ShouldReturnInternalServerError()
    {
        // Arrange
        var user = new ApiUser { Id = "test-user-id", UserName = "updateduser" };
        var exception = new Exception("User update failed");

        _mockUserService.Setup(x => x.ChangeUser(user))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.ChangeUser(user);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeOfType<ProblemDetails>();

        var problemDetails = (ProblemDetails)objectResult.Value!;
        problemDetails.Detail.Should().Be("User update failed");
    }

    /// <summary>
    ///     Tests that DeleteUser returns OK when user is deleted successfully.
    /// </summary>
    [Test]
    public async Task DeleteUser_WithExistingUser_ShouldReturnOk()
    {
        // Arrange
        const string userId = "test-user-id";
        var user = new ApiUser { Id = userId, UserName = "testuser" };

        _mockUserService.Setup(x => x.GetUserById(userId))
            .ReturnsAsync(user);
        _mockUserService.Setup(x => x.DeleteUser(user))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        result.Should().BeOfType<OkResult>();

        _mockUserService.Verify(x => x.GetUserById(userId), Times.Once);
        _mockUserService.Verify(x => x.DeleteUser(user), Times.Once);
    }

    /// <summary>
    ///     Tests that DeleteUser returns NotFound when user does not exist.
    /// </summary>
    [Test]
    public async Task DeleteUser_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        const string userId = "non-existent-id";
        _mockUserService.Setup(x => x.GetUserById(userId))
            .ReturnsAsync((ApiUser?)null);

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _mockUserService.Verify(x => x.GetUserById(userId), Times.Once);
        _mockUserService.Verify(x => x.DeleteUser(It.IsAny<ApiUser>()), Times.Never);
    }

    /// <summary>
    ///     Tests that DeleteUser returns InternalServerError when deletion fails.
    /// </summary>
    [Test]
    public async Task DeleteUser_WhenDeletionFails_ShouldReturnInternalServerError()
    {
        // Arrange
        const string userId = "test-user-id";
        var user = new ApiUser { Id = userId, UserName = "testuser" };

        _mockUserService.Setup(x => x.GetUserById(userId))
            .ReturnsAsync(user);
        _mockUserService.Setup(x => x.DeleteUser(user))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeOfType<ProblemDetails>();

        var problemDetails = (ProblemDetails)objectResult.Value!;
        problemDetails.Detail.Should().Be("Failed to delete user.");
    }

    /// <summary>
    ///     Tests that DeleteUser returns InternalServerError when service throws exception.
    /// </summary>
    [Test]
    public async Task DeleteUser_WithException_ShouldReturnInternalServerError()
    {
        // Arrange
        const string userId = "test-user-id";
        var user = new ApiUser { Id = userId, UserName = "testuser" };
        var exception = new Exception("Database deletion failed");

        _mockUserService.Setup(x => x.GetUserById(userId))
            .ReturnsAsync(user);
        _mockUserService.Setup(x => x.DeleteUser(user))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeOfType<ProblemDetails>();

        var problemDetails = (ProblemDetails)objectResult.Value!;
        problemDetails.Detail.Should().Be("Database deletion failed");
    }

    /// <summary>
    ///     Tests that GetUserById logs information when fetching a user.
    /// </summary>
    [Test]
    public async Task GetUserById_ShouldLogInformation()
    {
        // Arrange
        const string userId = "test-user-id";
        var user = new ApiUser { Id = userId, UserName = "testuser" };
        _mockUserService.Setup(x => x.GetUserById(userId))
            .ReturnsAsync(user);

        // Act
        await _controller.GetUserById(userId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Fetching user by ID: {userId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Tests that ChangePassword logs warning when model state is invalid.
    /// </summary>
    [Test]
    public async Task ChangePassword_WithInvalidModelState_ShouldLogWarning()
    {
        // Arrange
        var changePasswordInput = new ChangePassword
        {
            UserId = "test-user-id",
            CurrentPassword = "",
            NewPassword = "NewPassword123!"
        };

        _controller.ModelState.AddModelError("CurrentPassword", "Current password is required");

        // Act
        await _controller.ChangePassword(changePasswordInput);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid model state for ChangePassword")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}