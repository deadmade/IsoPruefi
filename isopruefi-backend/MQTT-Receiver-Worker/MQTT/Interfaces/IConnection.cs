using MQTTnet;

namespace MQTT_Receiver_Worker.MQTT.Interfaces;

/// <summary>
///     Interface for MQTT broker connection management.
///     Provides abstraction for establishing MQTT connections and handling message processing.
/// </summary>
public interface IConnection
{
    /// <summary>
    ///     Gets a value indicating whether the MQTT client is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     Establishes a connection to the MQTT broker and configures message handlers.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the connected MQTT client, or null
    ///     if connection failed.
    /// </returns>
    Task<IMqttClient?> GetConnectionAsync();

    /// <summary>
    ///     Attempts to connect to the MQTT broker.
    /// </summary>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result indicates whether the connection was
    ///     successful.
    /// </returns>
    Task<bool> TryConnectAsync();

    /// <summary>
    ///     Disconnects from the MQTT broker.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DisconnectAsync();
}