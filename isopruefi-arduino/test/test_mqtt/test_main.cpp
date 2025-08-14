#include "platform.h"
#include <unity.h>
#include "mqtt.h"

// Test data
DateTime testTime(2025, 7, 26, 14, 55, 0);
const char* testTopicPrefix = "dhbw/ai/si2023/2/";
const char* testSensorType = "temp";
const char* testSensorId = "Sensor_One";

// Test MQTT topic creation
void test_createFullTopic_normal(void) {
    char buffer[128];
    
    createFullTopic(buffer, sizeof(buffer), testTopicPrefix, testSensorType, testSensorId, nullptr);
    
    TEST_ASSERT_EQUAL_STRING("dhbw/ai/si2023/2/temp/Sensor_One", buffer);
}

// Test MQTT topic creation with suffix
void test_createFullTopic_with_suffix(void) {
    char buffer[128];
    
    createFullTopic(buffer, sizeof(buffer), testTopicPrefix, testSensorType, testSensorId, "recovered");
    
    TEST_ASSERT_EQUAL_STRING("dhbw/ai/si2023/2/temp/Sensor_One/recovered", buffer);
}

// Test pending data recovery - no files
void test_sendPendingData_no_files(void) {
    MockMqttClient client;
    MockMqttClient::setConnected(true);
    MockSdFat::reset();
    
    bool result = sendPendingData(client, testTopicPrefix, testSensorType, testSensorId, testTime);
    
    TEST_ASSERT_TRUE(result); // Should return true when no files to process
}

void setUp(void) {
    // Reset all MQTT mocks before each test
    MockMqttClient::reset();
    MockSdFat::reset();
}

void tearDown(void) {
    // Clean up after each test
}

// Bundle for central test runner
void run_mqtt_tests() {
    RUN_TEST(test_createFullTopic_normal);
    RUN_TEST(test_createFullTopic_with_suffix);
    RUN_TEST(test_sendPendingData_no_files);
}

// When standalone executable
#ifndef COMBINED_TEST_MAIN
int main(int argc, char **argv) {
    UNITY_BEGIN();
    run_mqtt_tests();
    return UNITY_END();
}
#endif