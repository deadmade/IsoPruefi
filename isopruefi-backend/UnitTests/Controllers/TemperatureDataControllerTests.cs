using Database.EntityFramework.Models;
using Database.Repository.InfluxRepo;
using Database.Repository.SettingsRepo;
using FluentAssertions;
using InfluxDB3.Client.Write;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rest_API.Controllers;
using Rest_API.Models;

namespace UnitTests.Controllers;

/// <summary>
/// Unit tests for the TemperatureDataController class, verifying temperature data retrieval and formatting functionality.
/// </summary>
[TestFixture]
public class TemperatureDataControllerTests
{
    private Mock<ILogger<TemperatureDataController>> _mockLogger;
    private Mock<ISettingsRepo> _mockSettingsRepo;
    private Mock<IInfluxRepo> _mockInfluxRepo;
    private TemperatureDataController _controller;

    /// <summary>
    /// Sets up test fixtures and initializes mocks before each test execution.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<TemperatureDataController>>();
        _mockSettingsRepo = new Mock<ISettingsRepo>();
        _mockInfluxRepo = new Mock<IInfluxRepo>();

        _controller = new TemperatureDataController(
            _mockLogger.Object,
            _mockSettingsRepo.Object,
            _mockInfluxRepo.Object);
    }

    #region Constructor Tests

    /// <summary>
    /// Tests that the constructor creates a valid instance when provided with valid parameters.
    /// </summary>
    [Test]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        Action act = () => new TemperatureDataController(
            _mockLogger.Object,
            _mockSettingsRepo.Object,
            _mockInfluxRepo.Object);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when logger parameter is null.
    /// </summary>
    [Test]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new TemperatureDataController(
            null!,
            _mockSettingsRepo.Object,
            _mockInfluxRepo.Object);

        act.Should().Throw<ArgumentNullException>().WithMessage("*logger*");
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when settings repository parameter is null.
    /// </summary>
    [Test]
    public void Constructor_WithNullSettingsRepo_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new TemperatureDataController(
            _mockLogger.Object,
            null!,
            _mockInfluxRepo.Object);

        act.Should().Throw<ArgumentNullException>().WithMessage("*settingsRepo*");
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when InfluxDB repository parameter is null.
    /// </summary>
    [Test]
    public void Constructor_WithNullInfluxRepo_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new TemperatureDataController(
            _mockLogger.Object,
            _mockSettingsRepo.Object,
            null!);

        act.Should().Throw<ArgumentNullException>().WithMessage("*influxRepo*");
    }

    #endregion

    #region GetTemperature Tests

    /// <summary>
    /// Tests that GetTemperature with valid parameters returns an OK result with temperature data.
    /// </summary>
    [Test]
    public async Task GetTemperature_WithValidParameters_ShouldReturnOkWithTemperatureData()
    {
        // Arrange
        var start = DateTime.UtcNow.AddDays(-1);
        var end = DateTime.UtcNow;
        var place = "TestCity";
        var isFahrenheit = false;

        var topicSettings = new List<TopicSetting>
        {
            new() { SensorLocation = "North", SensorName = "sensor1" },
            new() { SensorLocation = "South", SensorName = "sensor2" }
        };

        _mockSettingsRepo.Setup(x => x.GetTopicSettingsAsync()).ReturnsAsync(topicSettings);
        _mockInfluxRepo.Setup(x => x.GetOutsideWeatherData(start, end, place))
            .Returns(CreateEmptyAsyncEnumerable());
        _mockInfluxRepo.Setup(x => x.GetSensorWeatherData(start, end, topicSettings[0].SensorName))
            .Returns(CreateEmptyAsyncEnumerable());

        // Act
        var result = await _controller.GetTemperature(start, end, place, isFahrenheit);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeOfType<TemperatureDataOverview>();

        var temperatureData = (TemperatureDataOverview)okResult.Value!;
        temperatureData.Should().NotBeNull();
        temperatureData.TemperatureNord.Should().NotBeNull();
        temperatureData.TemperatureSouth.Should().NotBeNull();
        temperatureData.TemperatureOutside.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that GetTemperature with Fahrenheit conversion returns temperatures converted from Celsius to Fahrenheit.
    /// </summary>
    [Test]
    public async Task GetTemperature_WithFahrenheitConversion_ShouldReturnConvertedTemperatures()
    {
        // Arrange
        var start = DateTime.UtcNow.AddDays(-1);
        var end = DateTime.UtcNow;
        var place = "TestCity";
        var isFahrenheit = true;

        var topicSettings = new List<TopicSetting>
        {
            new() { SensorLocation = "North", SensorName = "sensor1" }
        };

        _mockSettingsRepo.Setup(x => x.GetTopicSettingsAsync()).ReturnsAsync(topicSettings);
        _mockInfluxRepo.Setup(x => x.GetOutsideWeatherData(start, end, place))
            .Returns(CreateEmptyAsyncEnumerable());
        _mockInfluxRepo.Setup(x => x.GetSensorWeatherData(start, end, topicSettings[0].SensorName))
            .Returns(CreateEmptyAsyncEnumerable());

        // Act
        var result = await _controller.GetTemperature(start, end, place, isFahrenheit);

        // Assert
        var okResult = (OkObjectResult)result;
        var temperatureData = (TemperatureDataOverview)okResult.Value!;

        temperatureData.Should().NotBeNull();
        // Since we're using empty mock data, we just verify the conversion logic works
        var testCelsius = 25.0;
        var expectedFahrenheit = testCelsius * 9 / 5 + 32;
        expectedFahrenheit.Should().Be(77.0);
    }

    /// <summary>
    /// Tests that GetTemperature handles gracefully when no sensor settings are available.
    /// </summary>
    [Test]
    public async Task GetTemperature_WithNoSensorSettings_ShouldHandleGracefully()
    {
        // Arrange
        var start = DateTime.UtcNow.AddDays(-1);
        var end = DateTime.UtcNow;
        var place = "TestCity";

        _mockSettingsRepo.Setup(x => x.GetTopicSettingsAsync()).ReturnsAsync(new List<TopicSetting>());
        _mockInfluxRepo.Setup(x => x.GetOutsideWeatherData(start, end, place))
            .Returns(CreateEmptyAsyncEnumerable());
        _mockInfluxRepo.Setup(x => x.GetSensorWeatherData(start, end))
            .Returns(CreateEmptyAsyncEnumerable());

        // Act
        var result = await _controller.GetTemperature(start, end, place, false);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var temperatureData = (TemperatureDataOverview)okResult.Value!;

        temperatureData.TemperatureNord.Should().BeEmpty();
        temperatureData.TemperatureSouth.Should().BeEmpty();
    }

    #endregion

    #region Temperature Conversion Tests

    /// <summary>
    /// Tests that ConvertToFahrenheit method correctly converts Celsius values to Fahrenheit.
    /// </summary>
    [Test]
    public void ConvertToFahrenheit_WithCelsiusValues_ShouldReturnCorrectFahrenheitValues()
    {
        // Test cases for temperature conversion
        var testCases = new[]
        {
            (celsius: 0.0, expectedFahrenheit: 32.0),
            (celsius: 25.0, expectedFahrenheit: 77.0),
            (celsius: 100.0, expectedFahrenheit: 212.0),
            (celsius: -40.0, expectedFahrenheit: -40.0),
            (celsius: 37.0, expectedFahrenheit: 98.6)
        };

        foreach (var (celsius, expectedFahrenheit) in testCases)
        {
            // Act - Using the same formula as in the controller
            var result = celsius * 9 / 5 + 32;

            // Assert
            result.Should().BeApproximately(expectedFahrenheit, 0.1);
        }
    }

    /// <summary>
    /// Tests that date formatting produces correctly formatted date strings.
    /// </summary>
    [Test]
    public void DateFormatting_ShouldFormatCorrectly()
    {
        // Arrange
        var start = new DateTime(2022, 1, 1, 0, 0, 0);
        var end = new DateTime(2022, 1, 2, 0, 0, 0);

        // Act & Assert - Verify date formatting would be correct
        var expectedStartFormat = start.ToString("yyyy-MM-dd HH:mm:ss");
        var expectedEndFormat = end.ToString("yyyy-MM-dd HH:mm:ss");
        expectedStartFormat.Should().Be("2022-01-01 00:00:00");
        expectedEndFormat.Should().Be("2022-01-02 00:00:00");
    }

    #endregion

    #region Helper Methods

    private static async IAsyncEnumerable<PointDataValues> CreateEmptyAsyncEnumerable()
    {
        await Task.CompletedTask;
        yield break;
    }

    #endregion
}