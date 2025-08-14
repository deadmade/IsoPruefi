#include "platform.h"
#include <unity.h>
#include "storage.h"
#include "mock_json.h"

// Simulated timestamp: July 26, 2025, 14:55:00
DateTime now(2025, 7, 26, 14, 55, 0);

// Tests if the folder name is generated correctly
void test_createFolderName(void) {
    const char* result = createFolderName(now);
    TEST_ASSERT_NOT_NULL(result);
    TEST_ASSERT_EQUAL_STRING("2025", result);
}

// Tests if the filename is generated correctly
void test_createFilename(void) {
    char buffer[32];
    createFilename(buffer, sizeof(buffer), now);
    TEST_ASSERT_EQUAL_STRING("2025/07261455.csv", buffer);
}

// Tests behavior at year change
void test_filename_end_of_year(void) {
    DateTime testTime(2023, 12, 31, 23, 59, 0);
    char buffer[32];
    createFilename(buffer, sizeof(buffer), testTime);
    TEST_ASSERT_EQUAL_STRING("2023/12312359.csv", buffer);
}

// Tests if JSON is built correctly with expected fields
void test_buildJson_sets_expected_fields(void) {
    JsonDocument doc;
    float temperature = 23.5f;
    int sequence = 42;
    
    buildJson(doc, temperature, now, sequence);
    
    // Test sequence field
    TEST_ASSERT_EQUAL_STRING("42", doc["sequence"].c_str());
    
    // Test timestamp field (Unix timestamp)
    TEST_ASSERT_EQUAL_STRING("1753778400", doc["timestamp"].c_str());
    
    // Test value array
    TEST_ASSERT_EQUAL(1, doc.size("value"));
    
    // Test meta array
    TEST_ASSERT_EQUAL(1, doc.size("meta"));
}

// Tests JSON building with different temperature values
void test_buildJson_different_temperatures(void) {
    JsonDocument doc;
    
    // Test negative temperature
    buildJson(doc, -10.5f, now, 1);
    TEST_ASSERT_EQUAL_STRING("1", doc["sequence"].c_str());
    
    // Test zero temperature
    doc.clear();
    buildJson(doc, 0.0f, now, 2);
    TEST_ASSERT_EQUAL_STRING("2", doc["sequence"].c_str());
    
    // Test high temperature
    doc.clear();
    buildJson(doc, 85.7f, now, 3);
    TEST_ASSERT_EQUAL_STRING("3", doc["sequence"].c_str());
}

// Tests filename creation with edge cases
void test_createFilename_edge_cases(void) {
    char buffer[32];
    
    // Test start of year
    DateTime startYear(2025, 1, 1, 0, 0, 0);
    createFilename(buffer, sizeof(buffer), startYear);
    TEST_ASSERT_EQUAL_STRING("2025/01010000.csv", buffer);
    
    // Test leap year
    DateTime leapYear(2024, 2, 29, 12, 30, 0);
    createFilename(buffer, sizeof(buffer), leapYear);
    TEST_ASSERT_EQUAL_STRING("2024/02291230.csv", buffer);
}

// Tests recovered filename creation
void test_createRecoveredFilename(void) {
    char recoveredFilename[64];
    
    createRecoveredFilename(recoveredFilename, sizeof(recoveredFilename), now, 13);
    TEST_ASSERT_EQUAL_STRING("2025/07261455_recovered.json", recoveredFilename);
    
    // Test with custom suffix
    createRecoveredFilename(recoveredFilename, sizeof(recoveredFilename), now, 13, "_backup.json");
    TEST_ASSERT_EQUAL_STRING("2025/07261455_backup.json", recoveredFilename);
}

// Tests folder name creation across different years
void test_createFolderName_different_years(void) {
    DateTime year2020(2020, 6, 15, 10, 30, 0);
    DateTime year2030(2030, 12, 25, 23, 59, 0);
    
    TEST_ASSERT_EQUAL_STRING("2020", createFolderName(year2020));
    TEST_ASSERT_EQUAL_STRING("2030", createFolderName(year2030));
}
// Optional: Bundle for central test_main.cpp
void run_storage_tests() {
    RUN_TEST(test_createFolderName);
    RUN_TEST(test_createFilename);
    RUN_TEST(test_filename_end_of_year);
    RUN_TEST(test_buildJson_sets_expected_fields);
    RUN_TEST(test_buildJson_different_temperatures);
    RUN_TEST(test_createFilename_edge_cases);
    RUN_TEST(test_createRecoveredFilename);
    RUN_TEST(test_createFolderName_different_years);
}
// When standalone executable
#ifndef COMBINED_TEST_MAIN
int main(int argc, char **argv) {
    UNITY_BEGIN();
    run_storage_tests();
    return UNITY_END();
}
#endif

void setUp(void) {}
void tearDown(void) {}

