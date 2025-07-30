#include "storage.h"

#define FILENAME_BUFFER_SIZE 32

void buildJson(JsonDocument& doc, float celsius, const DateTime& now, int sequence) {
  doc.clear();
  doc["timestamp"] = now.unixtime();
  JsonArray arr = doc.createNestedArray("value");
  arr.add(celsius);
  doc["sequence"] = sequence;
  doc.createNestedArray("meta").add(nullptr);
}

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

    StaticJsonDocument<256> entry;
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

void saveDataToSD(float celsius, const DateTime& now, int sequence) {
  saveToSD(sd, celsius, now, sequence); 
}

void saveToSD(SdFat& sdRef, float celsius, const DateTime& now, int sequence) {
  Serial.println("Saving data to SD card...");
  StaticJsonDocument<256> doc;
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
    serializeJson(doc, file);
    file.close();
    Serial.print("Saved JSON to SD card: ");
    Serial.println(filename);
  } else {
    Serial.println("Failed to write file.");
  }
}

void saveRecoveredJsonToSd(String* fileList, int count, const DateTime& now) {
  Serial.println("Saving recovered JSON to SD...");
  StaticJsonDocument<1024> doc;
  buildRecoveredJson(doc, fileList, count, now);

  char filename[32];
  snprintf(filename, sizeof(filename), "%s/recovered_%lu.json", createFolderName(now), now.unixtime());

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
  if (!root) {
    Serial.println("Folder not found or failed to open.");
    return 0;
  }

  File entry;
  while ((entry = root.openNextFile())) {
    if (!entry.isDirectory()) {
      StaticJsonDocument<128> doc;
      DeserializationError err = deserializeJson(doc, entry);
      if (!err) {
        uint32_t ts = doc["timestamp"] | 0;
        if (ts >= (now.unixtime() - 86400)) {
          if (count < maxFiles) {
            char name[64];
            entry.getName(name, sizeof(name));
            fileList[count++] = String(folder) + "/" + String(name);
            Serial.print("Adding to list: ");
            Serial.println(fileList[count - 1]);
          }
        }
      }
    }
    entry.close();
  }

  root.close();
  Serial.print("Total matching files: ");
  Serial.println(count);
  return count;
}
