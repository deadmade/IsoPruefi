#include "platform.h"

#ifdef UNIT_TEST

// Initialize all static variables for mocks
namespace MockArduino {
    unsigned long mockMillisCounter = 0;
    MockSerial Serial;
}

// WiFi mock static variables
int MockWiFiClass::_status = WL_DISCONNECTED;
bool MockWiFiClass::_connectResult = false;
MockWiFiClass WiFi;
MockWiFiClient wifiClient;

// MQTT mock static variables  
bool MockMqttClient::_connected = false;
bool MockMqttClient::_beginMessageResult = true;
bool MockMqttClient::_endMessageResult = true;
std::string MockMqttClient::_lastTopic = "";
std::string MockMqttClient::_lastPayload = "";
std::string MockMqttClient::_messageTopic = "";
bool MockMqttClient::_messageRetain = false;
int MockMqttClient::_available = 0;
std::string MockMqttClient::_readBuffer = "";
size_t MockMqttClient::_readPos = 0;

// Hardware mock static variables
bool MockRTC_DS3231::_beginResult = true;
bool MockRTC_DS3231::_lostPower = false;
MockDateTime MockRTC_DS3231::_currentTime = MockDateTime(2025, 7, 26, 14, 55, 0);
MockRTC_DS3231 rtc;

bool MockAdafruit_ADT7410::_beginResult = true;
float MockAdafruit_ADT7410::_temperature = 23.5f;
MockAdafruit_ADT7410 tempsensor;

bool MockSdFat::_beginResult = true;
std::map<std::string, std::string> MockSdFat::_files;
std::map<std::string, std::vector<std::string>> MockSdFat::_directories;
MockSdFat sd;

#endif