#pragma once
#include "platform.h"
#include <cstdio> // for snprintf

#ifndef UNIT_TEST
// Arduino-only functions with real types
void saveToSD(SdFat& sd, float celsius, const DateTime& now, int sequence);
void saveRecoveredJsonToSd(SdFat& sd, String* fileList, int count, const DateTime& now);
int listSavedFiles(SdFat& sd, String* fileList, int maxFiles, const DateTime& now);
void deleteRecoveredAndPendingSourceFiles(SdFat& sd, const String* fileList, int count, const DateTime& now, const String& recoveredFilename);

// ArduinoJson
void buildJson(JsonDocument& doc, float celsius, const DateTime& now, int sequence);
void buildRecoveredJson(JsonDocument& doc, String* fileList, int count, const DateTime& now);

#else
// Dummy prototypes for native tests
inline void buildJson(void*, float, const DateTime&, int) {}
inline void buildRecoveredJson(void*, void*, int, const DateTime&) {}
#endif

// Wrapper functions (also Arduino-only)
#ifndef UNIT_TEST
void saveDataToSD(float celsius, const DateTime& now, int sequence);
void saveRecoveredJsonDataToSd(String* fileList, int count, const DateTime& now);
int listSavedFilesData(String* fileList, int maxFiles, const DateTime& now);
void deleteRecoveredAndPendingSourceFilesData(const String* fileList, int count, const DateTime& now, const String& recoveredFilename);
#endif

// --- Inline helper functions (platform independent) ---
inline const char* createFolderName(const DateTime& now) {
    static char folderName[8];
    std::snprintf(folderName, sizeof(folderName), "%04d", now.year());
    return folderName;
}

inline void createFilename(char* buffer, size_t bufferSize, const DateTime& now) {
    std::snprintf(buffer, bufferSize, "%s/%02d%02d%02d%02d.json",
                  createFolderName(now),
                  now.month(), now.day(), now.hour(), now.minute());
}
