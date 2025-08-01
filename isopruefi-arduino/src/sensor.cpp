#include "sensor.h"

/**
 * @brief Initializes the ADT7410 temperature sensor.
 *
 * Attempts to initialize the temperature sensor, sets it to 16-bit resolution
 * for maximum precision, and includes a stabilization delay.
 *
 * @param sensor Reference to the ADT7410 sensor instance to initialize.
 * @return true if initialization is successful, false if sensor is not found.
 */
bool initSensor(Adafruit_ADT7410& sensor) {
  if (!sensor.begin()) {
    Serial.println("ADT7410 not found!");
    return false;
  }
  delay(250);
  sensor.setResolution(ADT7410_16BIT);
  return true;
}

float readTemperatureCelsius() {
  return tempsensor.readTempC();  
}
