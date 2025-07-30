#pragma once

#include "platform.h"

void buildJson(JsonDocument& doc, float celsius, const DateTime& now, int sequence);

void sendToMqtt(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                const char* sensorId, float celsius, const DateTime& now, int sequence);

void buildRecoveredJson(JsonDocument& doc, const String* fileList, int count, const DateTime& now);

void sendPendingData(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                const char* sensorId, const DateTime& now);

