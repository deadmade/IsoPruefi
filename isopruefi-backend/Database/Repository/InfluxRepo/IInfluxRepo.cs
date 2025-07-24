namespace Database.Repository.InfluxRepo;

public interface IInfluxRepo
{

      Task WriteSensorData(double measurement, string sensor, long timestamp);

      Task WriteOutsideWeatherData(string place, string website, double temperature, DateTime timestamp);
}