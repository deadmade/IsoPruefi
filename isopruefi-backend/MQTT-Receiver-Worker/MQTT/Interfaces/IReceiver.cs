namespace MQTT_Receiver_Worker.MQTT.Interfaces;

/// <summary>
/// Interface for MQTT topic subscription management.
/// Provides abstraction for subscribing to configured MQTT topics.
/// </summary>
public interface IReceiver
{
    /// <summary>
    /// Subscribes to configured MQTT topics using shared subscriptions.
    /// Retrieves topic settings from the repository and establishes subscriptions
    /// </summary>
    /// <returns>A task that represents the asynchronous subscribe operation.</returns>
    Task SubscribeToTopics();
}