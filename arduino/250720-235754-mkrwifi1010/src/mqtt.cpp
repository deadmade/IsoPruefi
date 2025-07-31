#include "mqtt.h"
#include "storage.h"

void sendToMqtt(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                const char* sensorId, float celsius, const DateTime& now, int sequence) {
  mqttClient.poll();

  char fullTopic[128];
  snprintf(fullTopic, sizeof(fullTopic), "%s%s/%s", topicPrefix, sensorType, sensorId);

  StaticJsonDocument<128> jsonDoc;
  buildJson(jsonDoc, celsius, now, sequence);

  char payload[128];
  serializeJson(jsonDoc, payload, sizeof(payload));

  if (mqttClient.beginMessage(fullTopic)) {
    mqttClient.print(payload);
    mqttClient.endMessage();
    Serial.print("Published to ");
    Serial.println(fullTopic);
    Serial.println(payload);
  } else {
    Serial.println("MQTT publish failed.");
  }
}
void sendPendingData(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                     const char* sensorId, const DateTime& now) {
  Serial.println("Sending pending data");

  String fileList[500];
  int count = listSavedFilesData(fileList, 500, now);
  Serial.println("Pending files count: " + String(count));

  if (count == 0) return;

  StaticJsonDocument<1024> mainDoc;
  buildRecoveredJson(mainDoc, fileList, count, now);

  if (!mainDoc.containsKey("meta") || mainDoc["meta"].size() == 0) {
    Serial.println("No valid recovered entries to send.");
    return;
  }

  Serial.println("Recovered entries to send: " + String(mainDoc["meta"].size()));

  saveRecoveredJsonDataToSd(fileList, count, now);

  // Prepare filename for deletion
  char baseFilename[64];
  createFilename(baseFilename, sizeof(baseFilename), now);
  String recoveredFilename = String(baseFilename);
  recoveredFilename.remove(recoveredFilename.length() - 5);  
  recoveredFilename += "_recovered.json";

  char payload[1024];
  size_t len = serializeJson(mainDoc, payload, sizeof(payload));
  if (len >= sizeof(payload)) {
    Serial.println("Payload too large, skipping publish.");
    return;
  }

  char fullTopic[128];
  snprintf(fullTopic, sizeof(fullTopic), "%s%s/%s/recovered", topicPrefix, sensorType, sensorId);
  Serial.print("Publishing recovered data to ");
  Serial.println(fullTopic);

  if (mqttClient.beginMessage(fullTopic)) {
    mqttClient.print(payload);
    mqttClient.endMessage();
    Serial.println("Published recovered data.");

  deleteRecoveredAndPendingSourceFilesData(fileList, count, now, recoveredFilename);
} else {
  Serial.println("MQTT recovered publish failed.");
}
}

