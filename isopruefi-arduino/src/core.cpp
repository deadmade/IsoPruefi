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

// =============================================================================
// SYSTEM CONFIGURATION CONSTANTS
// =============================================================================

/// SD card chip select pin
static const uint8_t chipSelect = 4;
static const char* sensorIdOne = "Sensor_One";
static const char* sensorIdInUse = sensorIdOne; 
// static const char* sensorIdTwo = "Sensor_Two";
// static const char* sensorIdInUse = sensorIdTwo; // Uncomment to use the second
static const char* sensorType = "temp";
static const char* topic = "dhbw/ai/si2023/2/";

// =============================================================================
// TIMING AND CONNECTION CONSTANTS
// =============================================================================

static const unsigned long WIFI_CONNECT_TIMEOUT_MS = 15000;
static const unsigned long LOOP_DELAY_MS = 1000;
static const size_t CLIENT_ID_BUFFER_SIZE = 64;
static const uint8_t SD_SCK_FREQUENCY_MHZ = 25;
static const int RECONNECT_INTERVAL_MS = 2000;

// =============================================================================
// SYSTEM STATE VARIABLES
// =============================================================================

static int lastLoggedMinute = -1;
static int count = 0;
static bool wifiOk = false;
static bool recoveredSent = false;
static unsigned long lastReconnectAttempt = 0;

// =============================================================================
// CONNECTION STATUS FUNCTIONS
// =============================================================================

bool isWifiConnected() {
  return WiFi.status() == WL_CONNECTED;
}

bool isMqttConnected() {
  return mqttClient.connected();
}

// =============================================================================
// FAT FILE SYSTEM CALLBACK FUNCTIONS
// =============================================================================

/**
 * @brief Callback function for FAT file system timestamp generation
 * 
 * This function is used by the SdFat library to obtain current date and time
 * for file system operations. It retrieves the time from the RTC and converts
 * it to the FAT file system format using the appropriate macros.
 * 
 * @param[out] date Pointer to store the encoded FAT date (year, month, day)
 * @param[out] time Pointer to store the encoded FAT time (hour, minute, second)
 * 
 * @note This function is registered as a callback with SdFile::dateTimeCallback()
 * @see FAT_DATE, FAT_TIME macros for encoding format details
 */
void dateTime(uint16_t* date, uint16_t* time) {
  DateTime now = rtc.now();
  *date = FAT_DATE(now.year(), now.month(), now.day());
  *time = FAT_TIME(now.hour(), now.minute(), now.second());
}

// =============================================================================
// SYSTEM INITIALIZATION FUNCTIONS
// =============================================================================

/**
 * @brief Initializes all core system components and peripherals
 * 
 * This function performs comprehensive system initialization including:
 * 
 * **Network Setup:**
 * - Establishes WiFi connection with timeout handling
 * - Configures MQTT client with unique sensor-based ID
 * - Attempts initial MQTT broker connection
 * 
 * **Hardware Initialization:**
 * - Initializes DS3231 real-time clock module
 * - Adjusts RTC time if power was lost (uses compilation timestamp)
 * - Sets up SD card with SPI communication
 * - Initializes ADT7410 temperature sensor
 * 
 * **Data Recovery:**
 * - Registers FAT file system timestamp callback
 * - Attempts to send any pending/recovered data from previous sessions
 * 
 * @warning This function will halt program execution (infinite loop) if any
 *          critical component fails to initialize (RTC, SD card, or temperature sensor)
 * 
 * @note The function uses compile-time constants for timeouts and configuration
 * @see WIFI_CONNECT_TIMEOUT_MS, CLIENT_ID_BUFFER_SIZE, SD_SCK_FREQUENCY_MHZ
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

  // Set RTC time if power was lost (uses compilation timestamp)
  if (rtc.lostPower()) {
    rtc.adjust(DateTime(F(__DATE__), F(__TIME__)));
  }

  // Register callback for SD file timestamps and initialize SD card
  SdFile::dateTimeCallback(dateTime);
  if (!sd.begin(chipSelect, SD_SCK_MHZ(SD_SCK_FREQUENCY_MHZ))) {
    Serial.println("SD card failed.");
    while (1);
  }

  if (!initSensor(tempsensor)) {
    Serial.println("ADT7410 init failed!");
    while (1);
  }

  // Attempt to send any pending recovery data from previous sessions
  DateTime now = rtc.now();
  Serial.print("Current time: ");
  Serial.println(now.timestamp(DateTime::TIMESTAMP_FULL));
  Serial.print("Lost Power? "); Serial.println(rtc.lostPower() ? "YES" : "NO");

  Serial.println("Setup complete.");
}

// =============================================================================
// MAIN OPERATIONAL LOOP
// =============================================================================

/**
 * @brief Main operational loop for continuous sensor monitoring and data transmission
 * 
 * This function implements the core operational logic of the temperature monitoring
 * system with robust connectivity handling and data persistence features.
 * 
 * **Operational Flow:**
 * 
 * 1. **Time Management:**
 *    - Reads current time from RTC
 *    - Tracks minute changes to prevent duplicate measurements
 * 
 * 2. **WiFi Connection Management:**
 *    - Monitors WiFi status with intelligent reconnection timing
 *    - Falls back to CSV logging during WiFi outages
 *    - Implements reconnection throttling to prevent excessive attempts
 * 
 * 3. **MQTT Connection Management:**
 *    - Verifies MQTT broker connectivity
 *    - Handles automatic reconnection with status reporting
 *    - Falls back to CSV logging during MQTT outages
 * 
 * 4. **Data Recovery Processing:**
 *    - Sends pending CSV data after successful reconnection
 *    - Ensures recovery data is transmitted only once per reconnection cycle
 * 
 * 5. **Normal Operation:**
 *    - Performs temperature measurements once per minute
 *    - Transmits data via MQTT to configured topic
 *    - Maintains MQTT client polling for incoming messages
 * 
 * **Error Handling:**
 * - Network failures trigger CSV fallback storage
 * - Connection attempts are rate-limited to prevent resource exhaustion
 * - All measurement data is preserved during connectivity issues
 * 
 * @note The function maintains a fixed loop delay for consistent timing
 * @see RECONNECT_INTERVAL_MS, LOOP_DELAY_MS for timing configuration
 * @see saveToCsvBatch() for offline data storage
 * @see sendPendingData() for data recovery mechanism
 */
void coreLoop() {
  DateTime now = rtc.now();
  static bool alreadyLoggedThisMinute = false;

  if (now.minute() != lastLoggedMinute) {
    lastLoggedMinute = now.minute();
    alreadyLoggedThisMinute = false;
  }

  // Step 1: Check WiFi connection
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

  // Step 2: Check MQTT connection
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
    recoveredSent = false; // Allow recovery again
  }

  // Step 3: After successful MQTT reconnect → send old CSVs
  if (!recoveredSent && mqttClient.connected()) {
    if (sendPendingData(mqttClient, topic, sensorType, sensorIdInUse, now)) {
      recoveredSent = true;
    }
  }

  // Step 4: Normal measurement and MQTT transmission
  if (!alreadyLoggedThisMinute) {
    float c = readTemperatureCelsius();
    sendToMqtt(mqttClient, topic, sensorType, sensorIdInUse, c, now, count);
    alreadyLoggedThisMinute = true;
    count++;
  }

  // Step 5: MQTT loop and wait time
  mqttClient.poll();
  delay(LOOP_DELAY_MS);
}
