#include "mqtt.h"
#include "storage.h"

// =============================================================================
// BUFFER SIZE CONSTANTS
// =============================================================================

/// Buffer size for small MQTT topics, payloads, and JSON documents
static const size_t SMALL_BUFFER_SIZE = 128;
/// Buffer size for large MQTT payloads and JSON documents (recovery data)
static const size_t LARGE_BUFFER_SIZE = 2048;
static const size_t FILE_NAME_BUFFER_SIZE = 64;
static const int MAX_RECOVERY_FILES_PER_LOOP = 3;

// =============================================================================
// FILE SYSTEM AND TIMING CONSTANTS
// =============================================================================

static const size_t FOLDER_NAME_BUFFER_SIZE = 8;
static const size_t FULL_PATH_BUFFER_SIZE = 64;
/// Buffer size for reading individual CSV lines
static const size_t LINE_BUFFER_SIZE = 64;
static const uint32_t SECONDS_IN_24_HOURS = 86400;
/// Timeout for recovery operations in milliseconds (60 seconds)
static const unsigned long RECOVERY_TIMEOUT_MS = 60000;
static const unsigned long ACK_TIMEOUT_MS = 5000;
static const unsigned long RECOVERY_ACK_TIMEOUT_MS  = 10000;
static const unsigned long DELAY_POLLING_LOOP_MS = 10;

// =============================================================================
// ACK/ECHO-HANDLING 
// =============================================================================

/**
 * @defgroup AckEchoHandling MQTT ACK/Echo Handling
 * @brief Implements publish acknowledgment and echo logic for reliable MQTT delivery.
 *
 * This section manages the detection and processing of MQTT PUBACK or echo messages from the broker.
 * It tracks acknowledgment state, extracts sequence numbers from echoed JSON payloads, and ensures
 * that published messages are confirmed before considering them delivered. If no acknowledgment is
 * received, data is saved for later recovery.
 *
 * - Uses MQTT_TOPIC-based filtering to only process echoes for the current publish MQTT_TOPIC
 * - Handles retained messages and ignores them for acknowledgment
 * - Extracts sequence numbers from JSON payloads for matching
 * - Registers a callback for incoming MQTT messages to detect PUBACK/echo
 * - Re-subscribes to the publish MQTT_TOPIC after each reconnect
 *
 * @note This logic is critical for QoS 1 delivery and robust offline recovery.
 */

static volatile bool  s_ackSeen   = false;
static volatile long  s_ackSeq    = -1;
static String         s_pubTopic;           // z. B. "<prefix>temp/Sensor_Two"
static bool           s_ackInit   = false;

/**
 * @brief Extracts the sequence number from a JSON string.
 *
 * Searches for the "sequence" field in the provided JSON and parses its value.
 * Returns false if the field is missing or set to null.
 *
 * @param json The JSON string to search
 * @param outSeq Reference to store the extracted sequence number
 * @return true if a valid sequence number was found, false otherwise
 */
static bool ExtractSequence(const char* json, long& outSeq) {
  const char* p = strstr(json, "\"sequence\":");
  if (!p) return false;
  p += 11; // length of "\"sequence\":"
  // Skip whitespace
  while (*p == ' ' || *p == '\t') ++p;
  // Support null (Recovery may have sequence:null)
  if (strncmp(p, "null", 4) == 0) return false;
  outSeq = strtol(p, nullptr, 10);
  return true;
}

/**
 * @brief MQTT message callback to detect PUBACK/echo for published messages.
 *
 * Processes incoming MQTT messages, filtering by MQTT_TOPIC and retain flag. If the message
 * is an echo for the current publish MQTT_TOPIC and not retained, extracts the sequence number
 * and sets acknowledgment flags.
 *
 * @param messageSize Size of the incoming message (needed because of the MQTT library's callback interface)
 */
static void OnMqttEchoMessage(int messageSize) {
  (void)messageSize;
  if (mqttClient.messageTopic() != s_pubTopic) return;
  if (mqttClient.messageRetain()) return;

  static char buf[SMALL_BUFFER_SIZE * 2];
  int n = 0;
  while (mqttClient.available() && n < (int)sizeof(buf) - 1) {
    buf[n++] = mqttClient.read();
  }
  buf[n] = 0;

  long seq;
  if (ExtractSequence(buf, seq)) {
    s_ackSeq  = seq;
    s_ackSeen = true;
  }
}

/**
 * @brief Initializes ACK/Echo handling and subscribes to the publish MQTT_TOPIC.
 *
 * Sets up the publish MQTT_TOPIC and registers the MQTT message callback for echo detection.
 * Ensures the callback is registered only once, and re-subscribes to the MQTT_TOPIC after each reconnect.
 *
 * @param client Reference to the MQTT client
 * @param topicPrefix Topic prefix for MQTT publishing
 * @param sensorType Sensor type string
 * @param sensorId Unique sensor identifier
 */
static void EnsureAckInit(MqttClient& client, const char* topicPrefix, const char* sensorType, const char* sensorId) {
  if (!s_ackInit) {
    char fullTopic[SMALL_BUFFER_SIZE];
    if (sensorType && sensorId) {
      snprintf(fullTopic, sizeof(fullTopic), "%s%s/%s", topicPrefix, sensorType, sensorId);
      s_pubTopic = fullTopic;
      s_ackInit  = true;
    } else {
      return; 
    }
    client.onMessage(OnMqttEchoMessage); // Callback register
  }

  if (client.connected()) {
    client.subscribe(s_pubTopic.c_str());
  }
}

// =============================================================================

void CreateFullTopic(char* buffer, size_t bufferSize, const char* topicPrefix,
                     const char* sensorType, const char* sensorId, const char* suffix) {
  if (suffix && strlen(suffix) > 0) {
    snprintf(buffer, bufferSize, "%s%s/%s/%s", topicPrefix, sensorType, sensorId, suffix);
  } else {
    snprintf(buffer, bufferSize, "%s%s/%s", topicPrefix, sensorType, sensorId);
  }
}

// =============================================================================
// REAL-TIME DATA TRANSMISSION FUNCTIONS
// =============================================================================

/**
 * @brief Publishes real-time sensor data to the MQTT broker with QoS 1 delivery.
 *
 * This function builds a JSON payload from the provided sensor data and publishes it
 * to the specified MQTT topic. After publishing, it waits briefly for a PUBACK
 * handshake from the broker to confirm delivery. If no acknowledgment is received within
 * the timeout window, the data is saved to a CSV file for later recovery.
 *
 * @param mqttClient Reference to the MQTT client instance
 * @param topicPrefix Topic prefix for MQTT publishing (e.g., "dhbw/ai/si2023/2/")
 * @param sensorType Sensor type string (e.g., "temp")
 * @param sensorId Unique sensor identifier
 * @param celsius Measured temperature value in Celsius
 * @param now Current timestamp (DateTime)
 * @param sequence Sequence number for the measurement
 * @return true if published and acknowledged by broker, false if fallback to CSV
 *
 * @note Uses QoS 1 for reliable delivery. If broker does not echo/PUBACK within
 *       the timeout, data is persisted for later transmission.
 */
bool SendTempToMqtt(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                const char* sensorId, float celsius, const DateTime& now, int sequence) {
  EnsureAckInit(mqttClient, topicPrefix, sensorType, sensorId);

  mqttClient.poll();

  char fullTopic[SMALL_BUFFER_SIZE];
  CreateFullTopic(fullTopic, sizeof(fullTopic), topicPrefix, sensorType, sensorId);

  StaticJsonDocument<SMALL_BUFFER_SIZE> jsonDoc;
  BuildJson(jsonDoc, celsius, now, sequence);

  char payload[SMALL_BUFFER_SIZE];
  serializeJson(jsonDoc, payload, sizeof(payload));

  // Reset ACK-Flags
  s_ackSeen = false;
  s_ackSeq  = -1;


  if (mqttClient.beginMessage(fullTopic, false, 1)) {
    mqttClient.print(payload);
    if (!mqttClient.endMessage()) {
      Serial.println("MQTT endMessage() failed → saving to CSV.");
      SaveTempToBatchCsv(now, celsius, sequence);
      return false;
    }

    // delay for a short window to allow poll() to process the PUBACK/echo
    unsigned long startTime = millis();
    bool ackOk = false;
    while (millis() - startTime < ACK_TIMEOUT_MS) {
      mqttClient.poll();
      if (s_ackSeen && s_ackSeq == sequence) {
        ackOk = true;
        break;
      }
      delay(DELAY_POLLING_LOOP_MS);
    }

    if (!ackOk) {
      Serial.println("No Echo/PUBACK within timeout → saving to CSV.");
      SaveTempToBatchCsv(now, celsius, sequence);
      return false;
    }

    Serial.print("Published to ");
    Serial.println(fullTopic);
    Serial.println(payload);
    return true;
  } else {
    Serial.println("MQTT beginMessage() failed → saving to CSV.");
    SaveTempToBatchCsv(now, celsius, sequence);
    return false;
  }
}

// =============================================================================
// DATA RECOVERY AND OFFLINE TRANSMISSION FUNCTIONS
// =============================================================================

/**
 * @brief Processes and transmits pending CSV files from offline periods to the MQTT broker.
 *
 * This function scans the SD card for CSV files containing unsent sensor data from previous offline periods.
 * Each file is converted to a JSON payload and published to the MQTT topic <topic>/recovered with QoS 1.
 * After publishing, it waits briefly for a PUBACK handshake from the broker to confirm delivery.
 * If the PUBACK is not received within the timeout period, the file is saved for later transmission.
 * Files are only deleted if the publish operation succeeds. Files older than 24 hours or with invalid data are skipped.
 *
 * @param mqttClient Reference to the MQTT client instance
 * @param topicPrefix Topic prefix for MQTT publishing (e.g., "dhbw/ai/si2023/2/")
 * @param sensorType Sensor type string (e.g., "temp")
 * @param sensorId Unique sensor identifier
 * @param now Current timestamp (DateTime)
 * @return true if all valid files were published and deleted, false if any files remain or errors occurred
 *
 * @note Uses QoS 1 for reliable delivery. Skips files older than 24 hours or with invalid content. Aborts if recovery exceeds time limit.
 */
bool SendPendingDataToMqtt(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                     const char* sensorId, const DateTime& now) {
  Serial.println("Looking for pending CSV files...");

  // Track processing time to prevent infinite loops
  const unsigned long startMillis = millis();
  bool allFilesSent = true;

  // Open the current date folder
  char folder[FOLDER_NAME_BUFFER_SIZE];
  strncpy(folder, CreateFolderName(now), sizeof(folder));
  File root = sd.open(folder);
  if (!root) {
    Serial.println("No folder found for pending data.");
    return true;
  }

  // Initialize processing counters
  int sentCount = 0;
  int checkedFiles = 0;
  int skippedEmptyFiles = 0;

  File entry;
  while ((entry = root.openNextFile())) {
    if (entry.isDirectory()) continue;

    char filename[FILE_NAME_BUFFER_SIZE];
    entry.getName(filename, sizeof(filename));
    entry.close();

    String nameStr(filename);
    if (!nameStr.endsWith(".csv")) continue;

    checkedFiles++;

    // Validate file age (skip files older than 24 hours)
    char fullPath[FULL_PATH_BUFFER_SIZE];
    snprintf(fullPath, sizeof(fullPath), "%s/%s", folder, filename);
    File tsFile = sd.open(fullPath, FILE_READ);
    if (tsFile) {
      char line[LINE_BUFFER_SIZE];
      if (tsFile.fgets(line, sizeof(line)) > 0) {
        char* p = strtok(line, ",");
        if (p) {
          Serial.print("Malformed CSV line (no timestamp): ");
          Serial.println(line);
          uint32_t ts = atol(p);
          if (now.unixtime() - ts > SECONDS_IN_24_HOURS) {
            Serial.print("Skipping old CSV file (>24h): ");
            Serial.println(nameStr);
            tsFile.close();
            continue;
          }
        }
      }
      tsFile.close();
    }

    // Convert CSV content to JSON format
    StaticJsonDocument<LARGE_BUFFER_SIZE> doc;
    BuildRecoveryJsonFromBatchCsv(doc, fullPath, now);

    // Validate that the file contains usable data
    if (!doc["meta"].is<JsonObject>() || doc["meta"].size() == 0) {
      Serial.println("No valid data in: " + nameStr);
      skippedEmptyFiles++;
      continue;
    }

    // Serialize JSON and check payload size
    char payload[LARGE_BUFFER_SIZE];
    size_t len = serializeJson(doc, payload, sizeof(payload));
    if (len >= sizeof(payload)) {
      Serial.println("Payload too large, skipping file: " + nameStr);
      allFilesSent = false;
      continue;
    }

    char fullTopic[SMALL_BUFFER_SIZE];
    CreateFullTopic(fullTopic, sizeof(fullTopic), topicPrefix, sensorType, sensorId, "recovered");

    Serial.print("Publishing recovered CSV: ");
    Serial.println(nameStr);
    Serial.print("MQTT payload: ");
    Serial.println(payload);

    bool published = false;
    if (mqttClient.beginMessage(fullTopic, false, 1)) {
      mqttClient.print(payload);
      if (mqttClient.endMessage()) {
        // wait for echo/PUBACK handshake
        unsigned long startTime = millis();
        while (millis() - startTime < RECOVERY_ACK_TIMEOUT_MS) {
          mqttClient.poll();
          delay(DELAY_POLLING_LOOP_MS);
        }
        published = true;
      }
    }

    if (published) {
      Serial.println("Published and deleting file.");
      DeleteCsvFile(fullPath);
      sentCount++;
    } else {
      Serial.println("Failed to publish. Keeping file: " + nameStr);
      allFilesSent = false;
    }

    // Check for overall timeout to prevent blocking too long
    if (millis() - startMillis > RECOVERY_TIMEOUT_MS) {
      Serial.println("Aborting recovery: 60s time limit exceeded.");
      allFilesSent = false;
      break;
    }
  }

  root.close();

  // Provide summary of recovery operation
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
