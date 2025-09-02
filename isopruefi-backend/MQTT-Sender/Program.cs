using System.Text.Json;
using MQTT_Receiver_Worker.MQTT.Models;
using MQTTnet;

namespace MQTT_Sender;

/// <summary>
/// This class is the entry point for the MQTT Sender application.
/// </summary>
internal class Program
{
    /// <summary>
    /// Entry point for the Sender application.
    /// </summary>
    /// <param name="args">Arguments passed to the application.</param>
    private static async Task Main(string[] args)
    {
        var client = await Connection.GetConnection();

        var rnd = Random.Shared;
        var sequenceOne = 1;
        var sequenceTwo = 1;

        while (true)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            double?[]? value = [Math.Round(rnd.NextDouble() * 100, 1)];

            var tempGen = new TempSensorReading
            {
                Timestamp = timestamp, Value = value, Sequence = sequenceOne++
            };
            var json = JsonSerializer.Serialize(tempGen);


            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic("dhbw/ai/si2023/2/temp/Sensor_One_Dev")
                .WithPayload(json)
                .Build();

            await client.PublishAsync(applicationMessage, CancellationToken.None);

            json = @"{
    ""timestamp"": 1753952353,
    ""sequence"": null,
    ""value"": [
        null
    ],
    ""meta"": 
        {
            ""t"": [1753884212, 1753884249, 1753884304, 1753884360, 1753884461],
            ""v"": [23.53125, 23.52344, 23.47656, 23.4375, 23.53125],
            ""s"": [0, 1, 2, 3, 0]
        }}";

            applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic("dhbw/ai/si2023/2/temp/Sensor_One_Dev/recovered")
                .WithPayload(json)
                .Build();

            await client.PublishAsync(applicationMessage, CancellationToken.None);

            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            value = [Math.Round(rnd.NextDouble() * 100, 1)];

            tempGen = new TempSensorReading
            {
                Timestamp = timestamp, Value = value, Sequence = sequenceTwo++
            };
            json = JsonSerializer.Serialize(tempGen);

            applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic("dhbw/ai/si2023/2/temp/Sensor_Two_Dev")
                .WithPayload(json)
                .Build();

            await client.PublishAsync(applicationMessage, CancellationToken.None);

            Console.WriteLine("Messages Sent!");

            Thread.Sleep(1000); // Wait for 1 second before sending the next messages
        }
    }
}