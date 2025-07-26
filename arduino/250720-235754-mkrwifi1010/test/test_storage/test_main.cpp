#include <unity.h>
#include "storage.h"

// Für native Tests: verwende MockDateTime aus mock_datetime.h
#ifdef UNIT_TEST
#include "mock_datetime.h"
using DateTime = MockDateTime;
#endif

// Simulierter Zeitpunkt: 26. Juli 2025, 14:55:00
DateTime now(2025, 7, 26, 14, 55, 0);

// Testet, ob der Ordnername korrekt erzeugt wird
void test_createFolderName(void) {
    const char* result = createFolderName(now);
    TEST_ASSERT_NOT_NULL(result);
    TEST_ASSERT_EQUAL_STRING("2025", result);
}

// Testet, ob der Dateiname korrekt erzeugt wird
void test_createFilename(void) {
    char buffer[32];
    createFilename(buffer, sizeof(buffer), now);
    TEST_ASSERT_EQUAL_STRING("2025/07261455.json", buffer);
}

// Testet Verhalten bei Jahreswechsel
void test_filename_end_of_year(void) {
    DateTime testTime(2023, 12, 31, 23, 59, 0);
    char buffer[32];
    createFilename(buffer, sizeof(buffer), testTime);
    TEST_ASSERT_EQUAL_STRING("2023/12312359.json", buffer);
}

// Optional: Bündeln für zentrale test_main.cpp
void run_storage_tests() {
    RUN_TEST(test_createFolderName);
    RUN_TEST(test_createFilename);
    RUN_TEST(test_filename_end_of_year);
}

// Wenn alleinstehend ausführbar
#ifndef COMBINED_TEST_MAIN
int main(int argc, char **argv) {
    UNITY_BEGIN();
    run_storage_tests();
    return UNITY_END();
}
#endif

void setUp(void) {}
void tearDown(void) {}

