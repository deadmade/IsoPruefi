#pragma once
#include <iostream>
#include <string>
#include <cstdarg>
#include <cstdio>
#include <cstdlib>
#include <cstring>

// Forward declarations to avoid conflicts
namespace MockArduino {
    
// Arduino Serial mock
class MockSerial {
public:
    void println(const char* text) {
        std::cout << text << std::endl;
    }
    
    void println(const std::string& text) {
        std::cout << text << std::endl;
    }
    
    void print(const char* text) {
        std::cout << text;
    }
    
    void print(const std::string& text) {
        std::cout << text;
    }
    
    void print(int value) {
        std::cout << value;
    }
    
    void println(int value) {
        std::cout << value << std::endl;
    }
};

// Arduino timing functions
extern unsigned long mockMillisCounter;

inline unsigned long millis() {
    return mockMillisCounter += 10; // Increment by 10ms each call for testing
}

inline void delay(unsigned long ms) {
    // No-op in tests
}

inline void setMockMillis(unsigned long value) {
    mockMillisCounter = value;
}

} // namespace MockArduino

#ifdef UNIT_TEST
using namespace MockArduino;
extern MockArduino::MockSerial Serial;
#endif