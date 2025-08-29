using Database.EntityFramework.Models;
using Database.Repository.SettingsRepo;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;
using Rest_API.Controllers;

namespace UnitTests.Controllers;

/// <summary>
/// Unit tests for the TopicController class, verifying MQTT topic management functionality.
/// </summary>
[TestFixture]
public class TopicControllerTests
{
    private Mock<ISettingsRepo> _mockSettingsRepo;
    private TopicController _controller;

    /// <summary>
    /// Sets up test fixtures and initializes mocks before each test execution.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _mockSettingsRepo = new Mock<ISettingsRepo>();
        _controller = new TopicController(_mockSettingsRepo.Object);
    }

    #region Constructor Tests

    /// <summary>
    /// Tests that the constructor creates a valid instance when provided with valid parameters.
    /// </summary>
    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        Action act = () => new TopicController(_mockSettingsRepo.Object);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when settings repository is null.
    /// </summary>
    [Test]
    public void Constructor_WithNullSettingsRepo_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new TopicController(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region GetAllTopics Tests

    /// <summary>
    /// Tests that GetAllTopics returns OK with topics when service returns data.
    /// </summary>
    [Test]
    public async Task GetAllTopics_WithValidData_ShouldReturnOkWithTopics()
    {
        // Arrange
        var expectedTopics = new List<TopicSetting>
        {
            new() { TopicSettingId = 1, SensorName = "TempSensor_01", SensorLocation = "North" },
            new() { TopicSettingId = 2, SensorName = "TempSensor_02", SensorLocation = "South" }
        };

        _mockSettingsRepo.Setup(x => x.GetTopicSettingsAsync())
            .ReturnsAsync(expectedTopics);

        // Act
        var result = await _controller.GetAllTopics();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.Value.Should().Be(expectedTopics);

        _mockSettingsRepo.Verify(x => x.GetTopicSettingsAsync(), Times.Once);
    }

    /// <summary>
    /// Tests that GetAllTopics returns InternalServerError when service throws exception.
    /// </summary>
    [Test]
    public async Task GetAllTopics_WithException_ShouldReturnInternalServerError()
    {
        // Arrange
        var exception = new Exception("Database connection failed");
        _mockSettingsRepo.Setup(x => x.GetTopicSettingsAsync())
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetAllTopics();

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var errorResponse = objectResult.Value;
        errorResponse.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that GetAllTopics returns OK with empty list when no topics exist.
    /// </summary>
    [Test]
    public async Task GetAllTopics_WithEmptyData_ShouldReturnOkWithEmptyList()
    {
        // Arrange
        var expectedTopics = new List<TopicSetting>();
        _mockSettingsRepo.Setup(x => x.GetTopicSettingsAsync())
            .ReturnsAsync(expectedTopics);

        // Act
        var result = await _controller.GetAllTopics();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.Value.Should().Be(expectedTopics);
    }

    #endregion

    #region CreateTopic Tests

    /// <summary>
    /// Tests that CreateTopic returns Created when topic is created successfully.
    /// </summary>
    [Test]
    public async Task CreateTopic_WithValidTopic_ShouldReturnCreated()
    {
        // Arrange
        var topicSetting = new TopicSetting
        {
            SensorName = "TempSensor_03",
            SensorLocation = "East"
        };

        _mockSettingsRepo.Setup(x => x.AddTopicSettingAsync(It.IsAny<TopicSetting>()))
            .ReturnsAsync(3);

        // Act
        var result = await _controller.CreateTopic(topicSetting);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();

        _mockSettingsRepo.Verify(x => x.AddTopicSettingAsync(It.Is<TopicSetting>(t =>
            t.SensorName == topicSetting.SensorName &&
            t.SensorLocation == topicSetting.SensorLocation &&
            t.TopicSettingId == 0)), Times.Once);
    }

    /// <summary>
    /// Tests that CreateTopic returns BadRequest when topic setting is null.
    /// </summary>
    [Test]
    public async Task CreateTopic_WithNullTopic_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.CreateTopic(null!);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;

        var errorResponse = badRequestResult.Value;
        errorResponse.Should().NotBeNull();

        _mockSettingsRepo.Verify(x => x.AddTopicSettingAsync(It.IsAny<TopicSetting>()), Times.Never);
    }

    /// <summary>
    /// Tests that CreateTopic returns BadRequest when model state is invalid.
    /// </summary>
    [Test]
    public async Task CreateTopic_WithInvalidModelState_ShouldReturnBadRequest()
    {
        // Arrange
        var topicSetting = new TopicSetting
        {
            SensorName = "TempSensor_03",
            SensorLocation = "East"
        };

        _controller.ModelState.AddModelError("SensorName", "SensorName is required");

        // Act
        var result = await _controller.CreateTopic(topicSetting);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.Should().BeOfType<SerializableError>();

        _mockSettingsRepo.Verify(x => x.AddTopicSettingAsync(It.IsAny<TopicSetting>()), Times.Never);
    }

    /// <summary>
    /// Tests that CreateTopic returns InternalServerError when service throws exception.
    /// </summary>
    [Test]
    public async Task CreateTopic_WithException_ShouldReturnInternalServerError()
    {
        // Arrange
        var topicSetting = new TopicSetting
        {
            SensorName = "TempSensor_03",
            SensorLocation = "East"
        };

        var exception = new Exception("Database insertion failed");
        _mockSettingsRepo.Setup(x => x.AddTopicSettingAsync(It.IsAny<TopicSetting>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.CreateTopic(topicSetting);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var errorResponse = objectResult.Value;
        errorResponse.Should().NotBeNull();
    }

    #endregion

    #region UpdateTopic Tests

    /// <summary>
    /// Tests that UpdateTopic returns OK when topic is updated successfully.
    /// </summary>
    [Test]
    public async Task UpdateTopic_WithValidTopic_ShouldReturnOk()
    {
        // Arrange
        var topicSetting = new TopicSetting
        {
            TopicSettingId = 1,
            SensorName = "TempSensor_01_Updated",
            SensorLocation = "North"
        };

        _mockSettingsRepo.Setup(x => x.UpdateTopicSettingAsync(topicSetting))
            .ReturnsAsync(1);

        // Act
        var result = await _controller.UpdateTopic(topicSetting);

        // Assert
        result.Should().BeOfType<StatusCodeResult>();
        var statusResult = (StatusCodeResult)result;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        _mockSettingsRepo.Verify(x => x.UpdateTopicSettingAsync(topicSetting), Times.Once);
    }

    /// <summary>
    /// Tests that UpdateTopic returns BadRequest when topic setting is null.
    /// </summary>
    [Test]
    public async Task UpdateTopic_WithNullTopic_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.UpdateTopic(null!);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _mockSettingsRepo.Verify(x => x.UpdateTopicSettingAsync(It.IsAny<TopicSetting>()), Times.Never);
    }

    /// <summary>
    /// Tests that UpdateTopic returns InternalServerError when service throws exception.
    /// </summary>
    [Test]
    public async Task UpdateTopic_WithException_ShouldReturnInternalServerError()
    {
        // Arrange
        var topicSetting = new TopicSetting
        {
            TopicSettingId = 1,
            SensorName = "TempSensor_01_Updated",
            SensorLocation = "North"
        };

        var exception = new Exception("Database update failed");
        _mockSettingsRepo.Setup(x => x.UpdateTopicSettingAsync(topicSetting))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.UpdateTopic(topicSetting);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    #endregion

    #region DeleteTopic Tests

    /// <summary>
    /// Tests that DeleteTopic returns OK when topic is deleted successfully.
    /// </summary>
    [Test]
    public async Task DeleteTopic_WithValidTopic_ShouldReturnOk()
    {
        // Arrange
        var topicSetting = new TopicSetting
        {
            TopicSettingId = 1,
            SensorName = "TempSensor_01",
            SensorLocation = "North"
        };

        _mockSettingsRepo.Setup(x => x.RemoveTopicSettingAsync(topicSetting))
            .ReturnsAsync(1);

        // Act
        var result = await _controller.DeleteTopic(topicSetting);

        // Assert
        result.Should().BeOfType<StatusCodeResult>();
        var statusResult = (StatusCodeResult)result;
        statusResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        _mockSettingsRepo.Verify(x => x.RemoveTopicSettingAsync(topicSetting), Times.Once);
    }

    /// <summary>
    /// Tests that DeleteTopic returns BadRequest when topic setting is null.
    /// </summary>
    [Test]
    public async Task DeleteTopic_WithNullTopic_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.DeleteTopic(null!);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _mockSettingsRepo.Verify(x => x.RemoveTopicSettingAsync(It.IsAny<TopicSetting>()), Times.Never);
    }

    /// <summary>
    /// Tests that DeleteTopic returns InternalServerError when service throws exception.
    /// </summary>
    [Test]
    public async Task DeleteTopic_WithException_ShouldReturnInternalServerError()
    {
        // Arrange
        var topicSetting = new TopicSetting
        {
            TopicSettingId = 1,
            SensorName = "TempSensor_01",
            SensorLocation = "North"
        };

        var exception = new Exception("Database deletion failed");
        _mockSettingsRepo.Setup(x => x.RemoveTopicSettingAsync(topicSetting))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.DeleteTopic(topicSetting);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    #endregion
}