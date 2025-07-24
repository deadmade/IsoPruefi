#include <string>
#include <cstring>
#include <cstdio>

// Test MQTT topic generation
std::string generateMqttTopic(const char* baseTopic, const char* sensorType, const char* sensorId) {
  char fullTopic[128];
  snprintf(fullTopic, sizeof(fullTopic), "%s%s/%s", baseTopic, sensorType, sensorId);
  return std::string(fullTopic);
}

void test_generateMqttTopic_creates_correct_format() {
  const char* baseTopic = "dhbw/ai/si2023/2/";
  const char* sensorType = "temp";
  const char* sensorId = "Sensor_One";
  
  std::string result = generateMqttTopic(baseTopic, sensorType, sensorId);
  
  if (result != "dhbw/ai/si2023/2/temp/Sensor_One") {
    printf("Test failed: expected 'dhbw/ai/si2023/2/temp/Sensor_One', got '%s'\n", result.c_str());
  }
}

void test_generateMqttTopic_handles_different_sensors() {
  const char* baseTopic = "dhbw/ai/si2023/2/";
  const char* sensorType = "temp";
  
  std::string result1 = generateMqttTopic(baseTopic, sensorType, "Sensor_One");
  std::string result2 = generateMqttTopic(baseTopic, sensorType, "Sensor_Two");
  
  if (result1 != "dhbw/ai/si2023/2/temp/Sensor_One") {
    printf("Test failed: expected 'dhbw/ai/si2023/2/temp/Sensor_One', got '%s'\n", result1.c_str());
  }
  if (result2 != "dhbw/ai/si2023/2/temp/Sensor_Two") {
    printf("Test failed: expected 'dhbw/ai/si2023/2/temp/Sensor_Two', got '%s'\n", result2.c_str());
  }
}

int main() {
  test_generateMqttTopic_creates_correct_format();
  test_generateMqttTopic_handles_different_sensors();
  printf("All tests completed.\n");
  return 0;
}
