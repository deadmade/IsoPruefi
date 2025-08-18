#pragma once

#ifdef UNIT_TEST
  #include <ArduinoFake.h>
  #include <string>
  #include <set>
  #include <cstdint>
  #include <cstdio>
  #include <cstdlib>
  #include <cstring>
  #include <cstdarg>
  #include <map>
  #include <ArduinoJson.h>

  // ArduinoFake provides: Arduino.h, String, Serial, etc.
  // Only mock what ArduinoFake doesn't provide
  
  // Mock DateTime class for RTClib
  class DateTime {
    public:
      DateTime(int y, int m, int d, int h, int min, int s) 
        : _year(y), _month(m), _day(d), _hour(h), _minute(min), _second(s) {}
      DateTime(const char* date, const char* time) 
        : _year(2025), _month(7), _day(26), _hour(14), _minute(55), _second(0) {} // Mock parsing
      DateTime(const __FlashStringHelper* date, const __FlashStringHelper* time) 
        : _year(2025), _month(7), _day(26), _hour(14), _minute(55), _second(0) {} // Mock F() macro support
      int year() const { return _year; }
      int month() const { return _month; }
      int day() const { return _day; }
      int hour() const { return _hour; }
      int minute() const { return _minute; }
      int second() const { return _second; }
      uint32_t unixtime() const { 
        // Simple mock timestamp calculation
        return 1640995200 + (_year - 2022) * 31536000 + _month * 2628000 + _day * 86400 + _hour * 3600 + _minute * 60 + _second;
      }
      String timestamp(int format = 0) const {
        return String("2025-07-26T14:55:00");
      }
      
      // Constants for timestamp formats
      static const int TIMESTAMP_FULL = 0;
      
    private:
      int _year, _month, _day, _hour, _minute, _second;
  };
  
  // Mock File class for SdFat
  class MockFile {
    public:
      MockFile() : _isOpen(false), _data(""), _position(0) {}
      MockFile(bool isOpen) : _isOpen(isOpen), _data(""), _position(0) {}
      
      // Use std::string internally, convert ArduinoFake String when needed
      bool print(const char* str) { if(_isOpen) _data += str; return _isOpen; }
      bool print(const String& str) {
          if(_isOpen) _data += str.c_str();  // Convert ArduinoFake String to const char*
          return _isOpen;
      }
      void close() { _isOpen = false; }
      bool available() { return _isOpen && _position < _data.length(); }
      size_t fgets(char* buffer, size_t size) {
        if (!_isOpen || _position >= _data.length()) return 0;
        size_t i = 0;
        while (i < size - 1 && _position < _data.length() && _data[_position] != '\n') {
          buffer[i++] = _data[_position++];
        }
        if (_position < _data.length() && _data[_position] == '\n') {
          buffer[i++] = _data[_position++];
        }
        buffer[i] = '\0';
        return i;
      }
      operator bool() const { return _isOpen; }
      
      // Additional methods needed by the code
      MockFile openNextFile() { return MockFile(false); } // Mock iterator
      bool isDirectory() { return false; }
      void getName(char* buffer, size_t size) {
        strncpy(buffer, "test.csv", size);
        buffer[size-1] = '\0';
      }
      
      void setTestData(const std::string& data) { _data = data; _position = 0; }
      std::string getWrittenData() const { return _data; }
      
    private:
      bool _isOpen;
      std::string _data;
      size_t _position;
  };
  
  // Mock SdFat class
  class MockSdFat {
    public:
      bool exists(const char* path) { return _existingFiles.find(std::string(path)) != _existingFiles.end(); }
      bool mkdir(const char* path) { _existingFiles.insert(std::string(path)); return true; }
      MockFile open(const char* path, int mode) { 
        std::string pathStr(path);
        if (mode == 1) { // FILE_WRITE
          _existingFiles.insert(pathStr);
          return MockFile(true);
        }
        // FILE_READ
        bool exists = _existingFiles.find(pathStr) != _existingFiles.end();
        MockFile file(exists);
        if (exists && _fileContents.find(pathStr) != _fileContents.end()) {
          file.setTestData(_fileContents[pathStr]);
        }
        return file;
      }
      MockFile open(const char* path) { 
        // Default to read mode
        std::string pathStr(path);
        bool exists = _existingFiles.find(pathStr) != _existingFiles.end();
        MockFile file(exists);
        if (exists && _fileContents.find(pathStr) != _fileContents.end()) {
          file.setTestData(_fileContents[pathStr]);
        }
        return file;
      }
      bool remove(const char* path) { 
        std::string pathStr(path);
        auto it = _existingFiles.find(pathStr);
        if (it != _existingFiles.end()) {
          _existingFiles.erase(it);
          _fileContents.erase(pathStr);
          return true;
        }
        return false;
      }
      bool begin(int chipSelect, int freq) { return true; }
      
      void addTestFile(const std::string& path) { _existingFiles.insert(path); }
      void addTestFile(const std::string& path, const std::string& content) { 
        _existingFiles.insert(path); 
        _fileContents[path] = content;
      }
      void clearTestFiles() { 
        _existingFiles.clear(); 
        _fileContents.clear();
      }
      
    private:
      std::set<std::string> _existingFiles;
      std::map<std::string, std::string> _fileContents;
  };
  
  // Global mock objects
  extern MockSdFat sd;

  // Mock WiFi classes using ArduinoFake
  class MockWiFiClient {
    public:
      MockWiFiClient() : _connected(false) {}
      int connect(const char* host, uint16_t port) { _connected = true; return 1; }
      size_t write(uint8_t data) { return 1; }
      size_t write(const uint8_t* buffer, size_t size) { return size; }
      int available() { return 0; }
      int read() { return -1; }
      int read(uint8_t* buffer, size_t size) { return 0; }
      int peek() { return -1; }
      void flush() {}
      void stop() { _connected = false; }
      uint8_t connected() { return _connected; }
      operator bool() { return _connected; }
      
    private:
      bool _connected;
  };
  
  class MockWiFiClass {
    public:
      int begin(const char* ssid, const char* pass) { _status = 3; return 3; } // WL_CONNECTED = 3
      uint8_t status() { return _status; }
      void disconnect() { _status = 6; } // WL_DISCONNECTED = 6
      
    private:
      uint8_t _status = 6; // Start disconnected
  };  

    using WiFiClient = MockWiFiClient;
  
  // Mock MQTT Client - use ArduinoFake's Client class
  class MockMqttClient {
    public:
      MockMqttClient(WiFiClient& client) : _connected(false) {}
      
      void setId(const char* id) { _clientId = id; }
      void setUsernamePassword(const char* user, const char* pass) { _username = user; _password = pass; }
      int connect(const char* broker, int port = 1883) { _connected = true; return 1; }
      bool connected() { return _connected; }
      void stop() { _connected = false; }
      void poll() {}
      
      int beginMessage(const char* topic, bool retain = false, int qos = 0) { 
        _currentTopic = topic; 
        _messageBuffer = "";
        return 1; 
      }
      size_t print(const char* data) { _messageBuffer += data; return strlen(data); }
      size_t print(const String& data) { _messageBuffer += data.c_str(); return data.length(); }
      int endMessage() { return 1; }
      
      String messageTopic() { return String(_currentTopic.c_str()); }
      bool messageRetain() { return false; }
      int available() { return 0; }
      int read() { return -1; }
      
      void setMessageCallback(void (*callback)(int)) { _callback = callback; }
      void onMessage(void (*callback)(int)) { _callback = callback; }
      void subscribe(const char* topic) { _subscribedTopic = topic; }
      void unsubscribe(const char* topic) {}
      
      // Test helpers
      std::string getLastMessage() { return _messageBuffer; }
      void simulateMessage(const std::string& topic, const std::string& message) {
        _currentTopic = topic;
        _messageBuffer = message;
        if (_callback) _callback(message.length());
      }
      
    private:
      bool _connected;
      std::string _clientId, _username, _password;
      std::string _currentTopic, _subscribedTopic, _messageBuffer;
      void (*_callback)(int) = nullptr;
  };
  
  // WiFi status constants
  #define WL_CONNECTED 3
  #define WL_DISCONNECTED 6
  
  // Global WiFi object
  extern MockWiFiClass WiFi;
  
  // Mock hardware objects
  class MockRTC {
    public:
      DateTime now() { return DateTime(2025, 7, 26, 14, 55, 0); }
      bool begin() { return true; }
      bool lostPower() { return false; }
      void adjust(const DateTime& dt) {}
  };
  
  class MockTempSensor {
    public:
      float readTempC() { return 25.5; }
      bool begin() { return true; }
      void setResolution(int resolution) {} // Mock resolution setting
  };
  
  // Type aliases for Arduino library classes - remove Client conflict
  using RTC_DS3231 = MockRTC;
  using Adafruit_ADT7410 = MockTempSensor;
  using MqttClient = MockMqttClient;
  using File = MockFile;
  
  extern MockRTC rtc;
  extern MockTempSensor tempsensor;
  extern MockWiFiClient wifiClient;
  extern MockMqttClient mqttClient;
  
  // File operation constants
  #define FILE_READ 0
  #define FILE_WRITE 1
  
  // SdFat constants
  #define SD_SCK_MHZ(freq) freq
  #define SD_SCK_FREQUENCY_MHZ 50
  
  // ADT7410 constants for mock
  #define ADT7410_16BIT 3
  
  // FAT time/date macros
  #define FAT_DATE(y, m, d) ((((y) - 1980) << 9) | ((m) << 5) | (d))
  #define FAT_TIME(h, m, s) (((h) << 11) | ((m) << 5) | ((s) >> 1))
  
  // SdFile for callback
  class SdFile {
    public:
      static void dateTimeCallback(void (*callback)(uint16_t*, uint16_t*)) {}
  };

#else

  // --- ORIGINAL ARDUINO-INCLUDES ---
  #include <Arduino.h> 
  #include <Wire.h>
  #include <SdFat.h>
  #include <RTClib.h>
  #include <Adafruit_ADT7410.h>
  #include <ArduinoJson.h>
  #include <ArduinoMqttClient.h>

  // Global hardware objects (declaration)
  extern RTC_DS3231 rtc;
  extern SdFat sd;
  extern Adafruit_ADT7410 tempsensor;

  // Unified WiFi header depending on board
  #if defined(ARDUINO_SAMD_MKRWIFI1010) || defined(ARDUINO_SAMD_NANO_33_IOT) || defined(ARDUINO_AVR_UNO_WIFI_REV2)
    #include <WiFiNINA.h>
  #elif defined(ARDUINO_SAMD_MKR1000)
    #include <WiFi101.h>
  #elif defined(ARDUINO_ARCH_ESP8266)
    #include <ESP8266WiFi.h>
  #elif defined(ARDUINO_PORTENTA_H7_M7) || defined(ARDUINO_NICLA_VISION) || defined(ARDUINO_ARCH_ESP32) || defined(ARDUINO_GIGA) || defined(ARDUINO_OPTA)
    #include <WiFi.h>
  #elif defined(ARDUINO_PORTENTA_C33)
    #include <WiFiC3.h>
  #elif defined(ARDUINO_UNOR4_WIFI)
    #include <WiFiS3.h>
  #else
    #error "No supported WiFi driver found for this board"
  #endif

#endif