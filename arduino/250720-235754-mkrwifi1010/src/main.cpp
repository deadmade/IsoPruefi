#include <Arduino.h>
#include "core.h"

void setup() {
  Serial.begin(9600);
  while (!Serial);
  coreSetup();
}

void loop() {
  coreLoop();
}