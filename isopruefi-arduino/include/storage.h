#pragma once

#include "platform.h"
#include <cstdio> 

void SaveTempToBatchCsv(const DateTime& now, float celsius, int sequence);
void DeleteCsvFile(const char* filepath);

void BuildJson(JsonDocument& doc, float celsius, const DateTime& now, int sequence);
void BuildRecoveryJsonFromBatchCsv(JsonDocument& doc, const char* filepath, const DateTime& now);

// --- Inline helper functions ---
inline const char* CreateFolderName(const DateTime& now) {
    static char folderName[8];
    std::snprintf(folderName, sizeof(folderName), "%04d", now.year());
    return folderName;
}

inline void CreateCsvFilename(char* buffer, size_t bufferSize, const DateTime& now) {
    std::snprintf(buffer, bufferSize, "%s/%02d%02d%02d%02d.csv",
                  CreateFolderName(now),
                  now.month(), now.day(), now.hour(), now.minute());
}