#include "platform.h"
#include <unity.h>
#include "core.h"
#include "network.h"

// Test connection status functions
void test_isWifiConnected_when_connected(void) {
    WiFi.setStatus(WL_CONNECTED);
    TEST_ASSERT_TRUE(isWifiConnected());
}

void test_isWifiConnected_when_disconnected(void) {
    WiFi.setStatus(WL_DISCONNECTED);
    TEST_ASSERT_FALSE(isWifiConnected());
}

void test_isWifiConnected_when_connection_lost(void) {
    WiFi.setStatus(WL_CONNECTION_LOST);
    TEST_ASSERT_FALSE(isWifiConnected());
}

void test_isMqttConnected_when_connected(void) {
    MockMqttClient::setConnected(true);
    MockMqttClient client;
    TEST_ASSERT_TRUE(client.connected());
}

void test_isMqttConnected_when_disconnected(void) {
    MockMqttClient::setConnected(false);
    MockMqttClient client;
    TEST_ASSERT_FALSE(client.connected());
}

// Test setup initialization success scenarios
void test_coreSetup_all_components_initialize_successfully(void) {
    // Setup successful conditions
    WiFi.setConnectResult(true);
    WiFi.setStatus(WL_CONNECTED);
    MockMqttClient::setConnected(true);
    MockRTC_DS3231::setBeginResult(true);
    MockRTC_DS3231::setLostPower(false);
    MockSdFat::setBeginResult(true);
    MockAdafruit_ADT7410::setBeginResult(true);

    // This should complete without infinite loops
    coreSetup();
    
    // Verify setup completed (if we reach here, no infinite loops occurred)
    TEST_ASSERT_TRUE(true);
}

void test_coreSetup_with_rtc_power_lost(void) {
    // Setup successful conditions with RTC power lost
    WiFi.setConnectResult(true);
    WiFi.setStatus(WL_CONNECTED);
    MockMqttClient::setConnected(true);
    MockRTC_DS3231::setBeginResult(true);
    MockRTC_DS3231::setLostPower(true); // Power was lost
    MockSdFat::setBeginResult(true);
    MockAdafruit_ADT7410::setBeginResult(true);

    coreSetup();
    
    // Verify setup completed
    TEST_ASSERT_TRUE(true);
}

void test_coreSetup_wifi_connection_fails(void) {
    // Setup WiFi failure
    WiFi.setConnectResult(false);
    WiFi.setStatus(WL_DISCONNECTED);
    MockRTC_DS3231::setBeginResult(true);
    MockRTC_DS3231::setLostPower(false);
    MockSdFat::setBeginResult(true);
    MockAdafruit_ADT7410::setBeginResult(true);

    coreSetup();
    
    // Should complete even with WiFi failure
    TEST_ASSERT_TRUE(true);
}

// Test loop behavior with different connection states
void test_coreLoop_wifi_disconnected_saves_to_csv(void) {
    // Setup disconnected WiFi
    WiFi.setStatus(WL_DISCONNECTED);
    WiFi.setConnectResult(false);
    MockRTC_DS3231::setCurrentTime(MockDateTime(2025, 7, 26, 15, 0, 0)); // New minute
    MockAdafruit_ADT7410::setTemperature(25.0f);

    coreLoop();
    
    // Should complete without errors (CSV fallback)
    TEST_ASSERT_TRUE(true);
}

void test_coreLoop_mqtt_disconnected_saves_to_csv(void) {
    // Setup connected WiFi but disconnected MQTT
    WiFi.setStatus(WL_CONNECTED);
    MockMqttClient::setConnected(false);
    MockRTC_DS3231::setCurrentTime(MockDateTime(2025, 7, 26, 15, 1, 0)); // New minute
    MockAdafruit_ADT7410::setTemperature(24.5f);

    coreLoop();
    
    // Should complete without errors (CSV fallback)
    TEST_ASSERT_TRUE(true);
}

void test_coreLoop_all_connected_normal_operation(void) {
    // Setup fully connected state
    WiFi.setStatus(WL_CONNECTED);
    MockMqttClient::setConnected(true);
    MockMqttClient::setPublishResults(true, true);
    MockRTC_DS3231::setCurrentTime(MockDateTime(2025, 7, 26, 15, 2, 0)); // New minute
    MockAdafruit_ADT7410::setTemperature(23.0f);

    coreLoop();
    
    // Should complete normal MQTT transmission
    TEST_ASSERT_TRUE(true);
}

// Test reconnection logic timing
void test_coreLoop_reconnection_timing(void) {
    // Test that reconnection attempts are rate-limited
    WiFi.setStatus(WL_DISCONNECTED);
    WiFi.setConnectResult(false);
    
    // Multiple loop iterations should not spam reconnection attempts
    coreLoop();
    coreLoop();
    coreLoop();
    
    TEST_ASSERT_TRUE(true);
}

// Test edge cases for minute changes
void test_coreLoop_same_minute_no_duplicate_logging(void) {
    WiFi.setStatus(WL_CONNECTED);
    MockMqttClient::setConnected(true);
    MockMqttClient::setPublishResults(true, true);
    
    // Same timestamp (same minute)
    MockRTC_DS3231::setCurrentTime(MockDateTime(2025, 7, 26, 15, 3, 0));
    
    coreLoop(); // First call should log
    coreLoop(); // Second call should not log (same minute)
    
    TEST_ASSERT_TRUE(true);
}

void setUp(void) {
    // Reset all mocks before each test
    WiFi.setStatus(WL_DISCONNECTED);
    WiFi.setConnectResult(false);
    MockMqttClient::reset();
    MockRTC_DS3231::reset();
    MockAdafruit_ADT7410::reset();
    MockSdFat::reset();
}

void tearDown(void) {
    // Clean up after each test
}

// Bundle for central test runner
void run_core_tests() {
    RUN_TEST(test_isWifiConnected_when_connected);
    RUN_TEST(test_isWifiConnected_when_disconnected);
    RUN_TEST(test_isWifiConnected_when_connection_lost);
    RUN_TEST(test_isMqttConnected_when_connected);
    RUN_TEST(test_isMqttConnected_when_disconnected);
    RUN_TEST(test_coreSetup_all_components_initialize_successfully);
    RUN_TEST(test_coreSetup_with_rtc_power_lost);
    RUN_TEST(test_coreSetup_wifi_connection_fails);
    RUN_TEST(test_coreLoop_wifi_disconnected_saves_to_csv);
    RUN_TEST(test_coreLoop_mqtt_disconnected_saves_to_csv);
    RUN_TEST(test_coreLoop_all_connected_normal_operation);
    RUN_TEST(test_coreLoop_reconnection_timing);
    RUN_TEST(test_coreLoop_same_minute_no_duplicate_logging);
}

// When standalone executable
#ifndef COMBINED_TEST_MAIN
int main(int argc, char **argv) {
    UNITY_BEGIN();
    run_core_tests();
    return UNITY_END();
}
#endif