#pragma once
#include <string>

class MockMqttClient {
private:
    static bool _connected;
    static bool _beginMessageResult;
    static bool _endMessageResult;
    static std::string _lastTopic;
    static std::string _lastPayload;
    static std::string _messageTopic;
    static bool _messageRetain;
    static int _available;
    static std::string _readBuffer;
    static size_t _readPos;

public:
    MockMqttClient() = default;
    MockMqttClient(void* client) {} // Constructor that takes WiFiClient
    
    // Connection management
    bool connected() { return _connected; }
    static void setConnected(bool connected) { _connected = connected; }
    
    // Message publishing
    bool beginMessage(const char* topic, bool retain = false, int qos = 0) {
        _lastTopic = topic;
        return _beginMessageResult;
    }
    
    bool endMessage() { return _endMessageResult; }
    
    void print(const char* payload) { _lastPayload = payload; }
    
    static void setPublishResults(bool beginResult, bool endResult) {
        _beginMessageResult = beginResult;
        _endMessageResult = endResult;
    }
    
    // Message receiving (for echo/ack testing)
    void onMessage(void (*callback)(int)) {}
    
    std::string messageTopic() { return _messageTopic; }
    bool messageRetain() { return _messageRetain; }
    
    int available() { return _available; }
    char read() {
        if (_readPos < _readBuffer.length()) {
            return _readBuffer[_readPos++];
        }
        return 0;
    }
    
    static void setMessageData(const std::string& topic, bool retain, const std::string& data) {
        _messageTopic = topic;
        _messageRetain = retain;
        _readBuffer = data;
        _readPos = 0;
        _available = data.length();
    }
    
    // Client configuration
    void setId(const char* id) {}
    bool subscribe(const char* topic) { return true; }
    void poll() {}
    
    // Test helpers
    static std::string getLastTopic() { return _lastTopic; }
    static std::string getLastPayload() { return _lastPayload; }
    static void reset() {
        _connected = false;
        _beginMessageResult = true;
        _endMessageResult = true;
        _lastTopic.clear();
        _lastPayload.clear();
        _messageTopic.clear();
        _messageRetain = false;
        _available = 0;
        _readBuffer.clear();
        _readPos = 0;
    }
};

#ifdef UNIT_TEST
using MqttClient = MockMqttClient;
#endif