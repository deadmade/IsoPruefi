#include "core.h"
#include "network.h"
#include "mqtt.h"
#include "sensor.h"
#include "storage.h"
#include "platform.h"
#include <RTClib.h>
#include <Adafruit_ADT7410.h>
#include <SdFat.h>
#include <ArduinoMqttClient.h>
#include "secrets.h"

static RTC_DS3231 rtc;
static Adafruit_ADT7410 tempsensor = Adafruit_ADT7410();
static SdFat sd;
static WiFiClient wifiClient;
static MqttClient mqttClient(wifiClient);

static const uint8_t chipSelect = 4;
static const char* sensorIdOne = "Sensor_One";
static const char* sensorIdTwo = "Sensor_Two";
static const char* sensorIdInUse = sensorIdOne; 
//const char* sensorIdInUse = sensorIdTwo; // Uncomment to use the second
static const char* sensorType = "temp";
static const char* topic = "dhbw/ai/si2023/2/";
static int lastLoggedMinute = -1;
static int count = 0;

// Zeitstempel für SD-Karte setzen
void dateTime(uint16_t* date, uint16_t* time) {
  DateTime now = rtc.now();
  *date = FAT_DATE(now.year(), now.month(), now.day());
  *time = FAT_TIME(now.hour(), now.minute(), now.second());
}

void coreSetup() {
  bool wifiOk = connectWiFi();

  char clientId[64];
  snprintf(clientId, sizeof(clientId), "IsoPruefi_%s", sensorIdInUse);
  mqttClient.setId(clientId);
  if (wifiOk) {
    connectMQTT(mqttClient);
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

void coreLoop() {
  DateTime now = rtc.now();

  if (now.minute() != lastLoggedMinute) {
    lastLoggedMinute = now.minute();
    float c = tempsensor.readTempC();

    if (mqttClient.connected()) {
      Serial.println("Trying to publish via MQTT...");
      sendToMqtt(mqttClient, topic, sensorType, sensorIdInUse, c, now, count);
    } else {
      Serial.println("MQTT not connected – saving to SD card...");
      saveToSD(sd, c, now, count);
    }
    count++;
  }

  mqttClient.poll();
  delay(1000);
}
