using Database.EntityFramework.Models;
using Database.Repository.SettingsRepo;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MQTT_Receiver_Worker.MQTT;
using MQTT_Receiver_Worker.MQTT.Interfaces;
using MQTTnet;

namespace UnitTests.MqttReceiver;

/// <summary>
/// Unit tests for the Receiver class, verifying MQTT topic subscription functionality.
/// </summary>
[TestFixture]
public class ReceiverTests
{
    private Mock<IServiceProvider> _mockServiceProvider;
    private Mock<IServiceScope> _mockServiceScope;
    private Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private Mock<ISettingsRepo> _mockSettingsRepo;
    private Mock<IConnection> _mockConnection;
    private Mock<IMqttClient> _mockMqttClient;
    private Mock<ILogger<Receiver>> _mockLogger;
    private Mock<IConfiguration> _mockConfiguration;
    private Receiver _receiver;
    private List<TopicSetting> _testTopicSettings;

    /// <summary>
    /// Sets up test fixtures and initializes mocks before each test execution.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockSettingsRepo = new Mock<ISettingsRepo>();
        _mockMqttClient = new Mock<IMqttClient>();
        _mockLogger = new Mock<ILogger<Receiver>>();
        _mockConfiguration = new Mock<IConfiguration>();

        var mockScopeServiceProvider = new Mock<IServiceProvider>();
        mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(ISettingsRepo)))
            .Returns(_mockSettingsRepo.Object);
        _mockServiceScope.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);

        _mockServiceScopeFactory.Setup(f => f.CreateScope()).Returns(_mockServiceScope.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockServiceScopeFactory.Object);

        // Create mock connection that returns our mock MQTT client
        _mockConnection = new Mock<IConnection>();
        _mockConnection.Setup(c => c.GetConnectionAsync()).ReturnsAsync(_mockMqttClient.Object);

        // Setup test topic settings
        _testTopicSettings = new List<TopicSetting>
        {
            new()
            {
                TopicSettingId = 1,
                DefaultTopicPath = "sensors",
                GroupId = 1,
                SensorType = "temperature",
                SensorName = "sensor1",
                HasRecovery = true
            },
            new()
            {
                TopicSettingId = 2,
                DefaultTopicPath = "sensors",
                GroupId = 2,
                SensorType = "humidity",
                SensorName = "sensor2",
                HasRecovery = false
            }
        };

        _mockSettingsRepo.Setup(r => r.GetTopicSettingsAsync())
            .ReturnsAsync(_testTopicSettings);

        _receiver = new Receiver(_mockServiceProvider.Object, _mockConnection.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    /// <summary>
    /// Tests that the Receiver constructor properly initializes with valid dependencies.
    /// </summary>
    [Test]
    public void Constructor_WithValidDependencies_InitializesSuccessfully()
    {
        var serviceProvider = Mock.Of<IServiceProvider>();
        var connection = Mock.Of<IConnection>();
        var logger = Mock.Of<ILogger<Receiver>>();

        var receiver = new Receiver(serviceProvider, connection, logger);

        receiver.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the Receiver constructor throws when service provider is null.
    /// </summary>
    [Test]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        var connection = Mock.Of<IConnection>();
        var logger = Mock.Of<ILogger<Receiver>>();

        var action = () => new Receiver(null!, connection, logger);

        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that the Receiver constructor throws when connection is null.
    /// </summary>
    [Test]
    public void Constructor_WithNullConnection_ThrowsArgumentNullException()
    {
        var serviceProvider = Mock.Of<IServiceProvider>();
        var logger = Mock.Of<ILogger<Receiver>>();

        var action = () => new Receiver(serviceProvider, null!, logger);

        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that the Receiver constructor throws when logger is null.
    /// </summary>
    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var serviceProvider = Mock.Of<IServiceProvider>();
        var connection = Mock.Of<IConnection>();

        var action = () => new Receiver(serviceProvider, connection, null!);

        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region SubscribeToTopics Tests

    /// <summary>
    /// Tests that SubscribeToTopics retrieves topic settings and establishes connection.
    /// </summary>
    [Test]
    public async Task SubscribeToTopics_WithValidSettings_RetrievesSettingsAndConnects()
    {
        await _receiver.SubscribeToTopics();

        // Verify that settings were retrieved
        _mockSettingsRepo.Verify(r => r.GetTopicSettingsAsync(), Times.Once);

        // Verify that connection was established
        _mockConnection.Verify(c => c.GetConnectionAsync(), Times.Once);
    }

    /// <summary>
    /// Tests that SubscribeToTopics handles empty topic settings gracefully.
    /// </summary>
    [Test]
    public async Task SubscribeToTopics_WithEmptySettings_CompletesWithoutError()
    {
        _mockSettingsRepo.Setup(r => r.GetTopicSettingsAsync())
            .ReturnsAsync(new List<TopicSetting>());

        await _receiver.SubscribeToTopics();

        _mockSettingsRepo.Verify(r => r.GetTopicSettingsAsync(), Times.Once);
        _mockConnection.Verify(c => c.GetConnectionAsync(), Times.Once);
    }

    /// <summary>
    /// Tests that SubscribeToTopics handles null topic settings gracefully.
    /// </summary>
    [Test]
    public async Task SubscribeToTopics_WithNullSettings_ThrowsException()
    {
        _mockSettingsRepo.Setup(r => r.GetTopicSettingsAsync())
            .ReturnsAsync((List<TopicSetting>)null!);

        var action = async () => await _receiver.SubscribeToTopics();

        await action.Should().ThrowAsync<NullReferenceException>();
    }

    /// <summary>
    /// Tests that SubscribeToTopics handles repository exceptions.
    /// </summary>
    [Test]
    public async Task SubscribeToTopics_WhenRepositoryThrows_PropagatesException()
    {
        var expectedException = new InvalidOperationException("Database error");
        _mockSettingsRepo.Setup(r => r.GetTopicSettingsAsync())
            .ThrowsAsync(expectedException);

        var action = async () => await _receiver.SubscribeToTopics();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");
    }

    /// <summary>
    /// Tests that SubscribeToTopics handles connection failures.
    /// </summary>
    [Test]
    public async Task SubscribeToTopics_WhenConnectionFails_PropagatesException()
    {
        var expectedException = new InvalidOperationException("Connection failed");
        _mockConnection.Setup(c => c.GetConnectionAsync())
            .ThrowsAsync(expectedException);

        var action = async () => await _receiver.SubscribeToTopics();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Connection failed");
    }

    #endregion

    #region Service Provider Scope Tests

    /// <summary>
    /// Tests that SubscribeToTopics properly creates and disposes of service scope.
    /// </summary>
    [Test]
    public async Task SubscribeToTopics_CreatesAndDisposesServiceScope()
    {
        await _receiver.SubscribeToTopics();

        _mockServiceScopeFactory.Verify(f => f.CreateScope(), Times.Once);
        _mockServiceScope.Verify(s => s.Dispose(), Times.Once);
    }

    /// <summary>
    /// Tests that SubscribeToTopics disposes scope even when exception occurs.
    /// </summary>
    [Test]
    public async Task SubscribeToTopics_DisposesScope_EvenWhenExceptionOccurs()
    {
        _mockSettingsRepo.Setup(r => r.GetTopicSettingsAsync())
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var action = async () => await _receiver.SubscribeToTopics();

        await action.Should().ThrowAsync<InvalidOperationException>();
        _mockServiceScope.Verify(s => s.Dispose(), Times.Once);
    }

    #endregion

    #region Integration Tests

    /// <summary>
    /// Tests the complete subscription workflow with realistic data.
    /// </summary>
    [Test]
    public async Task SubscribeToTopics_CompleteWorkflow_WorksCorrectly()
    {
        await _receiver.SubscribeToTopics();

        // Verify the complete workflow
        _mockServiceScopeFactory.Verify(f => f.CreateScope(), Times.Once);
        _mockSettingsRepo.Verify(r => r.GetTopicSettingsAsync(), Times.Once);
        _mockConnection.Verify(c => c.GetConnectionAsync(), Times.Once);
        _mockServiceScope.Verify(s => s.Dispose(), Times.Once);
    }

    #endregion
}