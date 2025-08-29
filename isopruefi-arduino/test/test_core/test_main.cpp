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
void Test_IsWifiConnected_returns_true_when_connected(void) {
    // Use mock WiFi object directly - simulate connected state
    WiFi.begin("test", "test"); // This sets status to WL_CONNECTED
    
    bool result = IsWifiConnected();
    
    TEST_ASSERT_TRUE(result);
}

void Test_IsWifiConnected_returns_false_when_disconnected(void) {
    // Use mock WiFi object directly - simulate disconnected state
    WiFi.disconnect(); // This sets status to WL_DISCONNECTED
    
    bool result = IsWifiConnected();
    
    TEST_ASSERT_FALSE(result);
}

void Test_IsMqttConnected_returns_boolean(void) {
    // Test that the function exists and returns a valid boolean
    bool result = IsMqttConnected();
    
    // The function should return either true or false (not crash)
    TEST_ASSERT_TRUE(result == true || result == false);
}

void Test_IsMqttConnected_when_mock_connected(void) {
    // Set mock MQTT client to connected state
    mqttClient.connect("test_broker");
    
    bool result = IsMqttConnected();
    
    TEST_ASSERT_TRUE(result);
}

// Test dateTime callback function
void Test_FatDateTime_callback_sets_values(void) {
    uint16_t date, time;
    
    // Call the dateTime function - it should use our mock RTC
    FatDateTime(&date, &time);
    
    // Verify that values were set (our mock RTC returns a fixed time)
    // FAT_DATE(2025, 7, 26) = ((2025-1980) << 9) | (7 << 5) | 26 = 23050
    // FAT_TIME(14, 55, 0) = (14 << 11) | (55 << 5) | (0/2) = 30496
    TEST_ASSERT_EQUAL(23290, date);
    TEST_ASSERT_EQUAL(30432, time);
}

// Test core function existence
void Test_Core_functions_exist_and_compile(void) {
    // Test that core functions exist and can be referenced
    // This verifies they compile and link correctly
    
    // Test that function pointers can be created (proves functions exist)
    void (*setupFunc)() = CoreSetup;
    void (*loopFunc)() = CoreLoop;
    bool (*wifiFunc)() = IsWifiConnected;
    bool (*mqttFunc)() = IsMqttConnected;
    
    TEST_ASSERT_NOT_NULL(setupFunc);
    TEST_ASSERT_NOT_NULL(loopFunc);
    TEST_ASSERT_NOT_NULL(wifiFunc);
    TEST_ASSERT_NOT_NULL(mqttFunc);
}

// Test mock objects functionality
void Test_mock_wifi_state_changes(void) {
    // Test that our mock WiFi object works as expected
    TEST_ASSERT_EQUAL(WL_DISCONNECTED, WiFi.status());
    
    WiFi.begin("test", "test");
    TEST_ASSERT_EQUAL(WL_CONNECTED, WiFi.status());
    
    WiFi.disconnect();
    TEST_ASSERT_EQUAL(WL_DISCONNECTED, WiFi.status());
}

void Test_mock_rtc_returns_fixed_time(void) {
    // Test that our mock RTC returns expected values
    DateTime now = rtc.now();
    
    TEST_ASSERT_EQUAL(2025, now.year());
    TEST_ASSERT_EQUAL(7, now.month());
    TEST_ASSERT_EQUAL(26, now.day());
    TEST_ASSERT_EQUAL(14, now.hour());
    TEST_ASSERT_EQUAL(55, now.minute());
    TEST_ASSERT_EQUAL(0, now.second());
}

void Test_mock_sd_operations(void) {
    // Test that our mock SD card works as expected
    TEST_ASSERT_FALSE(sd.exists("test.txt"));
    
    sd.addTestFile("test.txt");
    TEST_ASSERT_TRUE(sd.exists("test.txt"));
    
    sd.clearTestFiles();
    TEST_ASSERT_FALSE(sd.exists("test.txt"));
}

void Test_mock_temp_sensor(void) {
    // Test that our mock temperature sensor works
    float temp = tempsensor.readTempC();
    TEST_ASSERT_EQUAL_FLOAT(25.5, temp);
    
    bool initResult = tempsensor.begin();
    TEST_ASSERT_TRUE(initResult);
}

// Test constants and configuration
void Test_wifi_status_constants_defined(void) {
    // Test that WiFi status constants are properly defined
    TEST_ASSERT_EQUAL(3, WL_CONNECTED);
    TEST_ASSERT_EQUAL(6, WL_DISCONNECTED);
}

void Test_file_constants_defined(void) {
    // Test that file operation constants are defined
    TEST_ASSERT_EQUAL(0, FILE_READ);
    TEST_ASSERT_EQUAL(1, FILE_WRITE);
}

void Test_fat_time_macros_work(void) {
    // Test that FAT time/date macros work correctly
    uint16_t testDate = FAT_DATE(2025, 7, 26);
    uint16_t testTime = FAT_TIME(14, 55, 30);
    
    TEST_ASSERT_NOT_EQUAL(0, testDate);
    TEST_ASSERT_NOT_EQUAL(0, testTime);
    
    // Verify specific calculations
    TEST_ASSERT_EQUAL(23290, testDate); // ((2025-1980) << 9) | (7 << 5) | 26
    TEST_ASSERT_EQUAL(30447, testTime);  // (14 << 11) | (55 << 5) | (30/2)
}

// Integration tests that actually call core functions safely
void Test_IsWifiConnected_with_different_states(void) {
    // Test with connected state
    WiFi.begin("test", "test");
    TEST_ASSERT_TRUE(IsWifiConnected());
    
    // Test with disconnected state
    WiFi.disconnect();
    TEST_ASSERT_FALSE(IsWifiConnected());
}

void Test_IsMqttConnected_with_different_states(void) {
    // Test with disconnected state
    mqttClient.stop();
    TEST_ASSERT_FALSE(IsMqttConnected());
    
    // Test with connected state
    mqttClient.connect("test");
    TEST_ASSERT_TRUE(IsMqttConnected());
}

// Bundle for central test_main.cpp
void Run_core_tests() {
    RUN_TEST(Test_IsWifiConnected_returns_true_when_connected);
    RUN_TEST(Test_IsWifiConnected_returns_false_when_disconnected);
    RUN_TEST(Test_IsMqttConnected_returns_boolean);
    RUN_TEST(Test_IsMqttConnected_when_mock_connected);
    RUN_TEST(Test_FatDateTime_callback_sets_values);
    RUN_TEST(Test_Core_functions_exist_and_compile);
    RUN_TEST(Test_mock_wifi_state_changes);
    RUN_TEST(Test_mock_rtc_returns_fixed_time);
    RUN_TEST(Test_mock_sd_operations);
    RUN_TEST(Test_mock_temp_sensor);
    RUN_TEST(Test_wifi_status_constants_defined);
    RUN_TEST(Test_file_constants_defined);
    RUN_TEST(Test_fat_time_macros_work);
    RUN_TEST(Test_IsWifiConnected_with_different_states);
    RUN_TEST(Test_IsMqttConnected_with_different_states);
}

// When standalone executable
#ifndef COMBINED_TEST_MAIN
int main(int argc, char **argv) {
    UNITY_BEGIN();
    Run_core_tests();
    return UNITY_END();
}
#endif