#pragma once

#include "platform.h"

void sendToMqtt(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                const char* sensorId, float celsius, const DateTime& now, int sequence);

bool sendPendingData(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                     const char* sensorId, const DateTime& now);

void createFullTopic(char* buffer, size_t bufferSize, const char* topicPrefix, const char* sensorType,
                     const char* sensorId, const char* suffix = nullptr);
