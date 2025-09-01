using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace LoadTests.Infrastructure;

/// <summary>
/// Simple MQTT TestContainer using Eclipse Mosquitto
/// </summary>
public static class MqttContainer
{
    public const int MqttPort = 1883;
    public const int WebSocketPort = 9001;
    public const string DefaultImage = "eclipse-mosquitto:2.0";

    /// <summary>
    /// Create a new MQTT container
    /// </summary>
    public static IContainer Create()
    {
        return new ContainerBuilder()
            .WithImage(DefaultImage)
            .WithPortBinding(MqttPort, true)
            .WithPortBinding(WebSocketPort, true)
            .WithCommand("mosquitto", "-c", "/mosquitto-no-auth.conf")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(MqttPort))
            .WithStartupCallback((container, ct) =>
            {
                Console.WriteLine($"MQTT broker started at {container.Hostname}:{container.GetMappedPublicPort(MqttPort)}");
                return Task.CompletedTask;
            })
            .Build();
    }

    /// <summary>
    /// Get MQTT connection string for a container
    /// </summary>
    public static string GetConnectionString(this IContainer container)
    {
        return $"tcp://{container.Hostname}:{container.GetMappedPublicPort(MqttPort)}";
    }
}