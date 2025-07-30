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

  DateTime now = rtc.now();
  // sendPendingData(mqttClient, topic, sensorType, sensorIdInUse, now);
  Serial.println("Sensor initialized successfully.");

  Serial.println("Setup complete.");
}

void coreLoop() {
  DateTime now = rtc.now();

  // static bool recoveredSent = false;
  if (now.minute() != lastLoggedMinute) {
    lastLoggedMinute = now.minute();
    float c = readTemperatureCelsius();

    if (mqttClient.connected()) {
      Serial.print("Publishing data: ");
      sendToMqtt(mqttClient, topic, sensorType, sensorIdInUse, c, now, count);
    } else {
      Serial.println("MQTT not connected");
      saveToSD(sd, c, now, count);
    }

    count++;
  }

  if (!wifiOk || WiFi.status() != WL_CONNECTED) {
  Serial.println("WiFi not connected. Trying to reconnect...");
  wifiOk = connectWiFi(15000);

    if (wifiOk && connectMQTT(mqttClient)) {
      Serial.println("WiFi ok: Reconnected to MQTT.");
      sendPendingData(mqttClient, topic, sensorType, sensorIdInUse, now);
    } else {
      Serial.println("Reconnect failed. Skipping loop.");
      float c = readTemperatureCelsius();
      saveToSD(sd, c, now, count);

      delay(1000);
      return;
    }
}

  if (!mqttClient.connected()) {
    Serial.println("MQTT not connected. Trying to reconnect...");
      if (!connectMQTT(mqttClient)) {
        Serial.println("MQTT not connected");
        float c = readTemperatureCelsius();
        saveToSD(sd, c, now, count);
        delay(1000);
        return;
      }
    sendPendingData(mqttClient, topic, sensorType, sensorIdInUse, now);
    Serial.println("MQTT reconnected successfully.");
  }

  mqttClient.poll();
  delay(1000);
}