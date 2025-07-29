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
int listSavedFiles(String* fileList, int maxFiles, const DateTime& now) {
  Serial.println("Listing saved files on SD card...");
  int count = 0;

  char folder[8];
  strncpy(folder, createFolderName(now), sizeof(folder));
  File root = sd.open(folder);
  if (!root) return 0;

  File entry;
  while ((entry = root.openNextFile())) {
    if (!entry.isDirectory() && count < maxFiles) {
      char name[64];  // max. path length – adjust if needed
      entry.getName(name, sizeof(name));
      fileList[count++] = String(name);
    }
    entry.close();
  }
  Serial.print("Found Files on SD");
  root.close();
  return count;
}
