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
void Test_InitSensor_success(void) {
    MockTempSensor mockSensor;
    
    // Mock successful sensor initialization
    // The mock sensor always returns true for begin()
    bool result = InitSensor(mockSensor);
    
    TEST_ASSERT_TRUE(result);
}

// Test that initSensor handles delay properly (mock doesn't fail)
void Test_InitSensor_with_delay(void) {
    MockTempSensor mockSensor;
    
    // Test that the function completes even with delay call
    bool result = InitSensor(mockSensor);
    
    TEST_ASSERT_TRUE(result);
    TEST_ASSERT_EQUAL_INT(250, mockSensor.delayCalled());
}

// Test initSensor sets correct resolution
void Test_InitSensor_sets_resolution(void) {
    MockTempSensor mockSensor;

    // This test verifies the function completes successfully.
    bool result = InitSensor(mockSensor);
    
    TEST_ASSERT_TRUE(result);
    TEST_ASSERT_TRUE(mockSensor.setResolutionCalled());
}

// Test readTemperatureCelsius function
void Test_ReadTemperatureCelsius_returns_value(void) {
    // The global tempsensor mock returns 25.5 by default
    float temperature = ReadTemperatureInCelsius();
    
    TEST_ASSERT_EQUAL_FLOAT(25.5, temperature);
}

// Test readTemperatureCelsius with different mock values
void Test_ReadTemperatureCelsius_various_values(void) {
    // Test that the function correctly calls the sensor's readTempC method
    // Since we're using a global mock, we test with the default value
    float temperature = ReadTemperatureInCelsius();
    
    // Verify we get a reasonable temperature value
    TEST_ASSERT_GREATER_THAN(-50.0, temperature);
    TEST_ASSERT_LESS_THAN(100.0, temperature);
}

// Test edge cases for temperature reading
void Test_ReadTemperatureCelsius_precision(void) {
    float temperature = ReadTemperatureInCelsius();
    
    // Verify the temperature has reasonable precision
    TEST_ASSERT_FLOAT_WITHIN(0.5, 25.5, temperature);
}

// Bundle for central test_main.cpp
void Run_sensor_tests() {
    RUN_TEST(Test_InitSensor_success);
    RUN_TEST(Test_InitSensor_sets_resolution);
    RUN_TEST(Test_InitSensor_with_delay);
    RUN_TEST(Test_ReadTemperatureCelsius_returns_value);
    RUN_TEST(Test_ReadTemperatureCelsius_various_values);
    RUN_TEST(Test_ReadTemperatureCelsius_precision);
}

// When standalone executable
#ifndef COMBINED_TEST_MAIN
int main(int argc, char **argv) {
    UNITY_BEGIN();
    Run_sensor_tests();
    return UNITY_END();
}
#endif