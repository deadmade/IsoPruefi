using MQTT_Receiver_Worker.MQTT.Models;
using MQTTnet;

namespace MQTT_Sender;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var client = await Connection.GetConnection();

        var rnd = Random.Shared;
        var sequenceOne = 1;
        var sequenceTwo = 1;

        while (true)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            double[] value = [Math.Round(rnd.NextDouble() * 100, 1)];

            var tempGen = new TempSensorReading { Timestamp = timestamp, Value = value, Sequence = sequenceOne++ };
            var json = System.Text.Json.JsonSerializer.Serialize(tempGen);


            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic("dhbw/ai/si2023/2/temp/SENSOR-ONE")
                .WithPayload(json)
                .Build();

            await client.PublishAsync(applicationMessage, CancellationToken.None);

            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            value = [Math.Round(rnd.NextDouble() * 100, 1)];

            tempGen = new TempSensorReading { Timestamp = timestamp, Value = value, Sequence = sequenceTwo++ };
            json = System.Text.Json.JsonSerializer.Serialize(tempGen);

            applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic("dhbw/ai/si2023/2/temp/SENSOR-TWO")
                .WithPayload(json)
                .Build();

            await client.PublishAsync(applicationMessage, CancellationToken.None);

            Console.WriteLine("Messages Sent!");

            Thread.Sleep(1000); // Wait for 1 second before sending the next messages
        }
    }
}