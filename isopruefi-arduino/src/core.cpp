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
static const int RECONNECT_INTERVAL_MS = 2000;
static unsigned long lastReconnectAttempt = 0;

bool isWifiConnected() {
  return WiFi.status() == WL_CONNECTED;
}
bool isMqttConnected() {
  return mqttClient.connected();
}

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

  DateTime now = rtc.now();
  sendPendingData(mqttClient, topic, sensorType, sensorIdInUse, now);

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
  Serial.print("RTC time: ");
  Serial.println(now.timestamp());
  static bool alreadyLoggedThisMinute = false;

  if (now.minute() != lastLoggedMinute) {
    lastLoggedMinute = now.minute();
    alreadyLoggedThisMinute = false;
  }

  // Schritt 1: WiFi-Verbindung prüfen
  if (!isWifiConnected()) {
    if (millis() - lastReconnectAttempt > RECONNECT_INTERVAL_MS) {
      lastReconnectAttempt = millis();
      Serial.println("WiFi not connected. Trying to reconnect...");
      wifiOk = connectWiFi(WIFI_CONNECT_TIMEOUT_MS);
    }

    if (!wifiOk) {
      Serial.println("WiFi reconnect failed. Skipping loop.");
      if (!alreadyLoggedThisMinute) {
        float c = readTemperatureCelsius();
        saveToCsvBatch(now, c, count);
        alreadyLoggedThisMinute = true;
        count++;
      }
      delay(LOOP_DELAY_MS);
      return;
    }
  }

  // Schritt 2: MQTT-Verbindung prüfen
  if (!isMqttConnected()) {
    Serial.println("MQTT not connected. Trying to reconnect...");
    if (!connectMQTT(mqttClient)) {
      Serial.println("MQTT reconnect failed. Skipping loop.");
      if (!alreadyLoggedThisMinute) {
        float c = readTemperatureCelsius();
        saveToCsvBatch(now, c, count);
        alreadyLoggedThisMinute = true;
        count++;
      }
      delay(LOOP_DELAY_MS);
      return;
    }

    Serial.println("MQTT reconnected successfully.");
    recoveredSent = false; // Wiederherstellung erneut erlauben
  }

  // Schritt 3: Nach erfolgreichem MQTT-Reconnect → alte CSVs senden
  if (!recoveredSent && mqttClient.connected()) {
    if (sendPendingData(mqttClient, topic, sensorType, sensorIdInUse, now)) {
      recoveredSent = true;
    }
  }

  // Schritt 4: Normale Messung und MQTT-Versand
  if (!alreadyLoggedThisMinute) {
    float c = readTemperatureCelsius();
    sendToMqtt(mqttClient, topic, sensorType, sensorIdInUse, c, now, count);
    alreadyLoggedThisMinute = true;
    count++;
  }

  // Schritt 5: MQTT-Loop und Wartezeit
  mqttClient.poll();
  delay(LOOP_DELAY_MS);
}
