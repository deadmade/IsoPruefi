using Database.Repository.CoordinateRepo;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rest_API.Controllers;
using Rest_API.Services.Temp;

namespace UnitTests.Controllers;

/// <summary>
///     Unit tests for the TempController class, verifying temperature operations and location management functionality.
/// </summary>
[TestFixture]
public class LocationControllerTests
{
    /// <summary>
    ///     Sets up test fixtures and initializes mocks before each test execution.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _mockTempService = new Mock<ITempService>();
        _mockCoordinateRepo = new Mock<ICoordinateRepo>();
        _mockLogger = new Mock<ILogger<LocationController>>();

        _controller = new LocationController(_mockTempService.Object, _mockCoordinateRepo.Object, _mockLogger.Object);
    }

    private Mock<ITempService> _mockTempService;
    private Mock<ICoordinateRepo> _mockCoordinateRepo;
    private Mock<ILogger<LocationController>> _mockLogger;
    private LocationController _controller;

    /// <summary>
    ///     Tests that the constructor creates a valid instance when provided with valid parameters.
    /// </summary>
    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        Action act = () => new LocationController(
            _mockTempService.Object,
            _mockCoordinateRepo.Object,
            _mockLogger.Object);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    ///     Tests that GetAllPostalcodes returns OK with postal codes when service returns data.
    /// </summary>
    [Test]
    public async Task GetAllPostalcodes_WithValidData_ShouldReturnOkWithPostalcodes()
    {
        // Arrange
        var expectedPostalcodes = new List<Tuple<int, string>>
        {
            new(12345, "Berlin"),
            new(80331, "Munich")
        };

        _mockTempService.Setup(x => x.ShowAvailableLocations())
            .ReturnsAsync(expectedPostalcodes);

        // Act
        var result = await _controller.GetAllPostalcodes();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(expectedPostalcodes);

        _mockTempService.Verify(x => x.ShowAvailableLocations(), Times.Once);
    }

    /// <summary>
    ///     Tests that GetAllPostalcodes returns InternalServerError when service throws exception.
    /// </summary>
    [Test]
    public async Task GetAllPostalcodes_WithException_ShouldReturnInternalServerError()
    {
        // Arrange
        var exception = new Exception("Database connection failed");
        _mockTempService.Setup(x => x.ShowAvailableLocations())
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetAllPostalcodes();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeOfType<ProblemDetails>();

        var problemDetails = (ProblemDetails)objectResult.Value!;
        problemDetails.Detail.Should().Be("Database connection failed");
        problemDetails.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.6.1");
        problemDetails.Title.Should().Be("Internal Server Error");
    }

    /// <summary>
    ///     Tests that GetAllPostalcodes returns OK with empty list when no postal codes exist.
    /// </summary>
    [Test]
    public async Task GetAllPostalcodes_WithEmptyData_ShouldReturnOkWithEmptyList()
    {
        // Arrange
        var expectedPostalcodes = new List<Tuple<int, string>>();
        _mockTempService.Setup(x => x.ShowAvailableLocations())
            .ReturnsAsync(expectedPostalcodes);

        // Act
        var result = await _controller.GetAllPostalcodes();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().Be(expectedPostalcodes);
    }

    /// <summary>
    ///     Tests that InsertLocation returns OK when location is inserted successfully.
    /// </summary>
    [Test]
    public async Task InsertLocation_WithValidPostalcode_ShouldReturnOk()
    {
        // Arrange
        const int postalCode = 12345;
        _mockTempService.Setup(x => x.GetCoordinates(postalCode))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.InsertLocation(postalCode);

        // Assert
        result.Should().BeOfType<OkResult>();
        _mockTempService.Verify(x => x.GetCoordinates(postalCode), Times.Once);
    }

    /// <summary>
    ///     Tests that InsertLocation returns InternalServerError when InvalidOperationException is thrown.
    /// </summary>
    [Test]
    public async Task InsertLocation_WithInvalidOperationException_ShouldReturnBadRequest()
    {
        // Arrange
        const int postalCode = 12345;
        var exception = new InvalidOperationException("API limit exceeded");
        _mockTempService.Setup(x => x.GetCoordinates(postalCode))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.InsertLocation(postalCode);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().BeOfType<ProblemDetails>();

        var problemDetails = (ProblemDetails)badRequestResult.Value!;
        problemDetails.Detail.Should().Be("API limit exceeded");
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");
        problemDetails.Title.Should().Be("Bad Request");
    }

    /// <summary>
    ///     Tests that InsertLocation returns InternalServerError when generic exception is thrown.
    /// </summary>
    [Test]
    public async Task InsertLocation_WithGenericException_ShouldReturnInternalServerError()
    {
        // Arrange
        const int postalCode = 12345;
        var exception = new Exception("Unexpected error");
        _mockTempService.Setup(x => x.GetCoordinates(postalCode))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.InsertLocation(postalCode);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeOfType<ProblemDetails>();

        var problemDetails = (ProblemDetails)objectResult.Value!;
        problemDetails.Detail.Should().Be("Unexpected error");
        problemDetails.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.6.1");
        problemDetails.Title.Should().Be("Internal Server Error");
    }

    /// <summary>
    ///     Tests that RemovePostalcode returns OK when postal code is removed successfully.
    /// </summary>
    [Test]
    public async Task RemovePostalcode_WithValidPostalcode_ShouldReturnOk()
    {
        // Arrange
        const int postalCode = 12345;
        _mockCoordinateRepo.Setup(x => x.DeletePostalCode(postalCode))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RemovePostalcode(postalCode);

        // Assert
        result.Should().BeOfType<OkResult>();

        _mockCoordinateRepo.Verify(x => x.DeletePostalCode(postalCode), Times.Once);
    }

    /// <summary>
    ///     Tests that RemovePostalcode returns InternalServerError when exception is thrown.
    /// </summary>
    [Test]
    public async Task RemovePostalcode_WithException_ShouldReturnInternalServerError()
    {
        // Arrange
        const int postalCode = 12345;
        var exception = new Exception("Database deletion failed");
        _mockCoordinateRepo.Setup(x => x.DeletePostalCode(postalCode))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.RemovePostalcode(postalCode);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeOfType<ProblemDetails>();

        var problemDetails = (ProblemDetails)objectResult.Value!;
        problemDetails.Detail.Should().Be("Database deletion failed");
        problemDetails.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.6.1");
        problemDetails.Title.Should().Be("Internal Server Error");
    }

    /// <summary>
    ///     Tests that GetAllPostalcodes logs error when exception occurs.
    /// </summary>
    [Test]
    public async Task GetAllPostalcodes_WithException_ShouldLogError()
    {
        // Arrange
        var exception = new Exception("Database connection failed");
        _mockTempService.Setup(x => x.ShowAvailableLocations())
            .ThrowsAsync(exception);

        // Act
        await _controller.GetAllPostalcodes();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error fetching all postalcodes")),
                It.Is<Exception>(ex => ex == exception),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Tests that InsertLocation logs error when InvalidOperationException occurs.
    /// </summary>
    [Test]
    public async Task InsertLocation_WithInvalidOperationException_ShouldLogError()
    {
        // Arrange
        const int postalCode = 12345;
        var exception = new InvalidOperationException("API limit exceeded");
        _mockTempService.Setup(x => x.GetCoordinates(postalCode))
            .ThrowsAsync(exception);

        // Act
        await _controller.InsertLocation(postalCode);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Bad request while inserting a new location")),
                It.Is<Exception>(ex => ex == exception),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Tests that RemovePostalcode logs error when exception occurs.
    /// </summary>
    [Test]
    public async Task RemovePostalcode_WithException_ShouldLogError()
    {
        // Arrange
        const int postalCode = 12345;
        var exception = new Exception("Database deletion failed");
        _mockCoordinateRepo.Setup(x => x.DeletePostalCode(postalCode))
            .ThrowsAsync(exception);

        // Act
        await _controller.RemovePostalcode(postalCode);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error deleting postalcode")),
                It.Is<Exception>(ex => ex == exception),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}