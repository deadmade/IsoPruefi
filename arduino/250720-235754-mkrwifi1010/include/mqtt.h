#pragma once

#include "platform.h"

void buildJson(JsonDocument& doc, float celsius, const DateTime& now, int sequence);

// Sendet Temperaturdaten als JSON per MQTT
void sendToMqtt(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                const char* sensorId, float celsius, const DateTime& now, int sequence);
