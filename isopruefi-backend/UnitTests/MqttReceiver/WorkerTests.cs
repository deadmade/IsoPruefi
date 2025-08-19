using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MQTT_Receiver_Worker;
using MQTT_Receiver_Worker.MQTT.Interfaces;

namespace UnitTests.MqttReceiver;

/// <summary>
/// Unit tests for the Worker class, verifying background service orchestration functionality.
/// </summary>
[TestFixture]
public class WorkerTests
{
    private Mock<ILogger<Worker>> _mockLogger;
    private Mock<IReceiver> _mockReceiver;
    private Mock<IConnection> _mockConnection;
    private Worker _worker;

    /// <summary>
    /// Sets up test fixtures and initializes mocks before each test execution.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<Worker>>();
        _mockReceiver = new Mock<IReceiver>();
        _mockConnection = new Mock<IConnection>();

        _worker = new Worker(_mockLogger.Object, _mockReceiver.Object, _mockConnection.Object);
    }

    /// <summary>
    /// Cleans up resources after each test execution.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        _worker?.Dispose();
    }

    #region Constructor Tests

    /// <summary>
    /// Tests that the Worker constructor properly initializes with valid dependencies.
    /// </summary>
    [Test]
    public void Constructor_WithValidDependencies_InitializesSuccessfully()
    {
        var logger = Mock.Of<ILogger<Worker>>();
        var receiver = Mock.Of<IReceiver>();
        var connection = Mock.Of<IConnection>();

        var worker = new Worker(logger, receiver, connection);

        worker.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the Worker constructor throws when logger is null.
    /// </summary>
    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var receiver = Mock.Of<IReceiver>();
        var connection = Mock.Of<IConnection>();

        var action = () => new Worker(null!, receiver, connection);

        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that the Worker constructor throws when receiver is null.
    /// </summary>
    [Test]
    public void Constructor_WithNullReceiver_ThrowsArgumentNullException()
    {
        var logger = Mock.Of<ILogger<Worker>>();
        var connection = Mock.Of<IConnection>();

        var action = () => new Worker(logger, (IReceiver)null!, connection);

        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ExecuteAsync Tests

    /// <summary>
    /// Tests that ExecuteAsync handles cancellation token properly.
    /// </summary>
    [Test]
    public async Task ExecuteAsync_WithCancellationToken_CompletesGracefully()
    {
        _mockReceiver.Setup(r => r.SubscribeToTopics()).Returns(Task.CompletedTask);
        using var cancellationTokenSource = new CancellationTokenSource();

        await _worker.StartAsync(cancellationTokenSource.Token);

        // Cancel the operation
        cancellationTokenSource.Cancel();

        var stopTask = _worker.StopAsync(CancellationToken.None);
        await stopTask.WaitAsync(TimeSpan.FromSeconds(5));

        stopTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    #endregion


    #region Dispose Tests

    /// <summary>
    /// Tests that the worker disposes properly.
    /// </summary>
    [Test]
    public void Dispose_DisposesWithoutException()
    {
        var action = () => _worker.Dispose();

        action.Should().NotThrow();
    }

    /// <summary>
    /// Tests that the worker can be disposed multiple times without exception.
    /// </summary>
    [Test]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        _worker.Dispose();

        var action = () => _worker.Dispose();

        action.Should().NotThrow();
    }

    #endregion
}
