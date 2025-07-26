#include "storage.h"
#include "mqtt.h"

#define FILENAME_BUFFER_SIZE 32

// --- Interface-Funktion für Projektlogik ---

void saveDataToSD(float celsius, const DateTime& now, int sequence) {
  saveToSD(sd, celsius, now, sequence); // nutzt globalen SdFat aus platform.h
}

// --- Hauptfunktion für natives Speichern (unit-testbar) ---

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
