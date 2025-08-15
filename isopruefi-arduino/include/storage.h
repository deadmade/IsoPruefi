#pragma once
#include "platform.h"
#include <cstdio> // for snprintf

void saveToCsvBatch(const DateTime& now, float celsius, int sequence);
void deleteCsvFile(const char* filepath);

void buildJson(JsonDocument& doc, float celsius, const DateTime& now, int sequence);
void buildRecoveredJsonFromCsv(JsonDocument& doc, const char* filepath, const DateTime& now);

// inline void buildRecoveredJson(JsonDocument& doc, String* fileList, int count, const DateTime& now) {
//     doc["sequence"] = "recovered";
//     doc["timestamp"] = std::to_string(now.unixtime());
//     auto& arr = doc.createNestedArray("meta");
//     for (int i = 0; i < count; ++i) {
//         arr.add(fileList[i].c_str());
//     }
// }

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