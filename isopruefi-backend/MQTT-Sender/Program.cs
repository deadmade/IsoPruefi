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
                .WithTopic("dhbw/ai/si2023/2/temp/Sensor_One")
                .WithPayload(json)
                .Build();

            await client.PublishAsync(applicationMessage, CancellationToken.None);

            json = @"{
    ""timestamp"": 1753952353,
    ""sequence"": null,
    ""value"": [
        null
    ],
    ""meta"": [
        {
            ""timestamp"": 1753884212,
            ""value"": [
                23.53125
            ],
            ""sequence"": 0,
            ""meta"": [
                null
            ]
        },
        {
            ""timestamp"": 1753884249,
            ""value"": [
                23.52344
            ],
            ""sequence"": 1,
            ""meta"": [
                null
            ]
        },
        {
            ""timestamp"": 1753884304,
            ""value"": [
                23.47656
            ],
            ""sequence"": 2,
            ""meta"": [
                null
            ]
        },
        {
            ""timestamp"": 1753884360,
            ""value"": [
                23.4375
            ],
            ""sequence"": 3,
            ""meta"": [
                null
            ]
        },
        {
            ""timestamp"": 1753884461,
            ""value"": [
                23.53125
            ],
            ""sequence"": 0,
            ""meta"": [
                null
            ]
        },
        {
            ""timestamp"": 1753884498,
            ""value"": [
                23.44531
            ],
            ""sequence"": 1,
            ""meta"": [
                null
            ]
        },
        {
            ""timestamp"": 1753885113,
            ""value"": [
                23.55469
            ],
            ""sequence"": 0,
            ""meta"": [
                null
            ]
        },
        {
            ""timestamp"": 1753886978,
            ""value"": [
                23.59375
            ],
            ""sequence"": 0,
            ""meta"": [
                null
            ]
        }
    ]
}";
            
            applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic("dhbw/ai/si2023/2/temp/Sensor_One/recovered")
                .WithPayload(json)
                .Build();
            
            await client.PublishAsync(applicationMessage, CancellationToken.None); 
            
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            value = [Math.Round(rnd.NextDouble() * 100, 1)];

            tempGen = new TempSensorReading { Timestamp = timestamp, Value = value, Sequence = sequenceTwo++ };
            json = System.Text.Json.JsonSerializer.Serialize(tempGen);

            applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic("dhbw/ai/si2023/2/temp/Sensor_Two")
                .WithPayload(json)
                .Build();

            await client.PublishAsync(applicationMessage, CancellationToken.None);

            Console.WriteLine("Messages Sent!");

            Thread.Sleep(1000); // Wait for 1 second before sending the next messages
        }
    }
}