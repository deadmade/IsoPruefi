#include <Wire.h>
#include <RTClib.h>
#include <SdFat.h>
#include "Adafruit_ADT7410.h"
#include <ArduinoJson.h>
#include <ArduinoMqttClient.h>
#include "secrets.h"


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
#endif

// WiFi credentials
const char ssid[] = SECRET_SSID;
const char password[] = SECRET_PASS;
const char MQTT_USER[]  = SECRET_MQTT_USER; 
const char MQTT_PASS[]  = SECRET_MQTT_PASS; 

// MQTT server
const char* broker = "aicon.dhbw-heidenheim.de";
int port = 1883;
const char* topic = "dhbw/ai/si2023/2/";
const char* sensorIdOne = "Sensor_One";
const char* sensorIdTwo = "Sensor_Two";
const char* sensorIdInUse = sensorIdOne; 
//const char* sensorIdInUse = sensorIdTwo; // Uncomment to use the second
const char* sensorType = "temp"; //temperature

// RTC and temperature sensor
RTC_DS3231 rtc;
Adafruit_ADT7410 tempsensor = Adafruit_ADT7410();

// SD card
SdFat sd;
const uint8_t chipSelect = 4;

// WiFi & MQTT
WiFiClient wifiClient;
MqttClient mqttClient(wifiClient);

#define FILENAME_BUFFER_SIZE 32

// Track last minute
int lastLoggedMinute = -1;

// Timing for non-blocking MQTT send
unsigned long previousMillis = 0;
const long interval = 60000;
int count = 0;

// FAT timestamp for SD card
void dateTime(uint16_t* date, uint16_t* time) {
  DateTime now = rtc.now();
  *date = FAT_DATE(now.year(), now.month(), now.day());
  *time = FAT_TIME(now.hour(), now.minute(), now.second());
}

// create ISO8601 time format
String formatTimestamp(DateTime now) {
  char buffer[26];
  snprintf(buffer, sizeof(buffer), "%04d-%02d-%02d T%02d:%02d:%02d",
           now.year(), now.month(), now.day(),
           now.hour(), now.minute(), now.second());
  return String(buffer);
}



// Connect to WiFi with 10-second timeout
bool connectWiFi(unsigned long timeoutMs = 10000) {
  Serial.print("Connecting to WiFi...");
  WiFi.begin(ssid, password);

  unsigned long startAttemptTime = millis();
  while (WiFi.status() != WL_CONNECTED) {
    if (millis() - startAttemptTime >= timeoutMs) {
      Serial.println("\nWiFi connection timed out.");
      return false;  // failed
    }
    delay(500);
    Serial.print(".");
  }

  Serial.println("\nWiFi connected.");
  return true; // success
}

// Connect to MQTT with 10-second timeout
bool connectMQTT(unsigned long timeoutMs = 10000) {
  Serial.print("Connecting to MQTT...");
  unsigned long startAttemptTime = millis();

  while (!mqttClient.connect(broker, port)) {
    if (millis() - startAttemptTime >= timeoutMs) {
      Serial.println("\nMQTT connection timed out.");
      return false; // failed
    }
    Serial.print(".");
    delay(1000);
  }

  Serial.println(" connected.");
  return true; // success
}

//Build JSON payload
void buildJson(JsonDocument& doc, float celsius, DateTime now) {
  doc.clear();
  doc["timestamp"] = now.unixtime();
  JsonArray arr = doc.createNestedArray("value");
  arr.add(celsius);
  doc["sequence"] = count;
  count++;
}

// Modular MQTT send function
void SendToMqtt(float celsius, DateTime now) {
  mqttClient.poll();
  char fullTopic[128];
  snprintf(fullTopic, sizeof(fullTopic), "%s%s/%s", topic, sensorType, sensorIdInUse);

  StaticJsonDocument<128> jsonDoc;
  buildJson(jsonDoc, celsius, now);

  char payload[128];
  serializeJson(jsonDoc, payload);

  if (mqttClient.beginMessage(fullTopic)) {
    mqttClient.print(payload);
    mqttClient.endMessage();
    Serial.print("Published to ");
    Serial.println(fullTopic);
    Serial.println(payload);
  } else {
    Serial.println("MQTT publish failed.");
  }
}

// Writes JSON to SD card as fallback
void saveToSD(float celsius, DateTime now) {
  StaticJsonDocument<256> doc;
  buildJson(doc, celsius, now);

  // create folder jjjj
  char folderName[8];
  snprintf(folderName, sizeof(folderName), "%04d", now.year());
  if (!sd.exists(folderName)) {
    if (!sd.mkdir(folderName)) {
      Serial.println("Failed to create folder.");
      return;
    }
  }

  // Dateiname: MMDDHHMM.json
  char filename[FILENAME_BUFFER_SIZE];
  snprintf(filename, FILENAME_BUFFER_SIZE, "%s/%02d%02d%02d%02d.json",
           folderName, now.month(), now.day(), now.hour(), now.minute());

  File file = sd.open(filename, FILE_WRITE);
  if (file) {
    serializeJsonPretty(doc, file);
    file.close();
    Serial.println("Saved JSON to SD card.");
  } else {
    Serial.println("Failed to write file.");
  }
}

void setup() {
  Serial.begin(9600);
  while (!Serial);

  bool wifiOk = connectWiFi();
  char clientId[64];
  snprintf(clientId, sizeof(clientId), "IsoPruefi_%s", sensorIdInUse);
  mqttClient.setId(clientId);
  if (wifiOk) {
    connectMQTT();
  }
  if (!rtc.begin()) {
    Serial.println("RTC not found!");
    while (1);
  }
  if (rtc.lostPower()) {
    rtc.adjust(DateTime(F(__DATE__), F(__TIME__)));
  }

  SdFile::dateTimeCallback(dateTime);
  if (!sd.begin(chipSelect, SD_SCK_MHZ(25))) {
    Serial.println("SD card failed.");
    while (1);
  }

  if (!tempsensor.begin()) {
    Serial.println("ADT7410 not found!");
    while (1);
  }

  delay(250);
  tempsensor.setResolution(ADT7410_16BIT);
  Serial.println("Setup complete.");
}

void loop() {
  DateTime now = rtc.now();

  if (now.minute() != lastLoggedMinute) {
    lastLoggedMinute = now.minute();
    float c = tempsensor.readTempC();

    // Try MQTT
    if (mqttClient.connected()) {
      Serial.println("Trying to publish via MQTT...");
      SendToMqtt(c, now);
    } else {
      Serial.println("MQTT not connected – saving to SD card...");
      saveToSD(c, now);
    }
  }

  mqttClient.poll();
  delay(1000);
}