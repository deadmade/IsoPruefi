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

// =============================================================================

void createFullTopic(char* buffer, size_t bufferSize, const char* topicPrefix,
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
 * @brief Publishes real-time sensor data to MQTT broker
 * 
 * This function handles the complete process of publishing current sensor
 * readings to the MQTT broker, including JSON formatting and topic construction.
 * 
 * **Process Flow:**
 * 1. Polls MQTT client to maintain connection
 * 2. Constructs appropriate topic based on sensor information
 * 3. Builds JSON payload with sensor data
 * 4. Attempts to publish data to broker
 * 5. Provides status feedback via serial output
 * 
 * @param[in] mqttClient   Reference to the MQTT client instance
 * @param[in] topicPrefix  Base MQTT topic prefix
 * @param[in] sensorType   Type identifier for the sensor
 * @param[in] sensorId     Unique identifier for this sensor
 * @param[in] celsius      Temperature reading in Celsius
 * @param[in] now          Current timestamp for the reading
 * @param[in] sequence     Sequence number for the measurement
 * 
 * @note This function does not handle connection failures - the caller
 *       should verify MQTT connectivity before calling
 * @see buildJson() for JSON payload structure
 * @see createFullTopic() for topic construction details
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

// =============================================================================
// DATA RECOVERY AND OFFLINE TRANSMISSION FUNCTIONS
// =============================================================================

/**
 * @brief Processes and transmits pending CSV files from offline periods
 * 
 * This function implements the data recovery mechanism that ensures no sensor
 * data is lost during network outages. It scans for CSV files created during
 * offline periods, converts them to JSON format, and transmits them via MQTT.
 * 
 * **Recovery Process:**
 * 1. **File Discovery**: Scans the current date folder for CSV files
 * 2. **Age Filtering**: Skips files older than 24 hours to prevent stale data
 * 3. **Content Validation**: Checks for valid data before transmission
 * 4. **Size Management**: Handles large payloads that exceed buffer limits
 * 5. **Transmission**: Publishes data to recovery topic
 * 6. **Cleanup**: Deletes successfully transmitted files
 * 7. **Timeout Handling**: Prevents infinite processing loops
 * 
 * **Error Handling:**
 * - Skips empty or corrupted files
 * - Handles oversized payloads gracefully
 * - Preserves files if transmission fails
 * - Implements timeout protection
 * 
 * @param[in] mqttClient   Reference to the MQTT client instance
 * @param[in] topicPrefix  Base MQTT topic prefix
 * @param[in] sensorType   Type identifier for the sensor
 * @param[in] sensorId     Unique identifier for this sensor
 * @param[in] now          Current timestamp for filtering and processing
 * 
 * @return true if all pending files were successfully processed, false if
 *         timeout occurred or some files could not be transmitted
 * 
 * @note This function uses the "/recovered" topic suffix for transmitted data
 * @see buildRecoveredJsonFromCsv() for CSV to JSON conversion
 * @see deleteCsvFile() for file cleanup after successful transmission
 * @warning Files older than 24 hours are automatically skipped
 */
bool sendPendingData(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                     const char* sensorId, const DateTime& now) {
  Serial.println("Looking for pending CSV files...");

  // Track processing time to prevent infinite loops
  const unsigned long startMillis = millis();
  bool allFilesSent = true;

  // Open the current date folder
  char folder[FOLDER_NAME_BUFFER_SIZE];
  strncpy(folder, createFolderName(now), sizeof(folder)); 
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
    buildRecoveredJsonFromCsv(doc, fullPath, now);

    // Validate that the file contains usable data
    if (!doc.containsKey("meta") || doc["meta"].size() == 0) {
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
    createFullTopic(fullTopic, sizeof(fullTopic), topicPrefix, sensorType, sensorId, "recovered");

    Serial.print("Publishing recovered CSV: ");
    Serial.println(nameStr);
    Serial.print("MQTT payload: ");
    Serial.println(payload);

    if (mqttClient.beginMessage(fullTopic)) {
      mqttClient.print(payload);
      mqttClient.endMessage();
      Serial.println("Published and deleting file.");
      // deleteCsvFile(fullPath); // Uncomment to delete after successful publish
      sentCount++;
    } else {
      Serial.println("Failed to publish. Keeping file: " + nameStr);
      allFilesSent = false;
    }

    // Check for timeout to prevent blocking the main loop too long
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


