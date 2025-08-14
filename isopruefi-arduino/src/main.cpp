#include "platform.h"
#include "core.h"

#ifndef UNIT_TEST

void setup() {
  Serial.begin(9600);
  unsigned long startTime = millis();
  while (!Serial && (millis() - startTime < 3000));
  coreSetup();
}

void loop() {
  coreLoop();
}
#endif