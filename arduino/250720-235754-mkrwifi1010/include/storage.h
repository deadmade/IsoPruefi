#pragma once

#include <SdFat.h>
#include <RTClib.h>

// Funktion für Arduino-Board
void saveToSD(SdFat& sd, float celsius, const DateTime& now, int sequence);

// Diese Funktionen können nativ getestet werden:
const char* createFolderName(const DateTime& now);
void createFilename(char* buffer, size_t bufferSize, const DateTime& now);
