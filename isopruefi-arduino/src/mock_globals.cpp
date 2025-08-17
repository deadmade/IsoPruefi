#ifdef UNIT_TEST
#include "platform.h"

// Global mock objects for testing
MockSdFat sd;
MockRTC rtc;
MockTempSensor tempsensor;
MockWiFiClass WiFi;
MockWiFiClient wifiClient;
MockMqttClient mqttClient(wifiClient);

#endif