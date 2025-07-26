#pragma once

#include "platform.h"

// Hauptfunktion für Arduino (mit globalem SdFat-Objekt)
void saveDataToSD(float celsius, const DateTime& now, int sequence);

// Interface-Funktion für Speichern auf SD-Karte mit übergebenem Objekt
void saveToSD(SdFat& sd, float celsius, const DateTime& now, int sequence);

// Diese Funktionen können nativ getestet werden:
const char* createFolderName(const DateTime& now);
void createFilename(char* buffer, size_t bufferSize, const DateTime& now);
