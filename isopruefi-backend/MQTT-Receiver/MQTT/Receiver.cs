namespace MQTT_Receiver.MQTT;

public class Receiver
{
    public async Task Subscribe()
    {
        var client = await Connection.GetConnection();
        var available = client.SharedSubscriptionAvailable;

        Console.WriteLine(available);
    }
}