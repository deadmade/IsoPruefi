#include "platform.h"
#include "core.h"

void setup() {
  Serial.begin(9600);
  unsigned long startTime = millis();
  while (!Serial && (millis() - startTime < 1000));
  coreSetup();
}

void loop() {
  coreLoop();
}