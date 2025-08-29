#include "storage.h"

// =============================================================================
// CSV PROCESSING CONSTANTS
// =============================================================================

/// Buffer size for folder names (e.g., "2025")
static const size_t FOLDER_NAME_BUFFER_SIZE = 8;
/// Buffer size for storing the current active CSV filename
static const size_t CURRENT_FILENAME_BUFFER_SIZE = 32;
/// Buffer size for reading individual CSV lines
static const size_t CSV_LINE_BUFFER_SIZE = 64;
/// Maximum number of sensor readings per CSV batch file
static const int MAX_LINES_PER_CSV_FILE = 5;
static char currentFilename[CURRENT_FILENAME_BUFFER_SIZE] = "";
/// Static variable to track the number of lines in the current CSV file
static int linesInFile = 0;

// =============================================================================
// CSV BATCH STORAGE FUNCTIONS
// =============================================================================

/**
 * @brief Saves sensor data to CSV files in batch mode during network outages
 * 
 * This function implements intelligent batch CSV storage that creates new files
 * when needed and manages file rotation. It's designed as a fallback mechanism
 * when MQTT transmission is unavailable due to network connectivity issues.
 * 
 * **Batch Management:**
 * - Maintains a static filename for the current active CSV file
 * - Creates new files when the current file reaches maximum line limit
 * - Uses timestamp-based filenames for uniqueness and organization
 * 
 * **File Organization:**
 * - Creates date-based folders automatically (e.g., "2025/")
 * - Stores files with minute-precision timestamps in filename
 * - Format: "YYYY/MMDDHHMM.csv" (e.g., "2025/08051430.csv")
 * 
 * **Data Format:**
 * - CSV format: timestamp,temperature,sequence
 * - Temperature precision: 5 decimal places
 * - Unix timestamp for absolute time reference
 * 
 * @param[in] now      Current timestamp for folder creation and data logging
 * @param[in] celsius  Temperature reading in Celsius (stored with 5 decimal precision)
 * @param[in] sequence Sequence number for the measurement
 * 
 * @note This function uses static variables to maintain state across calls
 * @note Files are automatically rotated after MAX_LINES_PER_CSV_FILE entries
 * @see createFolderName() for folder naming convention
 * @see createFilename() for CSV filename generation
 */
void SaveTempToBatchCsv(const DateTime& now, float celsius, int sequence) {
  char folder[FOLDER_NAME_BUFFER_SIZE];
  strncpy(folder, CreateFolderName(now), sizeof(folder));

  if (!sd.exists(folder)) {
    sd.mkdir(folder);
  }

  // Create new file if needed
  if (strlen(currentFilename) == 0 || linesInFile >= MAX_LINES_PER_CSV_FILE) {
    CreateCsvFilename(currentFilename, sizeof(currentFilename), now);
    linesInFile = 0;
  }

  // Write the sensor data to the current CSV file
  File file = sd.open(currentFilename, FILE_WRITE);
  if (file) {
    char line[CSV_LINE_BUFFER_SIZE];
    snprintf(line, sizeof(line), "%lu,%.5f,%d\n", now.unixtime(), celsius, sequence);
    file.print(line);
    file.close();
    linesInFile++;
    Serial.print("Saved CSV fallback: ");
    Serial.println(currentFilename);
  } else {
    Serial.println("Failed to write CSV fallback.");
  }
}

// =============================================================================
// JSON DOCUMENT CREATION FUNCTIONS
// =============================================================================

/**
 * @brief Builds a JSON document from live sensor data for real-time transmission
 * 
 * This function creates a properly formatted JSON document containing current
 * sensor readings for immediate MQTT transmission. The JSON structure follows
 * a standardized format for IoT temperature monitoring systems with metadata support.
 * 
 * **JSON Structure:**
 * ```json
 * {
 *   "timestamp": 1737024000,
 *   "value": [25.12345],
 *   "sequence": 42,
 *   "meta": {}
 * }
 * ```
 * 
 * **Data Precision:**
 * - Temperature: 5 decimal places for high precision monitoring
 * - Timestamp: Unix timestamp (seconds since epoch)
 * - Sequence: Integer measurement counter
 * - Value array: Supports multiple sensor readings
 * 
 * **Performance Considerations:**
 * - Modifies existing JsonDocument for memory efficiency
 * - Clears previous data to prevent accumulation
 * - Optimized for embedded systems with limited memory
 * 
 * @param[out] doc     JsonDocument reference to populate (cleared before use)
 * @param[in]  celsius Temperature reading in Celsius (stored with 5 decimal precision)
 * @param[in]  now     Current timestamp for the measurement
 * @param[in]  sequence Sequence number for the measurement
 * 
 * @note The function clears the document before populating new data
 * @see buildRecoveredJsonFromCsv() for batch recovery JSON format
 * @see saveToCsvBatch() for CSV fallback storage format
 */
void BuildJson(JsonDocument& doc, float celsius, const DateTime& now, int sequence) {
  doc.clear();
  doc["timestamp"] = now.unixtime();
  JsonArray val = doc["value"].to<JsonArray>();
  val.add(celsius);
  doc["sequence"] = sequence;
  JsonObject meta = doc["meta"].to<JsonObject>();
}

/**
 * @brief Builds a JSON document from CSV batch data for recovery transmission
 * 
 * This function processes stored CSV data files and converts them into
 * JSON format for batch transmission when network connectivity is restored.
 * It handles multiple measurements in a metadata array structure for efficient
 * network utilization during recovery operations.
 * 
 * **Recovery JSON Structure:**
 * ```json
 * {
 *   "timestamp": 1737024000,
 *   "sequence": null,
 *   "value": [null],
 *   "meta": 
 *     {
 *        "t": [1737024000, 1737024060, ..], 
 *        "v": [25.12345, 25.12567, ...], 
 *        "s": [42, 43, ...]
 *      }
 * }
 * ```
 * 
 * **Processing Logic:**
 * - Reads each line from the specified CSV file using secure fgets()
 * - Parses CSV format: timestamp,temperature,sequence using strtok()
 * - Creates individual JSON objects for each measurement in meta array
 * - Uses null placeholders for top-level value and sequence fields
 * 
 * **Error Handling:**
 * - Returns early if file cannot be opened
 * - Skips malformed lines during CSV parsing
 * - Continues processing even if some lines fail
 * - Reports total number of successfully recovered entries
 * 
 * @param[out] doc      JsonDocument reference to populate (cleared before use)
 * @param[in]  filepath Path to the CSV file containing batch sensor data
 * @param[in]  now      Current timestamp for the recovery operation
 * 
 * @note Uses strtok() for safe CSV parsing with buffer protection
 * @note Clears the document before populating new batch data
 * @see saveToCsvBatch() for CSV storage format details
 * @see sendPendingData() in mqtt.cpp for recovery transmission
 */
void BuildRecoveryJsonFromBatchCsv(JsonDocument& doc, const char* filepath, const DateTime& now) {
  File file = sd.open(filepath, FILE_READ);
  if (!file) {
    Serial.print("CSV not found: ");
    Serial.println(filepath);
    return;
  }

  doc.clear();
  doc["timestamp"] = now.unixtime();
  doc["sequence"] = nullptr;
  JsonArray val = doc["value"].to<JsonArray>();
  val.add(nullptr);  // Dummy value for compatibility

  JsonObject meta = doc["meta"].to<JsonObject>();
  JsonArray tArr = meta["t"].to<JsonArray>();  // timestamp
  JsonArray vArr = meta["v"].to<JsonArray>();  // value
  JsonArray sArr = meta["s"].to<JsonArray>();  // sequence

  char line[CSV_LINE_BUFFER_SIZE];
  int added = 0;

  // Process each line of the CSV file safely
  while (file.available()) {
    size_t len = file.fgets(line, sizeof(line));
    if (len == 0) continue;

     // Parse CSV format: timestamp,temperature,sequence
     char* p = strtok(line, ",");
     if (!p) {
       Serial.print("Malformed CSV line (no timestamp): ");
       Serial.println(line);
       continue;
     }
     uint32_t ts = atol(p);

     p = strtok(nullptr, ",");
     if (!p) {
       Serial.print("Malformed CSV line (no temperature): ");
       Serial.println(line);
       continue;
     }
     float temp = atof(p);

     p = strtok(nullptr, ",");
     if (!p) {
       Serial.print("Malformed CSV line (no sequence): ");
       Serial.println(line);
       continue;
     }
     int seq = atoi(p);

     tArr.add(ts);
     vArr.add(temp);
     sArr.add(seq);

     added++;
   }

   file.close();

  // Report recovery statistics
   Serial.print("Recovered entries added from CSV: ");
   Serial.print(String(added));
   Serial.print(" (");
   Serial.print(filepath);
   Serial.println(")");
}

// =============================================================================
// FILE MANAGEMENT FUNCTIONS
// =============================================================================

/**
 * @brief Deletes a CSV file from the SD card storage system
 * 
 * This function provides safe file deletion with error handling and logging. 
 * It's primarily used during the data recovery process to clean up CSV files 
 * after they have been successfully transmitted via MQTT.
 * 
 * **Safety Features:**
 * - Checks file existence before attempting deletion
 * - Provides detailed success/failure logging
 * - Handles SD card filesystem errors gracefully
 * 
 * **Use Cases:**
 * - Cleanup after successful batch data transmission
 * - Manual file management during storage maintenance
 * - Recovery process completion in data transmission pipeline
 * 
 * **Error Scenarios:**
 * - File does not exist (silently ignored)
 * - SD card write protection or filesystem errors
 * - Insufficient permissions or corrupted filesystem
 * 
 * @param[in] filepath Complete path to the CSV file to be deleted
 * 
 * @note Function silently ignores attempts to delete non-existent files
 * @note All operations are logged to Serial for debugging and monitoring
 * @see buildRecoveredJsonFromCsv() for file processing before deletion
 * @see sendPendingData() in mqtt.cpp for recovery workflow integration
 */
void DeleteCsvFile(const char* filepath) {
  if (sd.exists(filepath)) {
    if (sd.remove(filepath)) {
      Serial.print("Deleted CSV file: ");
      Serial.println(filepath);
      if (strcmp(filepath, currentFilename) == 0) {
        currentFilename[0] = '\0';
        linesInFile = 0;
        Serial.println("Reset currentFilename after deletion.");
      }
    } else {
      Serial.print("Failed to delete CSV file: ");
      Serial.println(filepath);
    }
  }
}