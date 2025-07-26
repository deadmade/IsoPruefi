#include "storage.h"
#include "mqtt.h"
#include <ArduinoJson.h>
#include <Arduino.h>

#define FILENAME_BUFFER_SIZE 32

// --- Testbare Hilfsfunktionen ---

const char* createFolderName(const DateTime& now) {
  static char folderName[8];
  snprintf(folderName, sizeof(folderName), "%04d", now.year());
  return folderName;
}

void createFilename(char* buffer, size_t bufferSize, const DateTime& now) {
  snprintf(buffer, bufferSize, "%s/%02d%02d%02d%02d.json",
           createFolderName(now),
           now.month(), now.day(), now.hour(), now.minute());
}

// --- Hauptfunktion für Arduino ---

void saveToSD(SdFat& sd, float celsius, const DateTime& now, int sequence) {
  StaticJsonDocument<256> doc;
  buildJson(doc, celsius, now, sequence);

  char folder[8];
  strncpy(folder, createFolderName(now), sizeof(folder));

  if (!sd.exists(folder)) {
    if (!sd.mkdir(folder)) {
      Serial.println("Failed to create folder.");
      return;
    }
  }

  char filename[FILENAME_BUFFER_SIZE];
  createFilename(filename, FILENAME_BUFFER_SIZE, now);

  File file = sd.open(filename, FILE_WRITE);
  if (file) {
    serializeJsonPretty(doc, file);
    file.close();
    Serial.print("Saved JSON to SD card: ");
    Serial.println(filename);
  } else {
    Serial.println("Failed to write file.");
  }
}
