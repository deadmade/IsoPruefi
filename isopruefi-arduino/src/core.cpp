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

// Timing and connection constants
static const unsigned long WIFI_CONNECT_TIMEOUT_MS = 15000;
static const unsigned long LOOP_DELAY_MS = 1000;
static const size_t CLIENT_ID_BUFFER_SIZE = 64;
static const uint8_t SD_SCK_FREQUENCY_MHZ = 25;
static int lastLoggedMinute = -1;
static int count = 0;
static bool wifiOk = false;
static bool recoveredSent = false;


/**
 * @brief Retrieves the current date and time from the RTC and formats them for FAT file systems.
 *
 * This function obtains the current date and time from the real-time clock (RTC)
 * and encodes them using the FAT file system date and time format macros.
 *
 * @param[out] date Pointer to a uint16_t variable where the encoded FAT date will be stored.
 * @param[out] time Pointer to a uint16_t variable where the encoded FAT time will be stored.
 */
void dateTime(uint16_t* date, uint16_t* time) {
  DateTime now = rtc.now();
  *date = FAT_DATE(now.year(), now.month(), now.day());
  *time = FAT_TIME(now.hour(), now.minute(), now.second());
}

/**
 * @brief Initializes core system components.
 *
 * This function performs the following setup steps:
 * - Connects to WiFi and sets the global wifiOk flag.
 * - Generates and sets the MQTT client ID based on the sensor in use.
 * - Connects to the MQTT broker if WiFi connection is successful.
 * - Initializes the real-time clock (RTC) and sets the time if power was lost.
 * - Registers the date/time callback for SD file timestamps.
 * - Initializes the SD card and halts execution if initialization fails.
 * - Initializes the temperature sensor and halts execution if initialization fails.
 * - Prints a message to the serial console upon successful setup.
 *
 * This function will halt execution (infinite loop) if any critical component fails to initialize.
 */
void coreSetup() {
  wifiOk = connectWiFi(WIFI_CONNECT_TIMEOUT_MS);

  char clientId[CLIENT_ID_BUFFER_SIZE];
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
  if (!sd.begin(chipSelect, SD_SCK_MHZ(SD_SCK_FREQUENCY_MHZ))) {
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

/**
 * @brief Main loop function for core operations.
 *
 * This function manages the main operational loop, including:
 * - Reading the current time from the RTC.
 * - Ensuring WiFi and MQTT connections are active, attempting reconnection if necessary.
 * - Logging temperature data to SD card if network connections are unavailable.
 * - Sending recovered (pending) data after reconnection.
 * - Reading and sending temperature data to MQTT broker once per minute.
 * - Polling the MQTT client and maintaining a fixed loop delay.
 *
 * The function ensures that temperature data is not lost during connectivity issues
 * by saving to SD card and resending when connections are restored.
 */
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
    wifiOk = connectWiFi(WIFI_CONNECT_TIMEOUT_MS);

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
      delay(LOOP_DELAY_MS);
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
      delay(LOOP_DELAY_MS);
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
  delay(LOOP_DELAY_MS);
}