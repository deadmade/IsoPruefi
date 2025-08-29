#pragma once

void CoreSetup();
void CoreLoop();
bool IsWifiConnected();
bool IsMqttConnected();
void FatDateTime(uint16_t* date, uint16_t* time);