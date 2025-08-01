#include "network.h"
#include "secrets.h"
#include "mqtt.h"

static const char ssid[]     = SECRET_SSID;
static const char password[] = SECRET_PASS;
static const char* broker    = "aicon.dhbw-heidenheim.de";
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
bool connectWiFi(unsigned long timeoutMs) {
  Serial.print("Connecting to WiFi...");
  WiFi.begin(ssid, password);

  unsigned long startAttemptTime = millis();
  while (WiFi.status() != WL_CONNECTED) {
    if (millis() - startAttemptTime >= timeoutMs) {
      Serial.println("\nWiFi connection timed out.");
      return false;
    }
    delay(500);
    Serial.print(".");
  }

  Serial.println("\nWiFi connected.");
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
bool connectMQTT(MqttClient& mqttClient, unsigned long timeoutMs) {
  Serial.print("Connecting to MQTT...");
  
  mqttClient.setUsernamePassword(SECRET_MQTT_USER, SECRET_MQTT_PASS);
  
  unsigned long startAttemptTime = millis();

  while (!mqttClient.connect(broker, port)) {
    if (millis() - startAttemptTime >= timeoutMs) {
      Serial.println("\nMQTT connection timed out.");
      return false;
    }
    Serial.print(".");
    delay(1000);
  }

  Serial.println(" connected.");
  return true;
}

/**
 * @brief Checks if both WiFi and MQTT connections are active.
 *
 * Verifies the status of both WiFi and MQTT connections to ensure the device
 * can communicate with the MQTT broker.
 *
 * @param mqttClient Reference to the MQTT client instance to check.
 * @return true if both WiFi and MQTT are connected, false otherwise.
 */
bool isConnectedToServer(MqttClient& mqttClient) {
  return WiFi.status() == WL_CONNECTED && mqttClient.connected();
}
