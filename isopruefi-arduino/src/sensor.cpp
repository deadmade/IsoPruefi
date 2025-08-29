#include "sensor.h"

bool InitSensor(Adafruit_ADT7410& sensor) {
  if (!sensor.begin()) {
    Serial.println("ADT7410 not found!");
    return false;
  }
  delay(250);
  sensor.setResolution(ADT7410_16BIT);
  return true;
}

float ReadTemperatureInCelsius() {
  return tempsensor.readTempC();  
}
