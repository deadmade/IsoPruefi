#include "storage.h"

// Storage and JSON buffer size constants
static const size_t SMALL_BUFFER_SIZE = 32;        // For filenames and base names
static const size_t MEDIUM_BUFFER_SIZE = 128;      // For small JSON docs
static const size_t LARGE_BUFFER_SIZE = 256;       // For JSON entries and buffers
static const size_t XLARGE_BUFFER_SIZE = 1024;     // For large JSON docs and buffers
static const size_t FOLDER_NAME_BUFFER_SIZE = 8;
static const size_t RECOVERED_NAME_BUFFER_SIZE = 40;
static const size_t FILE_NAME_BUFFER_SIZE = 64;
static const int FILE_EXTENSION_LENGTH = 5;
static const uint32_t SECONDS_IN_24_HOURS = 86400;

/**
 * @brief Builds a JSON document for sensor data.
 *
 * Creates a standardized JSON structure containing sensor reading, timestamp,
 * sequence number, and metadata fields.
 *
 * @param doc JSON document to populate with sensor data.
 * @param celsius Temperature reading in Celsius.
 * @param now Current date and time for timestamp.
 * @param sequence Sequence number for the reading.
 */
void buildJson(JsonDocument& doc, float celsius, const DateTime& now, int sequence) {
  doc.clear();
  doc["timestamp"] = now.unixtime();
  JsonArray arr = doc.createNestedArray("value");
  arr.add(celsius);
  doc["sequence"] = sequence;
  doc.createNestedArray("meta").add(nullptr);
}

/**
 * @brief Builds a JSON document containing recovered sensor data from multiple files.
 *
 * Reads multiple JSON files from storage, parses them, and consolidates their
 * contents into a single JSON document with metadata array containing all
 * recovered entries.
 *
 * @param doc JSON document to populate with recovered data.
 * @param fileList Array of file paths to read and consolidate.
 * @param count Number of files in the fileList array.
 * @param now Current date and time for the recovery timestamp.
 */
void buildRecoveredJson(JsonDocument& doc, String* fileList, int count, const DateTime& now) {
  doc.clear();
  doc["timestamp"] = now.unixtime();
  doc["sequence"] = nullptr;
  JsonArray val = doc.createNestedArray("value");
  val.add(nullptr);

  JsonArray meta = doc.createNestedArray("meta");

  for (int i = 0; i < count; ++i) {
    Serial.print("Trying to open: ");
    Serial.println(fileList[i]);

    File file = sd.open(fileList[i].c_str(), FILE_READ);
    if (!file) {
      Serial.println(" -> Failed to open file.");
      continue;
    }

    StaticJsonDocument<LARGE_BUFFER_SIZE> entry;
    DeserializationError err = deserializeJson(entry, file);
    file.close();

    if (err) {
      Serial.print(" -> Failed to parse JSON: ");
      Serial.println(err.c_str());
    } else {
      Serial.println(" -> Parsed and added to meta.");
      meta.add(entry);
    }
  }

  Serial.print("Total valid entries added to meta: ");
  Serial.println(meta.size());
}

/**
 * @brief Wrapper function to save sensor data to SD card using global SD instance.
 *
 * @param celsius Temperature reading in Celsius.
 * @param now Current date and time for timestamp and file organization.
 * @param sequence Sequence number for the reading.
 */
void saveDataToSD(float celsius, const DateTime& now, int sequence) {
  saveToSD(sd, celsius, now, sequence); 
}

/**
 * @brief Saves sensor data to SD card as a JSON file.
 *
 * Creates a JSON document with sensor data and saves it to the SD card in a
 * date-organized folder structure. Creates directories as needed and handles
 * file writing with proper timestamping.
 *
 * @param sdRef Reference to the SD card file system instance.
 * @param celsius Temperature reading in Celsius.
 * @param now Current date and time for timestamp and file organization.
 * @param sequence Sequence number for the reading.
 */
void saveToSD(SdFat& sdRef, float celsius, const DateTime& now, int sequence) {
  Serial.println("Saving data to SD card...");
  StaticJsonDocument<LARGE_BUFFER_SIZE> doc;
  buildJson(doc, celsius, now, sequence);

  char folder[FOLDER_NAME_BUFFER_SIZE];
  strncpy(folder, createFolderName(now), sizeof(folder));

  if (!sdRef.exists(folder)) {
    if (!sdRef.mkdir(folder)) {
      Serial.println("Failed to create folder.");
      return;
    }
  }

  char filename[SMALL_BUFFER_SIZE];
  createFilename(filename, SMALL_BUFFER_SIZE, now);

  File file = sdRef.open(filename, FILE_WRITE);
  if (file) {
    // Write JSON to a buffer, then write buffer to file
    char jsonBuffer[LARGE_BUFFER_SIZE];
    size_t len = serializeJson(doc, jsonBuffer, sizeof(jsonBuffer));
    file.write((const uint8_t*)jsonBuffer, len);
    file.close();
    Serial.print("Saved JSON to SD card: ");
    Serial.println(filename);
  } else {
    Serial.println("Failed to write file.");
  }
}

/**
 * @brief Wrapper function to save recovered JSON data using global SD instance.
 *
 * @param fileList Array of file paths that were recovered.
 * @param count Number of files in the array.
 * @param now Current date and time for filename generation.
 */
void saveRecoveredJsonDataToSd(String* fileList, int count, const DateTime& now) {
  saveRecoveredJsonToSd(sd, fileList, count, now);
}

/**
 * @brief Saves consolidated recovered sensor data to SD card as a single JSON file.
 *
 * Takes multiple recovered sensor data entries and saves them as one consolidated
 * JSON file with a "_recovered" suffix. This file serves as a backup of the
 * data that was sent to the MQTT broker.
 *
 * @param sdRef Reference to the SD card file system instance.
 * @param fileList Array of original file paths that were recovered.
 * @param count Number of files in the array.
 * @param now Current date and time for filename generation.
 */
void saveRecoveredJsonToSd(SdFat& sdRef, String* fileList, int count, const DateTime& now) {
  Serial.println("Saving recovered JSON to SD...");
  StaticJsonDocument<XLARGE_BUFFER_SIZE> doc;
  buildRecoveredJson(doc, fileList, count, now);

  char baseName[SMALL_BUFFER_SIZE];
  createFilename(baseName, sizeof(baseName), now); 

  char recoveredName[RECOVERED_NAME_BUFFER_SIZE];
  snprintf(recoveredName, sizeof(recoveredName), "%.*s_recovered.json",
           (int)(strlen(baseName) - FILE_EXTENSION_LENGTH), baseName);

  File file = sdRef.open(recoveredName, FILE_WRITE);
  if (file) {
    char jsonBuffer[XLARGE_BUFFER_SIZE];
    size_t len = serializeJson(doc, jsonBuffer, sizeof(jsonBuffer));
    file.write((const uint8_t*)jsonBuffer, len);
    file.close();
    Serial.println("Saved recovered JSON to SD: " + String(recoveredName));
  } else {
    Serial.println("Failed to save recovered JSON.");
  }
}

/**
 * @brief Wrapper function to list saved files using global SD instance.
 *
 * @param fileList Array to store the found file paths.
 * @param maxFiles Maximum number of files to list.
 * @param now Current date and time for filtering files within 24 hours.
 * @return Number of files found and added to the list.
 */
int listSavedFilesData(String* fileList, int maxFiles, const DateTime& now) {
  return listSavedFiles(sd, fileList, maxFiles, now);
}

/**
 * @brief Lists saved sensor data files from the last 24 hours.
 *
 * Scans the SD card for JSON files in the current date folder, filters them
 * based on their timestamp to include only files from the last 24 hours,
 * and excludes recovery files.
 *
 * @param sdRef Reference to the SD card file system instance.
 * @param fileList Array to store the found file paths.
 * @param maxFiles Maximum number of files to list.
 * @param now Current date and time for filtering files within 24 hours.
 * @return Number of files found and added to the list.
 */
int listSavedFiles(SdFat& sdRef, String* fileList, int maxFiles, const DateTime& now) {
  Serial.println("Listing saved files from last 24h...");
  int count = 0;

  char folder[FOLDER_NAME_BUFFER_SIZE];
  strncpy(folder, createFolderName(now), sizeof(folder));
  File root = sdRef.open(folder);
  if (!root) {
    Serial.println("Folder not found or failed to open.");
    return 0;
  }

  File entry;
  while ((entry = root.openNextFile())) {
    if (!entry.isDirectory()) {
      char name[FILE_NAME_BUFFER_SIZE];
      entry.getName(name, sizeof(name));

      String nameStr(name);
      if (!nameStr.endsWith(".json") || nameStr.indexOf("_recovered") != -1) {
        entry.close();
        continue;
      }

      StaticJsonDocument<MEDIUM_BUFFER_SIZE> doc;
      DeserializationError err = deserializeJson(doc, entry);
      entry.close();

      if (!err) {
        uint32_t ts = doc["timestamp"] | 0;
        if (ts >= (now.unixtime() - SECONDS_IN_24_HOURS)) {
          if (count < maxFiles) {
            String fullPath = String(folder) + "/" + nameStr;
            fileList[count++] = fullPath;
            Serial.println("Adding to list: " + fullPath);
          }
        }
      }
    }
  }

  root.close();
  Serial.print("Total matching files: ");
  Serial.println(count);
  return count;
}

/**
 * @brief Wrapper function to delete recovered and pending files using global SD instance.
 *
 * @param fileList Array of original file paths to delete.
 * @param count Number of files in the array.
 * @param now Current date and time for folder path generation.
 * @param recoveredFilename Name of the recovery file to delete.
 */
void deleteRecoveredAndPendingSourceFilesData(const String* fileList, int count, const DateTime& now, const String& recoveredFilename) {
  deleteRecoveredAndPendingSourceFiles(sd, fileList, count, now, recoveredFilename);
}

/**
 * @brief Deletes original sensor data files and the recovery file after successful transmission.
 *
 * Removes the original sensor data files that were consolidated and sent, as well as
 * the recovery file that was created as backup. This cleanup prevents duplicate
 * data transmission and manages storage space.
 *
 * @param sdRef Reference to the SD card file system instance.
 * @param fileList Array of original file paths to delete.
 * @param count Number of files in the array.
 * @param now Current date and time for folder path generation.
 * @param recoveredFilename Name of the recovery file to delete.
 */
void deleteRecoveredAndPendingSourceFiles(SdFat& sdRef, const String* fileList, int count, const DateTime& now, const String& recoveredFilename) {
  char folder[FOLDER_NAME_BUFFER_SIZE];
  strncpy(folder, createFolderName(now), sizeof(folder));
  for (int i = 0; i < count; ++i) {
    String fullPath = String(folder) + "/" + fileList[i];
    if (sdRef.remove(fullPath.c_str())) {
      Serial.println("Deleted file: " + fullPath);
    } else {
      Serial.println("Failed to delete file: " + fullPath);
    }
  }

  if (sdRef.exists(recoveredFilename.c_str())) {
    if (sdRef.remove(recoveredFilename.c_str())) {
      Serial.println("Deleted recovered file: " + recoveredFilename);
    } else {
      Serial.println("Failed to delete recovered file: " + recoveredFilename);
    }
  }
}