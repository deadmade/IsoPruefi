#include "mqtt.h"
#include "storage.h"

// Creates JSON document with timestamp, temperature and sequence number
void buildJson(JsonDocument& doc, float celsius, const DateTime& now, int sequence) {
  doc.clear();
  doc["timestamp"] = now.unixtime();
  JsonArray arr = doc["value"].to<JsonArray>();
  arr.add(celsius);
  doc["sequence"] = sequence;
}

// Sends a JSON payload to the complete MQTT topic
void sendToMqtt(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                const char* sensorId, float celsius, const DateTime& now, int sequence) {
  mqttClient.poll();

  char fullTopic[128];
  snprintf(fullTopic, sizeof(fullTopic), "%s%s/%s", topicPrefix, sensorType, sensorId);

  ArduinoJson::StaticJsonDocument<128> jsonDoc;
  buildJson(jsonDoc, celsius, now, sequence);

  char payload[128];
  serializeJson(jsonDoc, payload);

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
  Serial.println("Sending pending data...");
  String fileList[1500];
  int count = listSavedFiles(fileList, 1500);

  if (count == 0) {
    Serial.println("No pending data to send.");
    return;
  }
  Serial.print("Found ");

  int startIdx = count > 1440 ? count - 1440 : 0;
  int sendCount = count - startIdx;

  ArduinoJson::DynamicJsonDocument root(4096);
  JsonArray dataArray = root.createNestedArray("data");
  JsonObject meta = root.createNestedObject("meta");

  int successCount = 0;
  uint32_t latestTimestamp = 0;

  for (int i = startIdx; i < count; ++i) {
    Serial.print("Processing file: ");
    File file = sd.open(fileList[i].c_str(), FILE_READ);
    if (!file) continue;

    StaticJsonDocument<128> entry;
    DeserializationError err = deserializeJson(entry, file);
    file.close();

    if (err) continue;

    dataArray.add(entry);
    uint32_t ts = entry["timestamp"] | 0;
    if (ts > latestTimestamp) latestTimestamp = ts;

    successCount++;
    Serial.print("File ");
    Serial.print(fileList[i]);
  }

  meta["count"] = successCount;
  meta["latest"] = latestTimestamp;

  char payload[4096];
  size_t len = serializeJson(root, payload, sizeof(payload));

  Serial.print("Total entries to send: ");
  if (len >= sizeof(payload)) {
    Serial.println("Payload too large, skipping publish.");
    return;
  }

  char fullTopic[128];
  snprintf(fullTopic, sizeof(fullTopic), "%s%s/%s/recovered", topicPrefix, sensorType, sensorId);
  Serial.print("Publishing recovered data to ");
  if (mqttClient.beginMessage(fullTopic)) {
    mqttClient.print(payload);
    mqttClient.endMessage();
    Serial.print("Published recovered data to ");
    Serial.println(fullTopic);
    Serial.print("Entries sent: ");
    Serial.println(successCount);
  } else {
    Serial.println("MQTT recovered publish failed.");
  }
}