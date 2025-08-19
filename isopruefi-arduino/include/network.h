#pragma once

#include "platform.h"

bool ConnectToWiFi(unsigned long timeoutMs = 10000);
bool ConnectToMQTT(MqttClient& mqttClient, unsigned long timeoutMs = 10000);

inline bool IsConnectedToServer(MqttClient& mqttClient) {
  return WiFi.status() == WL_CONNECTED && mqttClient.connected();
}