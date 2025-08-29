#include <ArduinoFake.h>
#include <unity.h>
#include "network.h"

using namespace fakeit;

void setUp(void) {
    ArduinoFakeReset();
    When(OverloadedMethod(ArduinoFake(Serial), print, size_t(const char[]))).AlwaysReturn(1);
    When(OverloadedMethod(ArduinoFake(Serial), println, size_t(const char[]))).AlwaysReturn(1);
    When(Method(ArduinoFake(), delay)).Return();
}

void tearDown(void) {
    ArduinoFakeReset();
}

void Test_ConnectToWiFi_success(void) {
    // Set up WiFi mock to succeed
    WiFi.begin("test", "test"); // Initialize mock WiFi
    
    // Mock millis() to simulate time progression
    When(Method(ArduinoFake(), millis)).Return(0, 1000, 2000);
    
    // Call the function
    bool result = ConnectToWiFi(10000);
    
    // Verify successful connection
    TEST_ASSERT_TRUE(result);
}

void Test_ConnectToMQTT_success(void) {
    // Use the global mock MQTT client
    MqttClient& mqttClient = ::mqttClient;
    
    // Mock millis() for timing
    When(Method(ArduinoFake(), millis)).Return(0, 1000);
    
    // Call the function
    bool result = ConnectToMQTT(mqttClient, 10000);
    
    // Verify successful connection
    TEST_ASSERT_TRUE(result);
}

void Test_IsConnectedToServer_both_connected(void) {
    // Set WiFi to connected
    WiFi.begin("test", "test");
    
    // Use connected MQTT client
    MqttClient& mqttClient = ::mqttClient;
    mqttClient.connect("test");
    
    // Call the function
    bool result = IsConnectedToServer(mqttClient);
    
    // Verify both connections result in true
    TEST_ASSERT_TRUE(result);
}

void Test_IsConnectedToServer_wifi_disconnected(void) {
    // Set WiFi to disconnected
    WiFi.disconnect();
    
    // MQTT client status doesn't matter since WiFi is disconnected
    MqttClient& mqttClient = ::mqttClient;
    mqttClient.connect("test");
    
    // Call the function
    bool result = IsConnectedToServer(mqttClient);
    
    // Verify result is false due to WiFi disconnection
    TEST_ASSERT_FALSE(result);
}

void Test_IsConnectedToServer_mqtt_disconnected(void) {
    // Set WiFi to connected
    WiFi.begin("test", "test");
    
    // Set MQTT client to disconnected
    MqttClient& mqttClient = ::mqttClient;
    mqttClient.stop();
    
    // Call the function
    bool result = IsConnectedToServer(mqttClient);
    
    // Verify result is false due to MQTT disconnection
    TEST_ASSERT_FALSE(result);
}

void Test_IsConnectedToServer_both_disconnected(void) {
    // Set WiFi to disconnected
    WiFi.disconnect();
    
    // Set MQTT client to disconnected
    MqttClient& mqttClient = ::mqttClient;
    mqttClient.stop();
    
    // Call the function
    bool result = IsConnectedToServer(mqttClient);
    
    // Verify result is false
    TEST_ASSERT_FALSE(result);
}

// Bundle for central test_main.cpp
void Run_network_tests() {
    RUN_TEST(Test_ConnectToWiFi_success);
    RUN_TEST(Test_ConnectToMQTT_success);
    RUN_TEST(Test_IsConnectedToServer_both_connected);
    RUN_TEST(Test_IsConnectedToServer_wifi_disconnected);
    RUN_TEST(Test_IsConnectedToServer_mqtt_disconnected);
    RUN_TEST(Test_IsConnectedToServer_both_disconnected);
}

// When standalone executable
#ifndef COMBINED_TEST_MAIN
int main(int argc, char **argv) {
    UNITY_BEGIN();
    Run_network_tests();
    return UNITY_END();
}
#endif