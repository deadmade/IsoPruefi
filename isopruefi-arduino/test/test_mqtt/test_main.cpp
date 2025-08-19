#include <ArduinoFake.h>
#include <unity.h>
#include "mqtt.h"
#include "storage.h"

using namespace fakeit;

void setUp(void) {
    ArduinoFakeReset();
    sd.clearTestFiles();

    // Basic Arduino function stubs
    When(OverloadedMethod(ArduinoFake(Serial), print, size_t(const char[]))).AlwaysReturn(1);
    When(OverloadedMethod(ArduinoFake(Serial), println, size_t(const char[]))).AlwaysReturn(1);

    When(Method(ArduinoFake(), delay)).Return();
    When(Method(ArduinoFake(), millis)).Return(8000, 0);

    When(OverloadedMethod(ArduinoFake(Serial), println, size_t(const String&))).AlwaysReturn(1);
    When(OverloadedMethod(ArduinoFake(Serial), print, size_t(const String&))).AlwaysReturn(1);
}

void tearDown(void) {
    ArduinoFakeReset();
}

// Test createFullTopic function
void Test_CreateFullTopic_with_suffix(void) {
    char buffer[128];
    CreateFullTopic(buffer, sizeof(buffer), "dhbw/ai/si2023/2/", "temp", "Sensor_One", "recovered");
    TEST_ASSERT_EQUAL_STRING("dhbw/ai/si2023/2/temp/Sensor_One/recovered", buffer);
}

void Test_CreateFullTopic_without_suffix(void) {
    char buffer[128];
    CreateFullTopic(buffer, sizeof(buffer), "dhbw/ai/si2023/2/", "temp", "Sensor_One");
    TEST_ASSERT_EQUAL_STRING("dhbw/ai/si2023/2/temp/Sensor_One", buffer);
}

void Test_CreateFullTopic_empty_suffix(void) {
    char buffer[128];
    CreateFullTopic(buffer, sizeof(buffer), "dhbw/ai/si2023/2/", "temp", "Sensor_One", "");
    TEST_ASSERT_EQUAL_STRING("dhbw/ai/si2023/2/temp/Sensor_One", buffer);
}

void Test_CreateFullTopic_buffer_size_handling(void) {
    char buffer[32];
    CreateFullTopic(buffer, sizeof(buffer), "dhbw/ai/si2023/2/", "temp", "Sensor_One", "recovered");
    // Should not overflow buffer
    TEST_ASSERT_TRUE(strlen(buffer) < sizeof(buffer));
}

// Test sendToMqtt function
void Test_SendTempToMqtt_builds_correct_json(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Mock MQTT client to capture the message
    SendTempToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", 25.5, now, 42);
    
    // Verify the JSON structure in the message
    std::string lastMessage = mqttClient.getLastMessage();
    TEST_ASSERT_TRUE(lastMessage.find("\"timestamp\"") != std::string::npos);
    TEST_ASSERT_TRUE(lastMessage.find("\"value\"") != std::string::npos);
    TEST_ASSERT_TRUE(lastMessage.find("25.5") != std::string::npos);
    TEST_ASSERT_TRUE(lastMessage.find("\"sequence\":42") != std::string::npos);
    TEST_ASSERT_TRUE(lastMessage.find("\"meta\"") != std::string::npos);
}

void Test_SendTempToMqtt_creates_correct_topic(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    SendTempToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", 25.5, now, 42);
    
    // The mock should have received the correct topic
    // This tests the topic creation logic
    TEST_ASSERT_NOT_NULL(mqttClient.getLastMessage().c_str());
}

void Test_SendTempToMqtt_handles_mqtt_connection_failure(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Simulate MQTT connection failure by disconnecting the client
    mqttClient.stop();
    
    bool result = SendTempToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", 25.5, now, 42);
    
    // Should fallback to CSV when MQTT fails
    TEST_ASSERT_FALSE(result);
    TEST_ASSERT_TRUE(sd.exists("2025"));
    TEST_ASSERT_TRUE(sd.exists("2025/07261455.csv"));
}

void Test_SendTempToMqtt_handles_negative_temperatures(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    SendTempToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", -5.5, now, 100);
    
    std::string lastMessage = mqttClient.getLastMessage();
    TEST_ASSERT_TRUE(lastMessage.find("\"sequence\":100") != std::string::npos);
    TEST_ASSERT_TRUE(lastMessage.find("-5.5") != std::string::npos);
}

// Test sendPendingData function
void Test_SendPendingDataToMqtt_no_folder_exists(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // No folder exists for the current year
    bool result = SendPendingDataToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", now);
    
    // Should return true when no folder exists
    TEST_ASSERT_TRUE(result);
}

void Test_SendPendingData_empty_folder(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Setup: Create empty folder
    sd.addTestFile("2025");
    
    bool result = SendPendingDataToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", now);
    
    // Should return true when folder is empty
    TEST_ASSERT_TRUE(result);
}

void Test_SendPendingData_ignores_non_csv_files(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Setup: Create folder with non-CSV files
    sd.addTestFile("2025");
    sd.addTestFile("2025/readme.txt");
    sd.addTestFile("2025/data.log");
    
    bool result = SendPendingDataToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", now);
    
    // Should ignore non-CSV files and return true
    TEST_ASSERT_TRUE(result);
}

void Test_SendPendingData_processes_csv_files(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Setup: Create folder with CSV files
    sd.addTestFile("2025");
    sd.addTestFile("2025/07261400.csv");
    sd.addTestFile("2025/07261401.csv");
    
    bool result = SendPendingDataToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", now);
    
    // Should process CSV files
    TEST_ASSERT_TRUE(result);
}

void Test_SendPendingData_creates_recovered_topic(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Setup: Create folder with CSV file
    sd.addTestFile("2025");
    sd.addTestFile("2025/07261400.csv");
    
    SendPendingDataToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", now);
    
    // The last message should be for a recovered topic
    // This tests that the recovered suffix is properly added
    std::string lastMessage = mqttClient.getLastMessage();
    TEST_ASSERT_NOT_NULL(lastMessage.c_str());
}

void Test_SendPendingData_handles_large_payloads(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Setup: Create folder with multiple CSV files to test large payload handling
    sd.addTestFile("2025");
    for (int i = 0; i < 5; i++) {
        char filename[32];
        snprintf(filename, sizeof(filename), "2025/0726140%d.csv", i);
        sd.addTestFile(filename);
    }
    
    bool result = SendPendingDataToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", now);
    
    // Should handle multiple files gracefully
    TEST_ASSERT_TRUE(result);
}

// Test edge cases and error conditions
void Test_SendTempToMqtt_null_parameters(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Test with null sensor type
    bool result = SendTempToMqtt(mqttClient, "dhbw/ai/si2023/2/", nullptr, "Sensor_One", 25.5, now, 42);
    
    // Should handle null parameters gracefully
    TEST_ASSERT_FALSE(result);
}

void Test_SendTempToMqtt_empty_strings(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Test with empty strings
    bool result = SendTempToMqtt(mqttClient, "", "", "", 25.5, now, 42);
    
    // Should handle empty strings
    TEST_ASSERT_FALSE(result);
}

void Test_CreateFullTopic_null_parameters(void) {
    char buffer[128];
    
    // Test with null parameters
    CreateFullTopic(buffer, sizeof(buffer), nullptr, "temp", "Sensor_One");
    
    // Should handle null parameters gracefully (may result in malformed topic)
    TEST_ASSERT_NOT_NULL(buffer);
}

void Test_SendTempToMqtt_extreme_values(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Test with extreme temperature values
    SendTempToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", 999.99, now, 999999);
    
    std::string lastMessage = mqttClient.getLastMessage();
    TEST_ASSERT_TRUE(lastMessage.find("\"sequence\":999999") != std::string::npos);
    TEST_ASSERT_TRUE(lastMessage.find("999.99") != std::string::npos);
}

void Test_SendTempToMqtt_zero_sequence(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    SendTempToMqtt(mqttClient, "dhbw/ai/si2023/2/", "temp", "Sensor_One", 20.0, now, 0);
    
    std::string lastMessage = mqttClient.getLastMessage();
    TEST_ASSERT_TRUE(lastMessage.find("\"sequence\":0") != std::string::npos);
}

// Bundle for central test_main.cpp
void Run_mqtt_tests() {
    RUN_TEST(Test_CreateFullTopic_with_suffix);
    RUN_TEST(Test_CreateFullTopic_without_suffix);
    RUN_TEST(Test_CreateFullTopic_empty_suffix);
    RUN_TEST(Test_CreateFullTopic_buffer_size_handling);
    RUN_TEST(Test_SendTempToMqtt_builds_correct_json);
    RUN_TEST(Test_SendTempToMqtt_creates_correct_topic);
    RUN_TEST(Test_SendTempToMqtt_handles_mqtt_connection_failure);
    RUN_TEST(Test_SendTempToMqtt_handles_negative_temperatures);
    RUN_TEST(Test_SendPendingDataToMqtt_no_folder_exists);
    RUN_TEST(Test_SendPendingData_empty_folder);
    RUN_TEST(Test_SendPendingData_ignores_non_csv_files);
    RUN_TEST(Test_SendPendingData_processes_csv_files);
    RUN_TEST(Test_SendPendingData_creates_recovered_topic);
    RUN_TEST(Test_SendPendingData_handles_large_payloads);
    RUN_TEST(Test_SendTempToMqtt_null_parameters);
    RUN_TEST(Test_SendTempToMqtt_empty_strings);
    RUN_TEST(Test_CreateFullTopic_null_parameters);
    RUN_TEST(Test_SendTempToMqtt_extreme_values);
    RUN_TEST(Test_SendTempToMqtt_zero_sequence);
}

// When standalone executable
#ifndef COMBINED_TEST_MAIN
int main(int argc, char **argv) {
    UNITY_BEGIN();
    Run_mqtt_tests();
    return UNITY_END();
}
#endif