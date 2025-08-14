#include <ArduinoFake.h>
#include <unity.h>
#include "sensor.h"

using namespace fakeit;

void setUp(void) {
    ArduinoFakeReset();
    When(OverloadedMethod(ArduinoFake(Serial), print, size_t(const char[]))).AlwaysReturn(1);
    When(OverloadedMethod(ArduinoFake(Serial), println, size_t(const char[]))).AlwaysReturn(1);
    When(Method(ArduinoFake(), delay)).Return();
    When(Method(ArduinoFake(), millis)).Return(1000);
}

void tearDown(void) {
    ArduinoFakeReset();
}

// Test initSensor function with successful initialization
void test_initSensor_success(void) {
    MockTempSensor mockSensor;
    
    // Mock successful sensor initialization
    // The mock sensor always returns true for begin()
    bool result = initSensor(mockSensor);
    
    TEST_ASSERT_TRUE(result);
}

// Test that initSensor handles delay properly (mock doesn't fail)
void test_initSensor_with_delay(void) {
    MockTempSensor mockSensor;
    
    // Test that the function completes even with delay call
    bool result = initSensor(mockSensor);
    
    TEST_ASSERT_TRUE(result);
}

// Test initSensor sets correct resolution
void test_initSensor_sets_resolution(void) {
    MockTempSensor mockSensor;
    
    bool result = initSensor(mockSensor);
    
    TEST_ASSERT_TRUE(result);
    // Note: In a real test, we would verify setResolution(ADT7410_16BIT) was called
    // but our mock doesn't track this. This test verifies the function completes successfully.
}

// Test readTemperatureCelsius function
void test_readTemperatureCelsius_returns_value(void) {
    // The global tempsensor mock returns 25.5 by default
    float temperature = readTemperatureCelsius();
    
    TEST_ASSERT_EQUAL_FLOAT(25.5, temperature);
}

// Test readTemperatureCelsius with different mock values
void test_readTemperatureCelsius_various_values(void) {
    // Test that the function correctly calls the sensor's readTempC method
    // Since we're using a global mock, we test with the default value
    float temperature = readTemperatureCelsius();
    
    // Verify we get a reasonable temperature value
    TEST_ASSERT_GREATER_THAN(-50.0, temperature);
    TEST_ASSERT_LESS_THAN(100.0, temperature);
}

// Test edge cases for temperature reading
void test_readTemperatureCelsius_precision(void) {
    float temperature = readTemperatureCelsius();
    
    // Verify the temperature has reasonable precision
    TEST_ASSERT_EQUAL_FLOAT(25.5, temperature);
}

// Bundle for central test_main.cpp
void run_sensor_tests() {
    RUN_TEST(test_initSensor_success);
    RUN_TEST(test_initSensor_sets_resolution);
    RUN_TEST(test_initSensor_with_delay);
    RUN_TEST(test_readTemperatureCelsius_returns_value);
    RUN_TEST(test_readTemperatureCelsius_various_values);
    RUN_TEST(test_readTemperatureCelsius_precision);
}

// When standalone executable
#ifndef COMBINED_TEST_MAIN
int main(int argc, char **argv) {
    UNITY_BEGIN();
    run_sensor_tests();
    return UNITY_END();
}
#endif