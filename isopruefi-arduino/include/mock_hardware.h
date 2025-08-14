#pragma once
#include "mock_datetime.h"
#include <map>
#include <string>
#include <cstring>

class MockRTC_DS3231 {
private:
    static bool _beginResult;
    static bool _lostPower;
    static MockDateTime _currentTime;

public:
    bool begin() { return _beginResult; }
    bool lostPower() { return _lostPower; }
    
    MockDateTime now() { return _currentTime; }
    
    void adjust(const MockDateTime& dt) { _currentTime = dt; }
    
    // Test helpers
    static void setBeginResult(bool result) { _beginResult = result; }
    static void setLostPower(bool lost) { _lostPower = lost; }
    static void setCurrentTime(const MockDateTime& time) { _currentTime = time; }
    static void reset() {
        _beginResult = true;
        _lostPower = false;
        _currentTime = MockDateTime(2025, 7, 26, 14, 55, 0);
    }
};

// Adafruit temperature sensor mock
class MockAdafruit_ADT7410 {
private:
    static bool _beginResult;
    static float _temperature;

public:
    bool begin() { return _beginResult; }
    void setResolution(int resolution) {}
    float readTempC() { return _temperature; }
    
    // Test helpers
    static void setBeginResult(bool result) { _beginResult = result; }
    static void setTemperature(float temp) { _temperature = temp; }
    static void reset() {
        _beginResult = true;
        _temperature = 23.5f;
    }
};

// SD card mock
class MockFile {
private:
    std::string _content;
    size_t _position;
    bool _isDirectory;
    std::string _name;

public:
    MockFile() : _position(0), _isDirectory(false) {}
    MockFile(const std::string& content, const std::string& name, bool isDir = false) 
        : _content(content), _position(0), _isDirectory(isDir), _name(name) {}
    
    bool isDirectory() { return _isDirectory; }
    void getName(char* buffer, size_t bufferSize) {
        strncpy(buffer, _name.c_str(), bufferSize - 1);
        buffer[bufferSize - 1] = '\0';
    }
    
    int fgets(char* buffer, int max_len) {
        if (_position >= _content.length()) return 0;
        
        size_t lineEnd = _content.find('\n', _position);
        if (lineEnd == std::string::npos) lineEnd = _content.length();
        
        size_t lineLen = std::min((size_t)(max_len - 1), lineEnd - _position);
        strncpy(buffer, _content.c_str() + _position, lineLen);
        buffer[lineLen] = '\0';
        
        _position = lineEnd + 1;
        return lineLen;
    }
    
    void close() {}
};

class MockSdFat {
private:
    static bool _beginResult;
    static std::map<std::string, std::string> _files;
    static std::map<std::string, std::vector<std::string>> _directories;

public:
    bool begin(int chipSelect, int clockSpeed = 0) { return _beginResult; }
    
    MockFile open(const char* path, int mode = 0) {
        std::string pathStr(path);
        if (_files.count(pathStr)) {
            return MockFile(_files[pathStr], pathStr);
        }
        if (_directories.count(pathStr)) {
            return MockFile("", pathStr, true);
        }
        return MockFile();
    }
    
    MockFile openNextFile() {
        // Simplified implementation for testing
        return MockFile();
    }
    
    // Test helpers
    static void setBeginResult(bool result) { _beginResult = result; }
    static void addFile(const std::string& path, const std::string& content) {
        _files[path] = content;
    }
    static void addDirectory(const std::string& path, const std::vector<std::string>& files) {
        _directories[path] = files;
    }
    static void reset() {
        _beginResult = true;
        _files.clear();
        _directories.clear();
    }
};

// Constants for compatibility
#define ADT7410_16BIT 3
#define FILE_READ 1

// FAT timestamp macros
#define FAT_DATE(y,m,d) ((((y)-1980)<<9)|((m)<<5)|(d))
#define FAT_TIME(h,m,s) (((h)<<11)|((m)<<5)|((s)>>1))

#ifdef UNIT_TEST
using RTC_DS3231 = MockRTC_DS3231;
using Adafruit_ADT7410 = MockAdafruit_ADT7410;
using SdFat = MockSdFat;
using File = MockFile;

// Mock external objects
extern MockRTC_DS3231 rtc;
extern MockAdafruit_ADT7410 tempsensor;
extern MockSdFat sd;
#endif