#pragma once

#include "platform.h"

extern MqttClient mqttClient;

bool SendTempToMqtt(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                const char* sensorId, float celsius, const DateTime& now, int sequence);

bool SendPendingDataToMqtt(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                     const char* sensorId, const DateTime& now);
                     
void CreateFullTopic(char* buffer, size_t bufferSize, const char* topicPrefix, const char* sensorType,
                     const char* sensorId, const char* suffix = "");