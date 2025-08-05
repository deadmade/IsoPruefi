using System.Net;
using Database.EntityFramework.Models;
using Database.Repository.SettingsRepo;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Rest_API.Services.Temp;

namespace UnitTests.Services;

/// <summary>
/// Unit tests for the TempService class, verifying temperature operations.
/// </summary>
[TestFixture]
public class TempServiceTests
{
    private Mock<ILogger<TempService>> _mockLogger;
    private Mock<IHttpClientFactory> _mockHttpClientFactory;
    private Mock<ISettingsRepo> _mockSettingsRepo;
    private TempService _tempService;

    /// <summary>
    /// Sets up test fixtures and initializes mocks before each test execution.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<TempService>>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockSettingsRepo = new Mock<ISettingsRepo>();

        _tempService = new TempService(
            _mockLogger.Object,
            _mockHttpClientFactory.Object,
            _mockSettingsRepo.Object
            );

    }
    
    
    #region Get Coordinates Test

    /// <summary>
    /// Tests that the GetCoordinates function calls the api when there is no existing entry in the database.
    /// </summary>
    [Test]
    public async Task GetCoordinates_WhenNoExistingEntry_ShouldCallApi()
    {
        // Arrange
        var postalCode = 12345;
        _mockSettingsRepo.Setup(x => x.ExistsPostalCode(postalCode)).ReturnsAsync(false);

        // Create a mock HttpMessageHandler to mock HttpMessage.
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[{\"lat\":\"52.5\",\"lon\":\"13.4\",\"display_name\":\"City, Region, Country\"}]")
            });

        var httpClient = new HttpClient(handlerMock.Object);

        _mockHttpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Act & Assert
        await _tempService.GetCoordinates(postalCode);
        
        _mockSettingsRepo.Verify<Task>(r => r.InsertNewPostalCode(It.Is<CoordinateMapping>(c =>
                c.PostalCode == postalCode &&
                c.Latitude == 52.5 &&
                c.Longitude == 13.4 &&
                c.Location == " Region"
            )), Times.Once);
    }

    /// <summary>
    /// Tests that the GetCoordinates function does not call the api when there is an entry in the database.
    /// </summary>
    [Test]
    public async Task GetCoordinates_WhenExistingEntry_ShouldNotCallAPi()
    {
        // Arrange
        var postalCode = 12345;
        _mockSettingsRepo.Setup(x => x.ExistsPostalCode(postalCode)).ReturnsAsync(true);
        
        // Act & Assert
        await _tempService.GetCoordinates(postalCode);
        
        _mockHttpClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        _mockSettingsRepo.Verify(r => r.InsertNewPostalCode(It.IsAny<CoordinateMapping>()), Times.Never);
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("There is an existing entry for that postalcode")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    /// <summary>
    /// Tests that the GetCoordinates function does not insert anything into the database, when there are missing fields in the JSON response.
    /// </summary>
    [Test]
    public async Task GetCoordinates_WhenMissingFields_ShouldNotInsert()
    {
        // Arrange
        var postalCode = 12345;
        _mockSettingsRepo.Setup(x => x.ExistsPostalCode(postalCode)).ReturnsAsync(false);

        // Create a mock HttpMessageHandler to mock HttpMessage.
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[{\"display_name\":\"City, Region, Country\"}]")
            });

        var httpClient = new HttpClient(handlerMock.Object);

        _mockHttpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);
        
        // Act & Assert
        await _tempService.GetCoordinates(postalCode);
        
        _mockSettingsRepo.Verify(r => r.InsertNewPostalCode(It.IsAny<CoordinateMapping>()), Times.Never);
        _mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Coordinates and city name could not be retrieved")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), 
            Times.Once);
    }

    /// <summary>
    /// Tests that the GetCoordinate function does not insert anything if the api returns a message with an error code.
    /// </summary>
    [Test]
    public async Task GetCoordinates_ApiErrorCode_ShouldNotInsert()
    {
        // Arrange
        var postalCode = 12345;
        _mockSettingsRepo.Setup(x => x.ExistsPostalCode(postalCode)).ReturnsAsync(false);

        // Create a mock HttpMessageHandler to mock HttpMessage.
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden
            });
        
        var httpClient = new HttpClient(handlerMock.Object);

        _mockHttpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);
        
        // Act & Assert
        await _tempService.GetCoordinates(postalCode);
        
        _mockSettingsRepo.Verify(r => r.InsertNewPostalCode(It.IsAny<CoordinateMapping>()), Times.Never);
        _mockLogger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting coordinates failed with HTTP status code: " + HttpStatusCode.Forbidden)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), 
            Times.Once);
    }
    
    #endregion
}