#include "mqtt.h"

// Erzeugt JSON-Dokument mit Timestamp, Temperatur und Sequenznummer
void buildJson(JsonDocument& doc, float celsius, const DateTime& now, int sequence) {
  doc.clear();
  doc["timestamp"] = now.unixtime();
  JsonArray arr = doc["value"].to<JsonArray>();
  arr.add(celsius);
  doc["sequence"] = sequence;
}

// Sendet ein JSON-Payload an den vollständigen MQTT-Topic
void sendToMqtt(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                const char* sensorId, float celsius, const DateTime& now, int sequence) {
  mqttClient.poll();

  // Topic zusammensetzen: z. B. dhbw/ai/si2023/2/temp/Sensor_One
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
