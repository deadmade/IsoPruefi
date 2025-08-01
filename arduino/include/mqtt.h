#pragma once

#include "platform.h"

void sendToMqtt(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                const char* sensorId, float celsius, const DateTime& now, int sequence);

void sendPendingData(MqttClient& mqttClient, const char* topicPrefix, const char* sensorType,
                const char* sensorId, const DateTime& now);

