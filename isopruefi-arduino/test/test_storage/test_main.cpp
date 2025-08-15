#include <ArduinoFake.h>
#include <unity.h>
#include "storage.h"

using namespace fakeit;

void setUp(void) {
    ArduinoFakeReset();
    sd.clearTestFiles();
    When(OverloadedMethod(ArduinoFake(Serial), print, size_t(const char[]))).AlwaysReturn(1);
    When(OverloadedMethod(ArduinoFake(Serial), println, size_t(const char[]))).AlwaysReturn(1);
    When(OverloadedMethod(ArduinoFake(Serial), print, size_t(const String&))).AlwaysReturn(1);
    When(OverloadedMethod(ArduinoFake(Serial), println, size_t(const String&))).AlwaysReturn(1);
}

void tearDown(void) {
    ArduinoFakeReset();
}

// Test helper functions
void test_createFolderName(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    const char* result = createFolderName(now);
    TEST_ASSERT_NOT_NULL(result);
    TEST_ASSERT_EQUAL_STRING("2025", result);
}

void test_createFilename(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    char buffer[32];
    createFilename(buffer, sizeof(buffer), now);
    TEST_ASSERT_EQUAL_STRING("2025/07261455.csv", buffer);
}

void test_filename_end_of_year(void) {
    DateTime testTime(2023, 12, 31, 23, 59, 0);
    char buffer[32];
    createFilename(buffer, sizeof(buffer), testTime);
    TEST_ASSERT_EQUAL_STRING("2023/12312359.csv", buffer);
}

// Test saveToCsvBatch function with ArduinoFake mocking
void test_saveToCsvBatch_creates_folder_when_not_exists(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Setup: folder doesn't exist initially
    TEST_ASSERT_FALSE(sd.exists("2025"));
        
    // Call the function
    saveToCsvBatch(now, 25.5, 42);
    
    // Verify folder was created
    TEST_ASSERT_TRUE(sd.exists("2025"));
}

void test_saveToCsvBatch_writes_csv_data(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    // Setup: folder exists
    sd.addTestFile("2025");
        
    // Call the function
    saveToCsvBatch(now, 25.12345, 42);
    
    // Verify the CSV file was created
    TEST_ASSERT_TRUE(sd.exists("2025/07261455.csv"));
}

void test_buildJson_creates_correct_structure(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    JsonDocument doc;
    
    // Call the function (using the native test version)
    buildJson(doc, 25.12345, now, 42);
    
    // Verify JSON structure (native test version uses strings)
    TEST_ASSERT_EQUAL_STRING(std::to_string(42).c_str(), doc["sequence"].c_str());
    TEST_ASSERT_EQUAL_STRING(std::to_string(now.unixtime()).c_str(), doc["timestamp"].c_str());
    
    // Check that the arrays were created
    TEST_ASSERT_TRUE(doc.containsKey("value"));
    TEST_ASSERT_TRUE(doc.containsKey("meta"));
}

void test_buildJson_clears_previous_data(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    JsonDocument doc;
    
    // Add some initial data
    doc["oldKey"] = "oldValue";
    doc["timestamp"] = "999";
    
    // Call the function
    buildJson(doc, 25.5, now, 10);
    
    // Verify old data is cleared
    TEST_ASSERT_FALSE(doc.containsKey("oldKey"));
    TEST_ASSERT_EQUAL_STRING(std::to_string(now.unixtime()).c_str(), doc["timestamp"].c_str());
    TEST_ASSERT_EQUAL_STRING(std::to_string(10).c_str(), doc["sequence"].c_str());
}

void test_deleteCsvFile_success(void) {
    const char* testFile = "2025/test.csv";
    
    // Setup: file exists
    sd.addTestFile(testFile);
    TEST_ASSERT_TRUE(sd.exists(testFile));
    
    // Call the function
    deleteCsvFile(testFile);
    
    // Verify file was deleted
    TEST_ASSERT_FALSE(sd.exists(testFile));
}

void test_deleteCsvFile_file_not_exists(void) {
    const char* testFile = "2025/nonexistent.csv";
    
    // Setup: file doesn't exist
    TEST_ASSERT_FALSE(sd.exists(testFile));
        
    // Call the function (should not crash)
    deleteCsvFile(testFile);
    
    // File still doesn't exist
    TEST_ASSERT_FALSE(sd.exists(testFile));
}

void test_buildRecoveredJsonFromCsv_structure(void) {
    DateTime now(2025, 7, 26, 14, 55, 0);
    
    sd.addTestFile("2025");

    const char* path = "2025/07261455.csv";

    sd.addTestFile(path, "1721995200,23.5,1\n1721995260,24.0,2\n");

    JsonDocument doc;
    buildRecoveredJsonFromCsv(doc, path, now);

    TEST_ASSERT_EQUAL_STRING(std::to_string(now.unixtime()).c_str(), doc["timestamp"].c_str());
    TEST_ASSERT_EQUAL_STRING("null", doc["sequence"].c_str());

    JsonArray& valueArr = doc["value"].to<JsonArray>();
    TEST_ASSERT_EQUAL(1, (int)valueArr.size());
    TEST_ASSERT_EQUAL_STRING("null", valueArr[0].c_str());

    JsonObject& meta = doc["meta"].to<JsonObject>();
    JsonArray& t = meta["t"].to<JsonArray>();
    JsonArray& v = meta["v"].to<JsonArray>();
    JsonArray& s = meta["s"].to<JsonArray>();
    TEST_ASSERT_EQUAL(2, (int)t.size());
    TEST_ASSERT_EQUAL(2, (int)v.size());
    TEST_ASSERT_EQUAL(2, (int)s.size());
}




// Bundle for central test_main.cpp
void run_storage_tests() {
    RUN_TEST(test_createFolderName);
    RUN_TEST(test_createFilename);
    RUN_TEST(test_filename_end_of_year);
    RUN_TEST(test_saveToCsvBatch_creates_folder_when_not_exists);
    RUN_TEST(test_saveToCsvBatch_writes_csv_data);
    RUN_TEST(test_buildJson_creates_correct_structure);
    RUN_TEST(test_buildJson_clears_previous_data);
    RUN_TEST(test_deleteCsvFile_success);
    RUN_TEST(test_deleteCsvFile_file_not_exists);
    RUN_TEST(test_buildRecoveredJsonFromCsv_structure);
}

// When standalone executable
#ifndef COMBINED_TEST_MAIN
int main(int argc, char **argv) {
    UNITY_BEGIN();
    run_storage_tests();
    return UNITY_END();
}
#endif