#pragma once

void coreSetup();
void coreLoop();
bool isWifiConnected();
bool isMqttConnected();
void dateTime(uint16_t* date, uint16_t* time);
