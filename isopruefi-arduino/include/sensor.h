#pragma once

#include "platform.h"

bool initSensor(Adafruit_ADT7410& sensor);

float readTemperatureCelsius();

String formatTimestamp(const DateTime& now);


