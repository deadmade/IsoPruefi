#pragma once

#include <ArduinoMqttClient.h>

// Initialisiert und verbindet mit WiFi (gibt true zurück bei Erfolg)
bool connectWiFi(unsigned long timeoutMs = 10000);

// Initialisiert und verbindet mit dem MQTT-Broker (gibt true zurück bei Erfolg)
bool connectMQTT(MqttClient& mqttClient, unsigned long timeoutMs = 10000);
