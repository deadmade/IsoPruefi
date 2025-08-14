#pragma once
#include <cstdint>

// WiFi status constants
#define WL_CONNECTED 3
#define WL_DISCONNECTED 6
#define WL_CONNECTION_LOST 5

class MockWiFiClass {
private:
    static int _status;
    static bool _connectResult;

public:
    static int status() { return _status; }
    static void setStatus(int status) { _status = status; }
    static void setConnectResult(bool result) { _connectResult = result; }
    
    static int begin(const char* ssid, const char* pass) {
        return _connectResult ? WL_CONNECTED : WL_DISCONNECTED;
    }
};

class MockWiFiClient {
public:
    bool connected() { return true; }
};

#ifdef UNIT_TEST
extern MockWiFiClass WiFi;
extern MockWiFiClient wifiClient;
#endif