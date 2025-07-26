#pragma once

#include <Adafruit_ADT7410.h>
#include <RTClib.h>

// Initialisiert den Temperatursensor
bool initSensor(Adafruit_ADT7410& sensor);

// Liest die Temperatur in Grad Celsius
float readTemperature(Adafruit_ADT7410& sensor);

// Optional: Formatiert Zeit als ISO 8601-String
String formatTimestamp(const DateTime& now);
