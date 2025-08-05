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
// static const uint32_t SECONDS_IN_24_HOURS = 86400;

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

void saveToCsvBatch(const DateTime& now, float celsius, int sequence) {
  char folder[8];
  strncpy(folder, createFolderName(now), sizeof(folder));

  if (!sd.exists(folder)) {
    sd.mkdir(folder);
  }

  static char currentFilename[32] = "";  // speichert aktuellen aktiven Dateinamen
  static int linesInFile = 0;

  if (strlen(currentFilename) == 0 || linesInFile >= 20) {
    // Neue Datei starten
    createFilename(currentFilename, sizeof(currentFilename), now);
    linesInFile = 0;
  }

  File file = sd.open(currentFilename, FILE_WRITE);
  if (file) {
    char line[64];
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

void buildRecoveredJsonFromCsv(JsonDocument& doc, const char* filepath, const DateTime& now) {
  File file = sd.open(filepath, FILE_READ);
  if (!file) {
    Serial.print("CSV not found: ");
    Serial.println(filepath);
    return;
  }

  doc.clear();
  doc["timestamp"] = now.unixtime();
  doc["sequence"] = nullptr;
  doc.createNestedArray("value").add(nullptr);
  JsonArray meta = doc.createNestedArray("meta");

  char line[64];
  int added = 0;

  while (file.available()) {
    size_t len = file.fgets(line, sizeof(line));
    if (len == 0) continue;

    char* p = strtok(line, ",");
    if (!p) continue;
    uint32_t ts = atol(p);

    p = strtok(nullptr, ",");
    if (!p) continue;
    float temp = atof(p);

    p = strtok(nullptr, ",");
    if (!p) continue;
    int seq = atoi(p);

    JsonObject entry = meta.createNestedObject();
    entry["timestamp"] = ts;
    JsonArray valueArr = entry.createNestedArray("value");
    valueArr.add(temp);
    entry["sequence"] = seq;

    added++;
  }

  file.close();

  Serial.print("Recovered entries added from CSV: ");
  Serial.print(added);
  Serial.print(" (");
  Serial.print(filepath);
  Serial.println(")");
}

void deleteCsvFile(const char* filepath) {
  if (sd.exists(filepath)) {
    if (sd.remove(filepath)) {
      Serial.print("Deleted CSV file: ");
      Serial.println(filepath);
    } else {
      Serial.print("Failed to delete CSV file: ");
      Serial.println(filepath);
    }
  }
}