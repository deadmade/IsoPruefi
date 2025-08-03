#include "mqtt.h"
#include "storage.h"

// MQTT and JSON buffer size constants
static const size_t SMALL_BUFFER_SIZE = 128;  // For topics, payloads, and small JSON docs
static const size_t LARGE_BUFFER_SIZE = 1024; // For large payloads and JSON docs
static const size_t BASE_FILENAME_BUFFER_SIZE = 64;
static const int MAX_PENDING_FILES = 500;
static const int FILE_EXTENSION_LENGTH = 5;

void createFullTopic(char* buffer, size_t bufferSize,
                     const char* topicPrefix,
                     const char* sensorType,
                     const char* sensorId,
                     const char* suffix) {
  if (suffix && strlen(suffix) > 0) {
    snprintf(buffer, bufferSize, "%s%s/%s/%s", topicPrefix, sensorType, sensorId, suffix);
  } else {
    snprintf(buffer, bufferSize, "%s%s/%s", topicPrefix, sensorType, sensorId);
  }
}

/**
 * @brief Publishes sensor data to an MQTT topic.
 *
 * Constructs a JSON payload with sensor data and publishes it to an MQTT topic
 * constructed from the given prefix, sensor type, and sensor ID.
 *
 * @param mqttClient   Reference to the MQTT client used for publishing.
 * @param topicPrefix  Prefix for the MQTT topic (e.g., "sensors/").
 * @param sensorType   Type of the sensor (e.g., "temperature").
 * @param sensorId     Unique identifier for the sensor.
 * @param celsius      Sensor reading in Celsius.
 * @param now          Current date and time.
 * @param sequence     Sequence number for the message.
 */
void sendToMqtt(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                const char* sensorId, float celsius, const DateTime& now, int sequence) {
  mqttClient.poll();

  char fullTopic[SMALL_BUFFER_SIZE];
  createFullTopic(fullTopic, sizeof(fullTopic), topicPrefix, sensorType, sensorId);

  StaticJsonDocument<SMALL_BUFFER_SIZE> jsonDoc;
  buildJson(jsonDoc, celsius, now, sequence);

  char payload[SMALL_BUFFER_SIZE];
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

/**
 * @brief Sends pending sensor data that was saved offline to MQTT broker.
 *
 * Retrieves saved sensor data files from the last 24 hours, consolidates them
 * into a single JSON payload, and publishes to a recovery topic. After successful
 * transmission, the original files and recovery file are deleted from storage.
 *
 * @param mqttClient   Reference to the MQTT client used for publishing.
 * @param topicPrefix  Prefix for the MQTT topic (e.g., "sensors/").
 * @param sensorType   Type of the sensor (e.g., "temperature").
 * @param sensorId     Unique identifier for the sensor.
 * @param now          Current date and time for file filtering.
 */
void sendPendingData(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                     const char* sensorId, const DateTime& now) {
  Serial.println("Sending pending data");

  String fileList[MAX_PENDING_FILES];
  int count = listSavedFilesData(fileList, MAX_PENDING_FILES, now);
  Serial.println("Pending files count: " + String(count));

  if (count == 0) return;

  StaticJsonDocument<LARGE_BUFFER_SIZE> mainDoc;
  buildRecoveredJson(mainDoc, fileList, count, now);

  if (!mainDoc.containsKey("meta") || mainDoc["meta"].size() == 0) {
    Serial.println("No valid recovered entries to send.");
    return;
  }

  Serial.println("Recovered entries to send: " + String(mainDoc["meta"].size()));

  saveRecoveredJsonDataToSd(fileList, count, now);

  // Prepare filename for deletion
  char baseFilename[BASE_FILENAME_BUFFER_SIZE];
  createFilename(baseFilename, sizeof(baseFilename), now);
  String recoveredFilename = String(baseFilename);
  recoveredFilename.remove(recoveredFilename.length() - FILE_EXTENSION_LENGTH);  
  recoveredFilename += "_recovered.json";

  char payload[LARGE_BUFFER_SIZE];
  size_t len = serializeJson(mainDoc, payload, sizeof(payload));
  if (len >= sizeof(payload)) {
    Serial.println("Payload too large, skipping publish.");
    return;
  }

  char fullTopic[SMALL_BUFFER_SIZE];
  createFullTopic(fullTopic, sizeof(fullTopic), topicPrefix, sensorType, sensorId, "recovered");
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

