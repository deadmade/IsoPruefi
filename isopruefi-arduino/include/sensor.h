#pragma once

#include "platform.h"

bool InitSensor(Adafruit_ADT7410& sensor);
float ReadTemperatureInCelsius();