#pragma once

#ifdef UNIT_TEST

  // --- MOCK-INCLUDES FÜR UNIT TESTING ---
  #include "mock_datetime.h"
  // Hier weitere Mock-Klassen bei Bedarf einfügen
  #define DateTime MockDateTime

#else

  // --- ORIGINALE ARDUINO-INCLUDES ---
  #include <Arduino.h> 
  #include <Wire.h>
  #include <SdFat.h>
  #include <RTClib.h>
  #include <Adafruit_ADT7410.h>
  #include <ArduinoJson.h>
  #include <ArduinoMqttClient.h>

  // Globale Hardware-Objekte (Deklaration)
  extern RTC_DS3231 rtc;
  extern SdFat sd;
  extern Adafruit_ADT7410 tempsensor;

  // Einheitlicher WiFi-Header je nach Board
  #if defined(ARDUINO_SAMD_MKRWIFI1010) || defined(ARDUINO_SAMD_NANO_33_IOT) || defined(ARDUINO_AVR_UNO_WIFI_REV2)
    #include <WiFiNINA.h>
  #elif defined(ARDUINO_SAMD_MKR1000)
    #include <WiFi101.h>
  #elif defined(ARDUINO_ARCH_ESP8266)
    #include <ESP8266WiFi.h>
  #elif defined(ARDUINO_PORTENTA_H7_M7) || defined(ARDUINO_NICLA_VISION) || defined(ARDUINO_ARCH_ESP32) || defined(ARDUINO_GIGA) || defined(ARDUINO_OPTA)
    #include <WiFi.h>
  #elif defined(ARDUINO_PORTENTA_C33)
    #include <WiFiC3.h>
  #elif defined(ARDUINO_UNOR4_WIFI)
    #include <WiFiS3.h>
  #else
    #error "Kein unterstützter WiFi-Treiber für dieses Board gefunden"
  #endif

#endif
