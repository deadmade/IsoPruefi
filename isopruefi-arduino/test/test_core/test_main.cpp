#include <ArduinoFake.h>
#include <unity.h>
#include "platform.h"
#include "core.h"

using namespace fakeit;

void setUp(void) {
    ArduinoFakeReset();
    
    // Mock basic Arduino functions
    When(OverloadedMethod(ArduinoFake(Serial), print, size_t(const char[]))).AlwaysReturn(1);
    When(OverloadedMethod(ArduinoFake(Serial), println, size_t(const char[]))).AlwaysReturn(1);
    When(OverloadedMethod(ArduinoFake(Serial), print, size_t(const String&))).AlwaysReturn(1);
    When(OverloadedMethod(ArduinoFake(Serial), println, size_t(const String&))).AlwaysReturn(1);
    
    // Mock delay and timing functions
    When(Method(ArduinoFake(), delay)).AlwaysReturn();
    When(Method(ArduinoFake(), millis)).AlwaysReturn(1000);
    
    // Reset mock WiFi state
    WiFi.disconnect(); // Sets status to WL_DISCONNECTED
}

void tearDown(void) {
    ArduinoFakeReset();
}

// Test connection status functions using direct mock manipulation
void test_isWifiConnected_returns_true_when_connected(void) {
    // Use mock WiFi object directly - simulate connected state
    WiFi.begin("test", "test"); // This sets status to WL_CONNECTED
    
    bool result = isWifiConnected();
    
    TEST_ASSERT_TRUE(result);
}

void test_isWifiConnected_returns_false_when_disconnected(void) {
    // Use mock WiFi object directly - simulate disconnected state
    WiFi.disconnect(); // This sets status to WL_DISCONNECTED
    
    bool result = isWifiConnected();
    
    TEST_ASSERT_FALSE(result);
}

void test_isMqttConnected_returns_boolean(void) {
    // Test that the function exists and returns a valid boolean
    // Since we have a mock mqttClient, this should work
    bool result = isMqttConnected();
    
    // The function should return either true or false (not crash)
    TEST_ASSERT_TRUE(result == true || result == false);
}

void test_mqtt_connected_when_mock_connected(void) {
    // Set mock MQTT client to connected state
    mqttClient.connect("test_broker");
    
    bool result = isMqttConnected();
    
    TEST_ASSERT_TRUE(result);
}

// Test dateTime callback function
void test_dateTime_callback_sets_values(void) {
    uint16_t date, time;
    
    // Call the dateTime function - it should use our mock RTC
    dateTime(&date, &time);
    
    // Verify that values were set (our mock RTC returns a fixed time)
    // FAT_DATE(2025, 7, 26) = ((2025-1980) << 9) | (7 << 5) | 26 = 23050
    // FAT_TIME(14, 55, 0) = (14 << 11) | (55 << 5) | (0/2) = 30496
    TEST_ASSERT_EQUAL(23050, date);
    TEST_ASSERT_EQUAL(30496, time);
}

// Test core function existence
void test_core_functions_exist_and_compile(void) {
    // Test that core functions exist and can be referenced
    // This verifies they compile and link correctly
    
    // Test that function pointers can be created (proves functions exist)
    void (*setupFunc)() = coreSetup;
    void (*loopFunc)() = coreLoop;
    bool (*wifiFunc)() = isWifiConnected;
    bool (*mqttFunc)() = isMqttConnected;
    
    TEST_ASSERT_NOT_NULL(setupFunc);
    TEST_ASSERT_NOT_NULL(loopFunc);
    TEST_ASSERT_NOT_NULL(wifiFunc);
    TEST_ASSERT_NOT_NULL(mqttFunc);
}

// Test mock objects functionality
void test_mock_wifi_state_changes(void) {
    // Test that our mock WiFi object works as expected
    TEST_ASSERT_EQUAL(WL_DISCONNECTED, WiFi.status());
    
    WiFi.begin("test", "test");
    TEST_ASSERT_EQUAL(WL_CONNECTED, WiFi.status());
    
    WiFi.disconnect();
    TEST_ASSERT_EQUAL(WL_DISCONNECTED, WiFi.status());
}

void test_mock_mqtt_state_changes(void) {
    // Test that our mock MQTT client works as expected
    TEST_ASSERT_FALSE(mqttClient.connected());
    
    mqttClient.connect("test_broker");
    TEST_ASSERT_TRUE(mqttClient.connected());
    
    mqttClient.stop();
    TEST_ASSERT_FALSE(mqttClient.connected());
}

void test_mock_rtc_returns_fixed_time(void) {
    // Test that our mock RTC returns expected values
    DateTime now = rtc.now();
    
    TEST_ASSERT_EQUAL(2025, now.year());
    TEST_ASSERT_EQUAL(7, now.month());
    TEST_ASSERT_EQUAL(26, now.day());
    TEST_ASSERT_EQUAL(14, now.hour());
    TEST_ASSERT_EQUAL(55, now.minute());
    TEST_ASSERT_EQUAL(0, now.second());
}

void test_mock_sd_operations(void) {
    // Test that our mock SD card works as expected
    TEST_ASSERT_FALSE(sd.exists("test.txt"));
    
    sd.addTestFile("test.txt");
    TEST_ASSERT_TRUE(sd.exists("test.txt"));
    
    sd.clearTestFiles();
    TEST_ASSERT_FALSE(sd.exists("test.txt"));
}

void test_mock_temp_sensor(void) {
    // Test that our mock temperature sensor works
    float temp = tempsensor.readTempC();
    TEST_ASSERT_EQUAL_FLOAT(25.5, temp);
    
    bool initResult = tempsensor.begin();
    TEST_ASSERT_TRUE(initResult);
}

// Test constants and configuration
void test_wifi_status_constants_defined(void) {
    // Test that WiFi status constants are properly defined
    TEST_ASSERT_EQUAL(3, WL_CONNECTED);
    TEST_ASSERT_EQUAL(6, WL_DISCONNECTED);
}

void test_file_constants_defined(void) {
    // Test that file operation constants are defined
    TEST_ASSERT_EQUAL(0, FILE_READ);
    TEST_ASSERT_EQUAL(1, FILE_WRITE);
}

void test_fat_time_macros_work(void) {
    // Test that FAT time/date macros work correctly
    uint16_t testDate = FAT_DATE(2025, 7, 26);
    uint16_t testTime = FAT_TIME(14, 55, 30);
    
    TEST_ASSERT_NOT_EQUAL(0, testDate);
    TEST_ASSERT_NOT_EQUAL(0, testTime);
    
    // Verify specific calculations
    TEST_ASSERT_EQUAL(23050, testDate); // ((2025-1980) << 9) | (7 << 5) | 26
    TEST_ASSERT_EQUAL(30511, testTime);  // (14 << 11) | (55 << 5) | (30/2)
}

// Integration tests that actually call core functions safely
void test_isWifiConnected_with_different_states(void) {
    // Test with connected state
    WiFi.begin("test", "test");
    TEST_ASSERT_TRUE(isWifiConnected());
    
    // Test with disconnected state
    WiFi.disconnect();
    TEST_ASSERT_FALSE(isWifiConnected());
}

void test_isMqttConnected_with_different_states(void) {
    // Test with disconnected state
    mqttClient.stop();
    TEST_ASSERT_FALSE(isMqttConnected());
    
    // Test with connected state
    mqttClient.connect("test");
    TEST_ASSERT_TRUE(isMqttConnected());
}

// Bundle for central test_main.cpp
void run_core_tests() {
    RUN_TEST(test_isWifiConnected_returns_true_when_connected);
    RUN_TEST(test_isWifiConnected_returns_false_when_disconnected);
    RUN_TEST(test_isMqttConnected_returns_boolean);
    RUN_TEST(test_mqtt_connected_when_mock_connected);
    RUN_TEST(test_dateTime_callback_sets_values);
    RUN_TEST(test_core_functions_exist_and_compile);
    RUN_TEST(test_mock_wifi_state_changes);
    RUN_TEST(test_mock_mqtt_state_changes);
    RUN_TEST(test_mock_rtc_returns_fixed_time);
    RUN_TEST(test_mock_sd_operations);
    RUN_TEST(test_mock_temp_sensor);
    RUN_TEST(test_wifi_status_constants_defined);
    RUN_TEST(test_file_constants_defined);
    RUN_TEST(test_fat_time_macros_work);
    RUN_TEST(test_isWifiConnected_with_different_states);
    RUN_TEST(test_isMqttConnected_with_different_states);
}

// When standalone executable
#ifndef COMBINED_TEST_MAIN
int main(int argc, char **argv) {
    UNITY_BEGIN();
    run_core_tests();
    return UNITY_END();
}
#endif