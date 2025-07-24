namespace Database.Repository.InfluxRepo;

public interface IInfluxRepo
{
     Task WriteSensorData(double measurement, string sensor, long timestamp, int sequence);
}