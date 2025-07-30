#include "platform.h"
#include "core.h"
#include "network.h"
#include "mqtt.h"
#include "sensor.h"
#include "storage.h"
#include "secrets.h"

RTC_DS3231 rtc;
Adafruit_ADT7410 tempsensor;   
SdFat sd;

static WiFiClient wifiClient;
static MqttClient mqttClient(wifiClient);

static const uint8_t chipSelect = 4;
static const char* sensorIdOne = "Sensor_One";
// static const char* sensorIdTwo = "Sensor_Two";
static const char* sensorIdInUse = sensorIdOne; 
//const char* sensorIdInUse = sensorIdTwo; // Uncomment to use the second
static const char* sensorType = "temp";
static const char* topic = "dhbw/ai/si2023/2/";
static int lastLoggedMinute = -1;
static int count = 0;
static bool wifiOk = false;
static bool recoveredSent = false;


// Set timestamp for SD card
void dateTime(uint16_t* date, uint16_t* time) {
  DateTime now = rtc.now();
  *date = FAT_DATE(now.year(), now.month(), now.day());
  *time = FAT_TIME(now.hour(), now.minute(), now.second());
}

void coreSetup() {
  wifiOk = connectWiFi(15000);

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

  if (!initSensor(tempsensor)) {
    Serial.println("ADT7410 init failed!");
    while (1);
  }

  // DateTime now = rtc.now();
  // sendPendingData(mqttClient, topic, sensorType, sensorIdInUse, now);

  Serial.println("Setup complete.");
}

void coreLoop() {
  DateTime now = rtc.now();
  static bool alreadyLoggedThisMinute = false;

  if (now.minute() != lastLoggedMinute) {
    lastLoggedMinute = now.minute();
    alreadyLoggedThisMinute = false;
  }

  // Reconnect WiFi
  if (!wifiOk || WiFi.status() != WL_CONNECTED) {
    Serial.println("WiFi not connected. Trying to reconnect...");
    wifiOk = connectWiFi(15000);

    if (wifiOk && connectMQTT(mqttClient)) {
      Serial.println("WiFi ok: Reconnected to MQTT.");
      recoveredSent = false;  // Reset to allow sending recovered data again
    } else {
      Serial.println("Reconnect failed. Skipping loop.");
      if (!alreadyLoggedThisMinute) {
        float c = readTemperatureCelsius();
        saveToSD(sd, c, now, count);
        alreadyLoggedThisMinute = true;
        count++;
      }
      delay(1000);
      return;
    }
  }

  // Reconnect MQTT
  if (!mqttClient.connected()) {
    Serial.println("MQTT not connected. Trying to reconnect...");
    if (!connectMQTT(mqttClient)) {
      Serial.println("MQTT still not connected.");
      if (!alreadyLoggedThisMinute) {
        float c = readTemperatureCelsius();
        saveToSD(sd, c, now, count);
        alreadyLoggedThisMinute = true;
        count++;
      }
      delay(1000);
      return;
    }
    Serial.println("MQTT reconnected successfully.");
    recoveredSent = false; // Reset to allow sending recovered data again
  }

  // After successful MQTT connection, send pending data
  if (!recoveredSent && mqttClient.connected()) {
    sendPendingData(mqttClient, topic, sensorType, sensorIdInUse, now);
    recoveredSent = true;
  }

  // normal operation: read temperature and send to MQTT
  if (!alreadyLoggedThisMinute) {
    float c = readTemperatureCelsius();
    sendToMqtt(mqttClient, topic, sensorType, sensorIdInUse, c, now, count);
    alreadyLoggedThisMinute = true;
    count++;
  }

  mqttClient.poll();
  delay(1000);
}