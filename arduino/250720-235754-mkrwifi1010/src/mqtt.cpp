#include "mqtt.h"
#include "storage.h"

// Creates JSON document with timestamp, temperature and sequence number
void buildJson(JsonDocument& doc, float celsius, const DateTime& now, int sequence) {
  doc.clear();
  doc["timestamp"] = now.unixtime();
  JsonArray arr = doc["value"].to<JsonArray>();
  arr.add(celsius);
  doc["sequence"] = sequence;
  JsonArray meta = doc.createNestedArray("meta");
  meta.add(nullptr);
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

// Baut ein Recovered-JSON mit aktuellem timestamp, null-Werten und meta-Array
void buildRecoveredJson(JsonDocument& doc, String* fileList, int count, const DateTime& now) {
  doc.clear();
  doc["timestamp"] = now.unixtime();
  JsonArray val = doc.createNestedArray("value");
  val.add(nullptr);
  doc["sequence"] = nullptr;

  JsonArray meta = doc.createNestedArray("meta");

  for (int i = 0; i < count; ++i) {
    File file = sd.open(fileList[i].c_str(), FILE_READ);
    if (!file) continue;

    StaticJsonDocument<256> entry;
    DeserializationError err = deserializeJson(entry, file);
    file.close();

    if (!err) {
      meta.add(entry);
    }
  }
}

void sendPendingData(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                     const char* sensorId, const DateTime& now) {
  Serial.println("Sending pending data");

  String fileList[500];
  int count = listSavedFiles(fileList, 500, now);
  Serial.println("Pending files count: " + String(count));

  if (count == 0) {
    Serial.println("No pending data to send.");
    return;
  }

  StaticJsonDocument<1024> mainDoc;
  buildRecoveredJson(mainDoc, fileList, count, now);

  if (!mainDoc.containsKey("meta") || mainDoc["meta"].size() == 0) {
    Serial.println("No valid recovered entries to send.");
    return;
  }

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

    // Nach erfolgreichem Senden: Dateien löschen
    char folder[8];
    strncpy(folder, createFolderName(now), sizeof(folder));
    for (int i = 0; i < count; ++i) {
      String fullPath = String(folder) + "/" + fileList[i];
      sd.remove(fullPath.c_str());
    }
  } else {
    Serial.println("MQTT recovered publish failed.");
  }
}

