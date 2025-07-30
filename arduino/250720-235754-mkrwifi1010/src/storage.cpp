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
    // Write JSON to a buffer, then write buffer to file
    char jsonBuffer[256];
    size_t len = serializeJson(doc, jsonBuffer, sizeof(jsonBuffer));
    file.write((const uint8_t*)jsonBuffer, len);
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

  char baseName[32];
  createFilename(baseName, sizeof(baseName), now); 

  char recoveredName[40];
  snprintf(recoveredName, sizeof(recoveredName), "%.*s_recovered.json",
           (int)(strlen(baseName) - 5), baseName);

  File file = sd.open(recoveredName, FILE_WRITE);
  if (file) {
    // Write JSON to a buffer, then write buffer to file
    char jsonBuffer[1024];
    size_t len = serializeJson(doc, jsonBuffer, sizeof(jsonBuffer));
    file.write((const uint8_t*)jsonBuffer, len);
    file.close();
    Serial.println("Saved recovered JSON to SD: " + String(recoveredName));
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
      char name[64];
      entry.getName(name, sizeof(name));

      // Nur JSON-Dateien ohne "_recovered" zulassen
      String nameStr(name);
      if (!nameStr.endsWith(".json") || nameStr.indexOf("_recovered") != -1) {
        entry.close();
        continue;
      }

      StaticJsonDocument<128> doc;
      DeserializationError err = deserializeJson(doc, entry);
      entry.close();

      if (!err) {
        uint32_t ts = doc["timestamp"] | 0;
        if (ts >= (now.unixtime() - 86400)) {
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

void deleteRecoveredAndPendingSourceFiles(const String* fileList, int count, const DateTime& now, const String& recoveredFilename) {
  char folder[8];
  strncpy(folder, createFolderName(now), sizeof(folder));
  for (int i = 0; i < count; ++i) {
    String fullPath = String(folder) + "/" + fileList[i];
    if (sd.remove(fullPath.c_str())) {
      Serial.println("Deleted file: " + fullPath);
    } else {
      Serial.println("Failed to delete file: " + fullPath);
    }
  }

  if (sd.exists(recoveredFilename.c_str())) {
    if (sd.remove(recoveredFilename.c_str())) {
      Serial.println("Deleted recovered file: " + recoveredFilename);
    } else {
      Serial.println("Failed to delete recovered file: " + recoveredFilename);
    }
  }
}
