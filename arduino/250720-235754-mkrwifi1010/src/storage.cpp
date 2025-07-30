#include "storage.h"
#include "mqtt.h"

#define FILENAME_BUFFER_SIZE 32

// --- Interface for project logic ---
void saveDataToSD(float celsius, const DateTime& now, int sequence) {
  saveToSD(sd, celsius, now, sequence); 
}

// --- Main function for native saving (unit-testable) ---

void saveToSD(SdFat& sdRef, float celsius, const DateTime& now, int sequence) {
  ArduinoJson::StaticJsonDocument<256> doc;
  buildJson(doc, celsius, now, sequence);

  char folder[8];
  strncpy(folder, createFolderName(now), sizeof(folder));

  if (!sdRef.exists(folder)) {
    if (!sdRef.mkdir(folder)) {
      Serial.println("Failed to create folder.");
      return;
    }
  }

  char filename[FILENAME_BUFFER_SIZE];
  createFilename(filename, FILENAME_BUFFER_SIZE, now);

  File file = sdRef.open(filename, FILE_WRITE);
  if (file) {
    serializeJsonPretty(doc, file);
    file.close();
    Serial.print("Saved JSON to SD card: ");
    Serial.println(filename);
  } else {
    Serial.println("Failed to write file.");
  }
}

void saveRecoveredJsonToSd(String* fileList, int count, const DateTime& now) {
  StaticJsonDocument<1024> doc;
  buildRecoveredJson(doc, fileList, count, now);

  char filename[32];
  snprintf(filename, sizeof(filename), "/recovered_%lu.json", now.unixtime());

  File file = sd.open(filename, FILE_WRITE);
  if (file) {
    serializeJson(doc, file);
    file.close();
    Serial.println("Saved recovered JSON to SD: " + String(filename));
  } else {
    Serial.println("Failed to save recovered JSON.");
  }
}


int listSavedFiles(String* fileList, int maxFiles, const DateTime& now) {
  Serial.println("Listing saved files from last 24h...");
  int count = 0;

  char folder[8];
  strncpy(folder, createFolderName(now), sizeof(folder));
  File root = sd.open(folder);
  Serial.print("Opening folder: ");
  if (!root) {
    Serial.println("Folder not found or failed to open.");
    return 0;
  }

  File entry;
  while ((entry = root.openNextFile())) {
    Serial.print("Found file: ");
    if (!entry.isDirectory()) {
      Serial.println("file entry");
      StaticJsonDocument<128> doc;
      DeserializationError err = deserializeJson(doc, entry);

      if (!err) {
        Serial.print("Timestamp: ");
        uint32_t ts = doc["timestamp"] | 0;
        if (ts >= (now.unixtime() - 86400)) {
          Serial.println(ts);
          if (count < maxFiles) {
            Serial.println("Adding to list: ");
            char name[64];
            entry.getName(name, sizeof(name));
            fileList[count++] = String(name);
          }
        }
      }
    }
    entry.close(); 
    Serial.println("Entry closed.");
  }

  root.close();
  Serial.print("Total matching files: ");
  Serial.println(count);
  return count;
}

