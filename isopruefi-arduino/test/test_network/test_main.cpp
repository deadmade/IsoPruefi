#include "platform.h"
#include <unity.h>
#include "network.h"

// Test WiFi connection success
void test_connectWiFi_success(void) {
    WiFi.setConnectResult(true);
    WiFi.setStatus(WL_CONNECTED);
    
    bool result = connectWiFi(10000);
    
    TEST_ASSERT_TRUE(result);
}

// Test WiFi connection failure
void test_connectWiFi_failure(void) {
    WiFi.setConnectResult(false);
    WiFi.setStatus(WL_DISCONNECTED);
    
    bool result = connectWiFi(10000);
    
    TEST_ASSERT_FALSE(result);
}

// Test MQTT connection success
void test_connectMQTT_success(void) {
    MockMqttClient client;
    MockMqttClient::setConnected(true);
    
    bool result = connectMQTT(client, 10000);
    
    TEST_ASSERT_TRUE(result);
}

// Test MQTT connection failure
void test_connectMQTT_failure(void) {
    MockMqttClient client;
    MockMqttClient::setConnected(false);
    
    bool result = connectMQTT(client, 10000);
    
    TEST_ASSERT_FALSE(result);
}

// Test server connection status - both connected
void test_isConnectedToServer_both_connected(void) {
    WiFi.setStatus(WL_CONNECTED);
    MockMqttClient client;
    MockMqttClient::setConnected(true);
    
    bool result = isConnectedToServer(client);
    
    TEST_ASSERT_TRUE(result);
}

void setUp(void) {
    // Reset all network mocks before each test
    WiFi.setStatus(WL_DISCONNECTED);
    WiFi.setConnectResult(false);
    MockMqttClient::reset();
}

void tearDown(void) {
    // Clean up after each test
}

// Bundle for central test runner
void run_network_tests() {
    RUN_TEST(test_connectWiFi_success);
    RUN_TEST(test_connectWiFi_failure);
    RUN_TEST(test_connectMQTT_success);
    RUN_TEST(test_connectMQTT_failure);
    RUN_TEST(test_isConnectedToServer_both_connected);
}

// When standalone executable
#ifndef COMBINED_TEST_MAIN
int main(int argc, char **argv) {
    UNITY_BEGIN();
    run_network_tests();
    return UNITY_END();
}
#endif