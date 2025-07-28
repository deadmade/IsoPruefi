#include "network.h"
#include "secrets.h"
#include "mqtt.h"

static const char ssid[]     = SECRET_SSID;
static const char password[] = SECRET_PASS;
static const char* broker    = "aicon.dhbw-heidenheim.de";
static const int port        = 1883;

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

bool connectMQTT(MqttClient& mqttClient, unsigned long timeoutMs) {
  Serial.print("Connecting to MQTT...");
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

static unsigned long lastReconnectAttempt = 0;
static unsigned long reconnectInterval = 10000; // Initial 10 Secunden

void tryReconnect(MqttClient& mqttClient) {
  unsigned long now = millis();

  if (now - lastReconnectAttempt >= reconnectInterval) {
    lastReconnectAttempt = now;

    if (WiFi.status() != WL_CONNECTED) {
      Serial.println("WiFi not connected. Trying to reconnect...");
      connectWiFi(3000);
    }

    if (!mqttClient.connected() && WiFi.status() == WL_CONNECTED) {
      Serial.println("MQTT not connected. Trying to reconnect...");
      if (connectMQTT(mqttClient, 3000)) {
        reconnectInterval = 10000; 
      } else {
        reconnectInterval = min(reconnectInterval + 10000, 300000UL); // Max 5 Min
      }
    }
  }
}

bool isConnectedToServer(MqttClient& mqttClient) {
  return WiFi.status() == WL_CONNECTED && mqttClient.connected();
}
