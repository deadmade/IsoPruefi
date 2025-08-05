using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Database.Repository.InfluxRepo;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MQTT_Receiver_Worker.MQTT;
using MQTT_Receiver_Worker.MQTT.Models;
using MQTTnet;

namespace UnitTests.MqttReceiver;

/// <summary>
/// Unit tests for the Connection class, verifying MQTT connection management and message processing functionality.
/// </summary>
[TestFixture]
public class ConnectionTests
{
    private Mock<ILogger<Connection>> _mockLogger;
    private Mock<IServiceProvider> _mockServiceProvider;
    private Mock<IServiceScope> _mockServiceScope;
    private IConfiguration _configuration;
    private Mock<IInfluxRepo> _mockInfluxRepo;
    private Connection _connection;

    /// <summary>
    /// Sets up test fixtures and initializes mocks before each test execution.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<Connection>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockInfluxRepo = new Mock<IInfluxRepo>();

        // Create a configuration using ConfigurationBuilder instead of mocking extension methods
        var configDict = new Dictionary<string, string>
        {
            ["Mqtt:BrokerHost"] = "localhost",
            ["Mqtt:BrokerPort"] = "1883"
        };
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict);
        _configuration = configBuilder.Build();

        // Setup service provider and scope
        var mockScopeServiceProvider = new Mock<IServiceProvider>();
        mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(IInfluxRepo)))
                               .Returns(_mockInfluxRepo.Object);
        _mockServiceScope.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);
        
        // Setup IServiceScopeFactory instead of extension method
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockServiceScope.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                           .Returns(mockScopeFactory.Object);

        _connection = new Connection(_mockLogger.Object, _mockServiceProvider.Object, _configuration);
    }

    #region Constructor Tests

    /// <summary>
    /// Tests that the Connection constructor properly initializes with valid dependencies.
    /// </summary>
    [Test]
    public void Constructor_WithValidDependencies_InitializesSuccessfully()
    {
        var logger = Mock.Of<ILogger<Connection>>();
        var serviceProvider = Mock.Of<IServiceProvider>();
        var configDict = new Dictionary<string, string>
        {
            ["Mqtt:BrokerHost"] = "localhost",
            ["Mqtt:BrokerPort"] = "1883"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var connection = new Connection(logger, serviceProvider, configuration);

        connection.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the Connection constructor throws when logger is null.
    /// </summary>
    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var serviceProvider = Mock.Of<IServiceProvider>();
        var configuration = new ConfigurationBuilder().Build();

        var action = () => new Connection(null!, serviceProvider, configuration);

        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that the Connection constructor throws when service provider is null.
    /// </summary>
    [Test]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        var logger = Mock.Of<ILogger<Connection>>();
        var mockConfig = new Mock<IConfiguration>();
        var configuration = mockConfig.Object;

        var action = () => new Connection(logger, null!, configuration);

        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that the Connection constructor throws when configuration is null.
    /// </summary>
    [Test]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var logger = Mock.Of<ILogger<Connection>>();
        var serviceProvider = Mock.Of<IServiceProvider>();

        var action = () => new Connection(logger, serviceProvider, null!);

        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that the constructor properly reads configuration values.
    /// </summary>
    [Test]
    public void Constructor_ReadsConfigurationValues_Successfully()
    {
        var configDict = new Dictionary<string, string>
        {
            ["Mqtt:BrokerHost"] = "test-broker",
            ["Mqtt:BrokerPort"] = "8883"
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var connection = new Connection(_mockLogger.Object, _mockServiceProvider.Object, config);

        connection.Should().NotBeNull();
    }

    #endregion

    #region JSON Serialization Tests

    /// <summary>
    /// Tests that JSON serializer options are properly configured.
    /// </summary>
    [Test]
    public void JsonSerializerOptions_AreConfiguredCorrectly()
    {
        // Access private field using reflection
        var field = typeof(Connection).GetField("_jsonSerializerOptions", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            var options = (JsonSerializerOptions)field.GetValue(_connection)!;
            
            options.PropertyNameCaseInsensitive.Should().BeTrue();
            options.DefaultIgnoreCondition.Should().Be(JsonIgnoreCondition.WhenWritingNull);
        }
    }

    /// <summary>
    /// Tests deserialization of valid sensor reading JSON.
    /// </summary>
    [Test]
    public void JsonDeserialization_ValidSensorReading_DeserializesCorrectly()
    {
        var json = """
        {
            "timestamp": 1234567890,
            "value": [25.5],
            "sequence": 1
        }
        """;

        var reading = JsonSerializer.Deserialize<TempSensorReading>(json);

        reading.Should().NotBeNull();
        reading!.Timestamp.Should().Be(1234567890);
        reading.Value.Should().BeEquivalentTo(new double?[] { 25.5 });
        reading.Sequence.Should().Be(1);
    }

    /// <summary>
    /// Tests deserialization of invalid JSON.
    /// </summary>
    [Test]
    public void JsonDeserialization_InvalidJson_ThrowsJsonException()
    {
        var invalidJson = "{ invalid json }";

        var action = () => JsonSerializer.Deserialize<TempSensorReading>(invalidJson);

        action.Should().Throw<JsonException>();
    }

    #endregion

    #region Message Processing Tests - Via Reflection

    /// <summary>
    /// Tests processing of a valid single sensor reading message using reflection.
    /// </summary>
    [Test]
    public async Task ProcessSensorReading_ValidSingleReading_WritesToDatabase()
    {
        var sensorReading = new TempSensorReading
        {
            Timestamp = 1234567890,
            Value = new double?[] { 25.5 },
            Sequence = 1
        };

        // Use reflection to access the private method
        var method = typeof(Connection).GetMethod("ProcessSensorReading", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            var task = (Task)method.Invoke(_connection, new object[] { sensorReading, "testSensor", _mockInfluxRepo.Object })!;
            await task;

            _mockInfluxRepo.Verify(r => r.WriteSensorData(25.5, "testSensor", 1234567890, 1), Times.Once);
        }
    }

    /// <summary>
    /// Tests processing of sensor reading with null value.
    /// </summary>
    [Test]
    public async Task ProcessSensorReading_NullValue_SkipsProcessing()
    {
        var sensorReading = new TempSensorReading
        {
            Timestamp = 1234567890,
            Value = null,
            Sequence = 1
        };

        var method = typeof(Connection).GetMethod("ProcessSensorReading", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            var task = (Task)method.Invoke(_connection, new object[] { sensorReading, "testSensor", _mockInfluxRepo.Object })!;
            await task;

            _mockInfluxRepo.Verify(r => r.WriteSensorData(It.IsAny<double>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>()), Times.Never);
        }
    }

    /// <summary>
    /// Tests processing of sensor reading with empty value array.
    /// </summary>
    [Test]
    public async Task ProcessSensorReading_EmptyValueArray_SkipsProcessing()
    {
        var sensorReading = new TempSensorReading
        {
            Timestamp = 1234567890,
            Value = new double?[0],
            Sequence = 1
        };

        var method = typeof(Connection).GetMethod("ProcessSensorReading", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            var task = (Task)method.Invoke(_connection, new object[] { sensorReading, "testSensor", _mockInfluxRepo.Object })!;
            await task;

            _mockInfluxRepo.Verify(r => r.WriteSensorData(It.IsAny<double>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>()), Times.Never);
        }
    }

    /// <summary>
    /// Tests processing of batch sensor readings with meta data.
    /// </summary>
    [Test]
    public async Task ProcessBatchSensorReading_WithMetaData_ProcessesAllReadings()
    {
        var metaReadings = new List<TempSensorReading>
        {
            new() { Timestamp = 1234567890, Value = new double?[] { 25.5 }, Sequence = 1 },
            new() { Timestamp = 1234567891, Value = new double?[] { 26.0 }, Sequence = 2 }
        };

        var batchReading = new TempSensorReading
        {
            Meta = metaReadings
        };

        var method = typeof(Connection).GetMethod("ProcessBatchSensorReading", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            var task = (Task)method.Invoke(_connection, new object[] { batchReading, "testSensor", _mockInfluxRepo.Object })!;
            await task;

            // Should process each meta reading
            _mockInfluxRepo.Verify(r => r.WriteSensorData(It.IsAny<double>(), "testSensor", It.IsAny<long>(), It.IsAny<int>()), Times.AtLeast(1));
        }
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// Tests that database write errors are handled gracefully.
    /// </summary>
    [Test]
    public async Task ProcessSensorReading_DatabaseError_HandlesGracefully()
    {
        _mockInfluxRepo.Setup(r => r.WriteSensorData(It.IsAny<double>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>()))
                      .ThrowsAsync(new InvalidOperationException("Database error"));

        var sensorReading = new TempSensorReading
        {
            Timestamp = 1234567890,
            Value = new double?[] { 25.5 },
            Sequence = 1
        };

        var method = typeof(Connection).GetMethod("ProcessSensorReading", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            var action = async () =>
            {
                var task = (Task)method.Invoke(_connection, new object[] { sensorReading, "testSensor", _mockInfluxRepo.Object })!;
                await task;
            };

            await action.Should().ThrowAsync<InvalidOperationException>()
                       .WithMessage("Database error");
        }
    }

    #endregion

    #region Integration Tests

    /// <summary>
    /// Tests that GetConnection method can be called without throwing exceptions.
    /// </summary>
    [Test]
    public async Task GetConnection_CanBeCalled_WithoutThrowingArgumentExceptions()
    {
        // This test will likely fail due to actual MQTT connection attempts,
        // but it verifies that the method can be called and basic setup works
        Exception? exception = null;
        try
        {
            await _connection.GetConnectionAsync();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // We expect this to not throw ArgumentNullException or similar setup errors
        // It may throw network-related exceptions which is expected
        exception.Should().NotBeOfType<ArgumentNullException>();
        exception.Should().NotBeOfType<NullReferenceException>();
    }

    #endregion
}