using MQTT_Receiver_Worker.MQTT.Models;
using MQTTnet;

namespace MQTT_Sender;

class Program
{
    static async Task Main(string[] args)
    {
        var client = await Connection.GetConnection();

         Random rnd = Random.Shared;
         int sequenceOne = 1;
         int sequenceTwo = 1;

        while (true)
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            double value = Math.Round(rnd.NextDouble() * 100, 1);

            var tempGen = new TempSensorReading { Timestamp = timestamp, Value = value, Sequence = sequenceOne++ };
            string json = System.Text.Json.JsonSerializer.Serialize(tempGen);


            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic("dhbw/ai/si2023/2/temp/SENSOR-ONE")
                .WithPayload(json)
                .Build();

            await client.PublishAsync(applicationMessage, CancellationToken.None);

             timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
             value = Math.Round(rnd.NextDouble() * 100, 1);

            tempGen = new TempSensorReading { Timestamp = timestamp, Value = value, Sequence = sequenceTwo++ };
            json = System.Text.Json.JsonSerializer.Serialize(tempGen);

            applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic("dhbw/ai/si2023/2/temp/SENSOR-TWO")
                .WithPayload(json)
                .Build();

            await client.PublishAsync(applicationMessage, CancellationToken.None);

            Console.WriteLine("Messages Sent!");
        }

    }
}