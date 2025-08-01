#pragma once

#ifdef UNIT_TEST
  #include "mock_datetime.h"
  #include "mock_json.h"
  #include <string>
  typedef std::string String;
  #define DateTime MockDateTime
  #define JsonDocument MockJsonDocument
  #define JsonArray MockJsonArray


#else

  // --- ORIGINAL ARDUINO-INCLUDES ---
  #include <Arduino.h> 
  #include <Wire.h>
  #include <SdFat.h>
  #include <RTClib.h>
  #include <Adafruit_ADT7410.h>
  #include <ArduinoJson.h>
  #include <ArduinoMqttClient.h>

  // Global hardware objects (declaration)
  extern RTC_DS3231 rtc;
  extern SdFat sd;
  extern Adafruit_ADT7410 tempsensor;

  // Unified WiFi header depending on board
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
    #error "No supported WiFi driver found for this board"
  #endif

#endif
