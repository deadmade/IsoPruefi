#include "platform.h"
#include <unity.h>
#include "sensor.h"

// Test sensor initialization success
void test_initSensor_success(void) {
    MockAdafruit_ADT7410::setBeginResult(true);
    MockAdafruit_ADT7410 testSensor;
    
    bool result = initSensor(testSensor);
    
    TEST_ASSERT_TRUE(result);
}

// Test sensor initialization failure
void test_initSensor_failure(void) {
    MockAdafruit_ADT7410::setBeginResult(false);
    MockAdafruit_ADT7410 testSensor;
    
    bool result = initSensor(testSensor);
    
    TEST_ASSERT_FALSE(result);
}

// Test temperature reading normal values
void test_readTemperatureCelsius_normal_values(void) {
    MockAdafruit_ADT7410::setTemperature(23.5f);
    
    float temperature = readTemperatureCelsius();
    
    TEST_ASSERT_EQUAL_FLOAT(23.5f, temperature);
}

// Test temperature reading zero
void test_readTemperatureCelsius_zero(void) {
    MockAdafruit_ADT7410::setTemperature(0.0f);
    
    float temperature = readTemperatureCelsius();
    
    TEST_ASSERT_EQUAL_FLOAT(0.0f, temperature);
}

// Test temperature reading negative values
void test_readTemperatureCelsius_negative(void) {
    MockAdafruit_ADT7410::setTemperature(-15.2f);
    
    float temperature = readTemperatureCelsius();
    
    TEST_ASSERT_EQUAL_FLOAT(-15.2f, temperature);
}

// Test temperature reading high values
void test_readTemperatureCelsius_high_values(void) {
    MockAdafruit_ADT7410::setTemperature(85.7f);
    
    float temperature = readTemperatureCelsius();
    
    TEST_ASSERT_EQUAL_FLOAT(85.7f, temperature);
}

void setUp(void) {
    // Reset sensor mock before each test
    MockAdafruit_ADT7410::reset();
}

void tearDown(void) {
    // Clean up after each test
}

// Bundle for central test runner
void run_sensor_tests() {
    RUN_TEST(test_initSensor_success);
    RUN_TEST(test_initSensor_failure);
    RUN_TEST(test_readTemperatureCelsius_normal_values);
    RUN_TEST(test_readTemperatureCelsius_zero);
    RUN_TEST(test_readTemperatureCelsius_negative);
    RUN_TEST(test_readTemperatureCelsius_high_values);
}

// When standalone executable
#ifndef COMBINED_TEST_MAIN
int main(int argc, char **argv) {
    UNITY_BEGIN();
    run_sensor_tests();
    return UNITY_END();
}
#endif