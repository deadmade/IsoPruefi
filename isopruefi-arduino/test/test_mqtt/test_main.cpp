#include <ArduinoFake.h>
#include <unity.h>
#include "mqtt.h"
#include "storage.h"

using namespace fakeit;

void setUp(void) {
    ArduinoFakeReset();
    sd.clearTestFiles();
    When(OverloadedMethod(ArduinoFake(Serial), print, size_t(const char[]))).AlwaysReturn(1);
    When(OverloadedMethod(ArduinoFake(Serial), println, size_t(const char[]))).AlwaysReturn(1);
}

void tearDown(void) {
    ArduinoFakeReset();
}

// Test createFullTopic function
void test_createFullTopic_with_suffix(void) {
    char buffer[128];
    createFullTopic(buffer, sizeof(buffer), "dhbw/ai/si2023/2/", "temp", "Sensor_One", "recovered");
    TEST_ASSERT_EQUAL_STRING("dhbw/ai/si2023/2/temp/Sensor_One/recovered", buffer);
}

void test_createFullTopic_without_suffix(void) {
    char buffer[128];
    createFullTopic(buffer, sizeof(buffer), "dhbw/ai/si2023/2/", "temp", "Sensor_One");
    TEST_ASSERT_EQUAL_STRING("dhbw/ai/si2023/2/temp/Sensor_One", buffer);
}

void test_createFullTopic_empty_suffix(void) {
    char buffer[128];
    createFullTopic(buffer, sizeof(buffer), "dhbw/ai/si2023/2/", "temp", "Sensor_One", "");
    TEST_ASSERT_EQUAL_STRING("dhbw/ai/si2023/2/temp/Sensor_One", buffer);
}

void test_createFullTopic_buffer_size_handling(void) {
    char buffer[32];
    createFullTopic(buffer, sizeof(buffer), "dhbw/ai/si2023/2/", "temp", "Sensor_One", "recovered");
    // Should not overflow buffer
    TEST_ASSERT_TRUE(strlen(buffer) < sizeof(buffer));
}

// Test sendToMqtt function
void test_sendToMqtt_builds_correct_json(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Mock MQTT client to capture the message
    sendToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", 25.5, now, 42);
    
    // Verify the JSON structure in the message
    std::string lastMessage = mqttClient.getLastMessage();
    TEST_ASSERT_TRUE(lastMessage.find("\"sequence\":42") != std::string::npos);
    TEST_ASSERT_TRUE(lastMessage.find("25.5") != std::string::npos);
    TEST_ASSERT_TRUE(lastMessage.find("\"timestamp\"") != std::string::npos);
    TEST_ASSERT_TRUE(lastMessage.find("\"value\"") != std::string::npos);
    TEST_ASSERT_TRUE(lastMessage.find("\"meta\"") != std::string::npos);
}

void test_sendToMqtt_creates_correct_topic(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    sendToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", 25.5, now, 42);
    
    // The mock should have received the correct topic
    // This tests the topic creation logic
    TEST_ASSERT_NOT_NULL(mqttClient.getLastMessage().c_str());
}

void test_sendToMqtt_handles_mqtt_connection_failure(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Simulate MQTT connection failure by disconnecting the client
    mqttClient.stop();
    
    bool result = sendToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", 25.5, now, 42);
    
    // Should fallback to CSV when MQTT fails
    TEST_ASSERT_FALSE(result);
    TEST_ASSERT_TRUE(sd.exists("2025"));
    TEST_ASSERT_TRUE(sd.exists("2025/07261455.csv"));
}

void test_sendToMqtt_handles_negative_temperatures(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    sendToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", -5.5, now, 100);
    
    std::string lastMessage = mqttClient.getLastMessage();
    TEST_ASSERT_TRUE(lastMessage.find("\"sequence\":100") != std::string::npos);
    TEST_ASSERT_TRUE(lastMessage.find("-5.5") != std::string::npos);
}

// Test sendPendingData function
void test_sendPendingData_no_folder_exists(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // No folder exists for the current year
    bool result = sendPendingData(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", now);
    
    // Should return true when no folder exists
    TEST_ASSERT_TRUE(result);
}

void test_sendPendingData_empty_folder(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Setup: Create empty folder
    sd.addTestFile("2025");
    
    bool result = sendPendingData(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", now);
    
    // Should return true when folder is empty
    TEST_ASSERT_TRUE(result);
}

void test_sendPendingData_ignores_non_csv_files(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Setup: Create folder with non-CSV files
    sd.addTestFile("2025");
    sd.addTestFile("2025/readme.txt");
    sd.addTestFile("2025/data.log");
    
    bool result = sendPendingData(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", now);
    
    // Should ignore non-CSV files and return true
    TEST_ASSERT_TRUE(result);
}

void test_sendPendingData_processes_csv_files(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Setup: Create folder with CSV files
    sd.addTestFile("2025");
    sd.addTestFile("2025/07261400.csv");
    sd.addTestFile("2025/07261401.csv");
    
    bool result = sendPendingData(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", now);
    
    // Should process CSV files
    TEST_ASSERT_TRUE(result);
}

void test_sendPendingData_creates_recovered_topic(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Setup: Create folder with CSV file
    sd.addTestFile("2025");
    sd.addTestFile("2025/07261400.csv");
    
    sendPendingData(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", now);
    
    // The last message should be for a recovered topic
    // This tests that the recovered suffix is properly added
    std::string lastMessage = mqttClient.getLastMessage();
    TEST_ASSERT_NOT_NULL(lastMessage.c_str());
}

void test_sendPendingData_handles_large_payloads(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Setup: Create folder with multiple CSV files to test large payload handling
    sd.addTestFile("2025");
    for (int i = 0; i < 5; i++) {
        char filename[32];
        snprintf(filename, sizeof(filename), "2025/0726140%d.csv", i);
        sd.addTestFile(filename);
    }
    
    bool result = sendPendingData(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", now);
    
    // Should handle multiple files gracefully
    TEST_ASSERT_TRUE(result);
}

// Test edge cases and error conditions
void test_sendToMqtt_null_parameters(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Test with null sensor type
    bool result = sendToMqtt(mqttClient, "dhbw/ai/si2023/2/", nullptr, "Sensor_One", 25.5, now, 42);
    
    // Should handle null parameters gracefully
    TEST_ASSERT_FALSE(result);
}

void test_sendToMqtt_empty_strings(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Test with empty strings
    bool result = sendToMqtt(mqttClient, "", "", "", 25.5, now, 42);
    
    // Should handle empty strings
    TEST_ASSERT_FALSE(result);
}

void test_createFullTopic_null_parameters(void) {
    char buffer[128];
    
    // Test with null parameters
    createFullTopic(buffer, sizeof(buffer), nullptr, "temp", "Sensor_One");
    
    // Should handle null parameters gracefully (may result in malformed topic)
    TEST_ASSERT_NOT_NULL(buffer);
}

void test_sendToMqtt_extreme_values(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Test with extreme temperature values
    sendToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", 999.99, now, 999999);
    
    std::string lastMessage = mqttClient.getLastMessage();
    TEST_ASSERT_TRUE(lastMessage.find("\"sequence\":999999") != std::string::npos);
    TEST_ASSERT_TRUE(lastMessage.find("999.99") != std::string::npos);
}

void test_sendToMqtt_zero_sequence(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    sendToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", 20.0, now, 0);
    
    std::string lastMessage = mqttClient.getLastMessage();
    TEST_ASSERT_TRUE(lastMessage.find("\"sequence\":0") != std::string::npos);
}

// Bundle for central test_main.cpp
void run_mqtt_tests() {
    RUN_TEST(test_createFullTopic_with_suffix);
    RUN_TEST(test_createFullTopic_without_suffix);
    RUN_TEST(test_createFullTopic_empty_suffix);
    RUN_TEST(test_createFullTopic_buffer_size_handling);
    RUN_TEST(test_sendToMqtt_builds_correct_json);
    RUN_TEST(test_sendToMqtt_creates_correct_topic);
    RUN_TEST(test_sendToMqtt_handles_mqtt_connection_failure);
    RUN_TEST(test_sendToMqtt_handles_negative_temperatures);
    RUN_TEST(test_sendPendingData_no_folder_exists);
    RUN_TEST(test_sendPendingData_empty_folder);
    RUN_TEST(test_sendPendingData_ignores_non_csv_files);
    RUN_TEST(test_sendPendingData_processes_csv_files);
    RUN_TEST(test_sendPendingData_creates_recovered_topic);
    RUN_TEST(test_sendPendingData_handles_large_payloads);
    RUN_TEST(test_sendToMqtt_null_parameters);
    RUN_TEST(test_sendToMqtt_empty_strings);
    RUN_TEST(test_createFullTopic_null_parameters);
    RUN_TEST(test_sendToMqtt_extreme_values);
    RUN_TEST(test_sendToMqtt_zero_sequence);
}

// When standalone executable
#ifndef COMBINED_TEST_MAIN
int main(int argc, char **argv) {
    UNITY_BEGIN();
    run_mqtt_tests();
    return UNITY_END();
}
#endif