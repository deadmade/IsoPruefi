#include "mqtt.h"
#include "storage.h"

// MQTT and JSON buffer size constants
static const size_t SMALL_BUFFER_SIZE = 128;  // For topics, payloads, and small JSON docs
static const size_t LARGE_BUFFER_SIZE = 2048; // For large payloads and JSON docs
static const size_t FILE_NAME_BUFFER_SIZE = 64;
static const int MAX_RECOVERY_FILES_PER_LOOP = 3;

void createFullTopic(char* buffer, size_t bufferSize, const char* topicPrefix, const char* sensorType,
                     const char* sensorId, const char* suffix) {
  if (suffix && strlen(suffix) > 0) {
    snprintf(buffer, bufferSize, "%s%s/%s/%s", topicPrefix, sensorType, sensorId, suffix);
  } else {
    snprintf(buffer, bufferSize, "%s%s/%s", topicPrefix, sensorType, sensorId);
  }
}

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

bool sendPendingData(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                     const char* sensorId, const DateTime& now) {
  Serial.println("Looking for pending CSV files...");

  const unsigned long startMillis = millis();
  bool allFilesSent = true;

  char folder[8];
  strncpy(folder, createFolderName(now), sizeof(folder)); 
  File root = sd.open(folder);
  if (!root) {
    Serial.println("No folder found for pending data.");
    return true; // kein Ordner = keine Arbeit mehr
  }

  int sentCount = 0;
  int checkedFiles = 0;
  int skippedEmptyFiles = 0;

  File entry;
  while ((entry = root.openNextFile())) {
    if (entry.isDirectory()) continue;

    char filename[64];
    entry.getName(filename, sizeof(filename));
    entry.close();

    String nameStr(filename);
    if (!nameStr.endsWith(".csv")) continue;

    checkedFiles++;

    // Prüfen, ob Datei älter als 24 Stunden ist
    char fullPath[64];
    snprintf(fullPath, sizeof(fullPath), "%s/%s", folder, filename);
    File tsFile = sd.open(fullPath, FILE_READ);
    if (tsFile) {
      char line[64];
      if (tsFile.fgets(line, sizeof(line)) > 0) {
        char* p = strtok(line, ",");
        if (p) {
          uint32_t ts = atol(p);
          if (now.unixtime() - ts > 86400) {
            Serial.print("Skipping old CSV file (>24h): ");
            Serial.println(nameStr);
            tsFile.close();
            continue;
          }
        }
      }
      tsFile.close();
    }

    // Umwandlung in JSON
    StaticJsonDocument<LARGE_BUFFER_SIZE> doc;
    buildRecoveredJsonFromCsv(doc, fullPath, now);

    if (!doc.containsKey("meta") || doc["meta"].size() == 0) {
      Serial.println("No valid data in: " + nameStr);
      skippedEmptyFiles++;
      continue;
    }

    char payload[LARGE_BUFFER_SIZE];
    size_t len = serializeJson(doc, payload, sizeof(payload));
    if (len >= sizeof(payload)) {
      Serial.println("Payload too large, skipping file: " + nameStr);
      allFilesSent = false;
      continue;
    }

    char fullTopic[SMALL_BUFFER_SIZE];
    createFullTopic(fullTopic, sizeof(fullTopic), topicPrefix, sensorType, sensorId, "recovered");

    Serial.print("Publishing recovered CSV: ");
    Serial.println(nameStr);
    Serial.print("MQTT payload: ");
    Serial.println(payload);

    if (mqttClient.beginMessage(fullTopic)) {
      mqttClient.print(payload);
      mqttClient.endMessage();
      Serial.println("Published and deleting file.");
      deleteCsvFile(fullPath); // aktivieren wenn Test erfolgreich
      sentCount++;
    } else {
      Serial.println("Failed to publish. Keeping file: " + nameStr);
      allFilesSent = false;
    }

    // Abbruchbedingung: Zeitlimit überschritten?
    if (millis() - startMillis > 60000) {
      Serial.println("Aborting recovery: 60s time limit exceeded.");
      allFilesSent = false;
      break;
    }
  }

  root.close();

  if (checkedFiles == 0) {
    Serial.println("No CSV recovery files found.");
  } else if (sentCount == 0 && skippedEmptyFiles == checkedFiles) {
    Serial.println("All found recovery files were empty, too old, or invalid.");
  } else {
    Serial.print("Recovered files sent this loop: ");
    Serial.println(sentCount);
  }

  return allFilesSent;
}


