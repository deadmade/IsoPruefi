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
    private Worker _worker;

    /// <summary>
    /// Sets up test fixtures and initializes mocks before each test execution.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<Worker>>();
        _mockReceiver = new Mock<IReceiver>();

        _worker = new Worker(_mockLogger.Object, _mockReceiver.Object);
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

        var worker = new Worker(logger, receiver);

        worker.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the Worker constructor throws when logger is null.
    /// </summary>
    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var receiver = Mock.Of<IReceiver>();

        var action = () => new Worker(null!, receiver);

        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that the Worker constructor throws when receiver is null.
    /// </summary>
    [Test]
    public void Constructor_WithNullReceiver_ThrowsArgumentNullException()
    {
        var logger = Mock.Of<ILogger<Worker>>();

        var action = () => new Worker(logger, (IReceiver)null!);

        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ExecuteAsync Tests

    /// <summary>
    /// Tests that ExecuteAsync calls SubscribeToTopics on the receiver.
    /// </summary>
    [Test]
    public async Task ExecuteAsync_CallsReceiverSubscribeToTopics()
    {
        _mockReceiver.Setup(r => r.SubscribeToTopics()).Returns(Task.CompletedTask);
        using var cancellationTokenSource = new CancellationTokenSource();

        await _worker.StartAsync(cancellationTokenSource.Token);

        // Give the background service a moment to start
        await Task.Delay(100, cancellationTokenSource.Token);

        await _worker.StopAsync(cancellationTokenSource.Token);

        _mockReceiver.Verify(r => r.SubscribeToTopics(), Times.Once);
    }

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

    #region Integration Tests

    /// <summary>
    /// Tests the complete worker lifecycle from start to stop.
    /// </summary>
    [Test]
    public async Task WorkerLifecycle_StartAndStop_WorksCorrectly()
    {
        _mockReceiver.Setup(r => r.SubscribeToTopics()).Returns(Task.CompletedTask);
        using var cancellationTokenSource = new CancellationTokenSource();

        // Start the worker
        await _worker.StartAsync(cancellationTokenSource.Token);

        // Verify it's running
        await Task.Delay(50);

        // Stop the worker
        await _worker.StopAsync(cancellationTokenSource.Token);

        _mockReceiver.Verify(r => r.SubscribeToTopics(), Times.Once);
    }

    /// <summary>
    /// Tests that multiple start/stop cycles work correctly.
    /// </summary>
    [Test]
    public async Task WorkerLifecycle_MultipleStartStop_WorksCorrectly()
    {
        _mockReceiver.Setup(r => r.SubscribeToTopics()).Returns(Task.CompletedTask);

        // First cycle
        using (var cts1 = new CancellationTokenSource())
        {
            await _worker.StartAsync(cts1.Token);
            await Task.Delay(50);
            await _worker.StopAsync(cts1.Token);
        }

        // Second cycle
        using (var cts2 = new CancellationTokenSource())
        {
            await _worker.StartAsync(cts2.Token);
            await Task.Delay(50);
            await _worker.StopAsync(cts2.Token);
        }

        _mockReceiver.Verify(r => r.SubscribeToTopics(), Times.Exactly(2));
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