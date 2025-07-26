#pragma once

#include "platform.h"

// Initialisiert den Temperatursensor
bool initSensor(Adafruit_ADT7410& sensor);

// Interface-Funktion (nutzt globalen Sensor aus platform.h)
float readTemperatureCelsius();

// Optional: Formatiert Zeit als ISO 8601-String
String formatTimestamp(const DateTime& now);
