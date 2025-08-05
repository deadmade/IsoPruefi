using MQTTnet;

namespace MQTT_Receiver_Worker.MQTT.Interfaces;

/// <summary>
/// Interface for MQTT broker connection management.
/// Provides abstraction for establishing MQTT connections and handling message processing.
/// </summary>
public interface IConnection
{
    /// <summary>
    /// Establishes a connection to the MQTT broker and configures message handlers.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the connected MQTT client.</returns>
    Task<IMqttClient> GetConnection();

    Task DisconnectAsync();
    Task<bool> TryConnectAsync();

    bool IsConnected { get; }
}