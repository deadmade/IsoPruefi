#pragma once

#include "platform.h"

// Stellt Verbindung zum WLAN her (true bei Erfolg)
bool connectWiFi(unsigned long timeoutMs = 10000);

// Verbindet mit MQTT-Broker (true bei Erfolg)
bool connectMQTT(MqttClient& mqttClient, unsigned long timeoutMs = 10000);

// Optional: Interface-Funktion für Hauptprogramm mit globalem mqttClient
bool connectMqttClient();  // nutzt mqttClient aus platform.h
