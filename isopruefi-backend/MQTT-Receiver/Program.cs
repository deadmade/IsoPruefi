using MQTT_Receiver.MQTT;

namespace MQTT_Receiver;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        Console.WriteLine("I like cake");

        var receiver = new Receiver();
        await receiver.Subscribe();
    }
}