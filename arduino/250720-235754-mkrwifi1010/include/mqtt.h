#pragma once

#include "platform.h"

void buildJson(JsonDocument& doc, float celsius, const DateTime& now, int sequence);

void sendToMqtt(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                const char* sensorId, float celsius, const DateTime& now, int sequence);

void sendPendingData(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                const char* sensorId);

