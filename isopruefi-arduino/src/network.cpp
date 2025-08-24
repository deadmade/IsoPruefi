#include "network.h"

#ifdef UNIT_TEST
#include "secrets_example.h"
#else
#include "secrets.h"
#endif

#include "mqtt.h"

static const char SSID[]     = SECRET_SSID;
static const char PASSWORD[] = SECRET_PASS;
static const char* BROKER    = "aicon.dhbw-heidenheim.de";
static const int port        = 1883;

/**
 * @brief Establishes a WiFi connection with the configured network.
 *
 * Attempts to connect to the WiFi network using credentials from the secrets file.
 * Provides visual feedback via serial output and enforces a connection timeout.
 *
 * @param timeoutMs Maximum time in milliseconds to wait for connection (default: 10000ms).
 * @return true if WiFi connection is successful, false if timeout occurs.
 */
bool ConnectToWiFi(unsigned long timeoutMs) {
  Serial.print("Connecting to WiFi...");
  WiFi.begin(SSID, PASSWORD);

  unsigned long startAttemptTime = millis();
  while (WiFi.status() != WL_CONNECTED) {
    if (millis() - startAttemptTime >= timeoutMs) {
      Serial.println("WiFi connection timed out.");
      return false;
    }
    delay(500);
    Serial.print(".");
  }

  Serial.println("WiFi is connected.");
  return true;
}

/**
 * @brief Establishes an authenticated MQTT connection to the broker.
 *
 * Sets up MQTT client credentials using values from the secrets file and attempts
 * to connect to the configured MQTT broker. Provides visual feedback and enforces
 * a connection timeout.
 *
 * @param mqttClient Reference to the MQTT client instance to connect.
 * @param timeoutMs Maximum time in milliseconds to wait for connection (default: 10000ms).
 * @return true if MQTT connection is successful, false if timeout occurs.
 */
bool ConnectToMQTT(MqttClient& mqttClient, unsigned long timeoutMs) {
  Serial.print("Connecting to MQTT...");
  
  mqttClient.setUsernamePassword(SECRET_MQTT_USER, SECRET_MQTT_PASS);
  
  unsigned long startAttemptTime = millis();

  while (!mqttClient.connect(BROKER, port)) {
    if (millis() - startAttemptTime >= timeoutMs) {
      Serial.println("MQTT connection timed out.");
      return false;
    }
    Serial.print(".");
    delay(1000);
  }

  Serial.println(" connected.");
  return true;
}
