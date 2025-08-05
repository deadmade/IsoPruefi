#pragma once
#include "platform.h"
#include <cstdio> // for snprintf

#ifndef UNIT_TEST

void saveToCsvBatch(const DateTime& now, float celsius, int sequence);
void saveToSD(SdFat& sd, float celsius, const DateTime& now, int sequence);
void saveRecoveredJsonToSd(SdFat& sd, String* fileList, int count, const DateTime& now);
int listSavedFiles(SdFat& sd, String* fileList, int maxFiles, const DateTime& now);
void deleteRecoveredAndPendingSourceFiles(SdFat& sd, const String* fileList, int count, const DateTime& now, const String& recoveredFilename);
void deleteCsvFile(const char* filepath);

void buildJson(JsonDocument& doc, float celsius, const DateTime& now, int sequence);
void buildRecoveredJson(JsonDocument& doc, String* fileList, int count, const DateTime& now);
void buildRecoveredJsonFromCsv(JsonDocument& doc, const char* filepath, const DateTime& now);

#else

// native test versions
inline void buildJson(JsonDocument& doc, float celsius, const DateTime& now, int sequence) {
    doc["sequence"] = std::to_string(sequence);
    doc["timestamp"] = std::to_string(now.unixtime());

    auto& valArr = doc.createNestedArray("value");
    valArr.add(celsius);

    auto& metaArr = doc.createNestedArray("meta");
    metaArr.add("null");
}

inline void buildRecoveredJson(JsonDocument& doc, String* fileList, int count, const DateTime& now) {
    doc["sequence"] = "recovered";
    doc["timestamp"] = std::to_string(now.unixtime());
    auto& arr = doc.createNestedArray("meta");
    for (int i = 0; i < count; ++i) {
        arr.add(fileList[i].c_str());
    }
}

#endif

// --- Wrapper functions (Arduino only) ---
void saveDataToSD(float celsius, const DateTime& now, int sequence);
void saveRecoveredJsonDataToSd(String* fileList, int count, const DateTime& now);
int listSavedFilesData(String* fileList, int maxFiles, const DateTime& now);
void deleteRecoveredAndPendingSourceFilesData(const String* fileList, int count, const DateTime& now, const String& recoveredFilename);

// --- Inline helper functions (shared) ---
inline const char* createFolderName(const DateTime& now) {
    static char folderName[8];
    std::snprintf(folderName, sizeof(folderName), "%04d", now.year());
    return folderName;
}

inline void createFilename(char* buffer, size_t bufferSize, const DateTime& now) {
    std::snprintf(buffer, bufferSize, "%s/%02d%02d%02d%02d.csv",
                  createFolderName(now),
                  now.month(), now.day(), now.hour(), now.minute());
}

inline void createRecoveredFilename(char* recoveredFilename, size_t bufferSize,
                                    const DateTime& now, int baseLength, const char* suffix = "_recovered.json") {
    char baseFilename[64];
    createFilename(baseFilename, sizeof(baseFilename), now);
    std::snprintf(recoveredFilename, bufferSize, "%.*s%s", baseLength, baseFilename, suffix);
} 