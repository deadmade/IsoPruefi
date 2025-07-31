#pragma once

#include "platform.h"

bool connectWiFi(unsigned long timeoutMs = 10000);

bool connectMQTT(MqttClient& mqttClient, unsigned long timeoutMs = 10000);


void tryReconnect(MqttClient& mqttClient);

bool isConnectedToServer(MqttClient& mqttClient);