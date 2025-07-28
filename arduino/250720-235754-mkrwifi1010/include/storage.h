#pragma once
#include "platform.h"
#include <cstdio> // for snprintf

// Only available on Arduino
#ifndef UNIT_TEST
void saveToSD(SdFat& sd, float celsius, const DateTime& now, int sequence);
#endif

void saveDataToSD(float celsius, const DateTime& now, int sequence);
int listSavedFiles(String* fileList, int maxFiles);


// --- Inline helper functions (for Arduino & native build) ---
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
