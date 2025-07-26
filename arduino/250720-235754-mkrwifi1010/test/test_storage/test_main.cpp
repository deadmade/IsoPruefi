#include <unity.h>
#include "storage.h"

// Simulierter Zeitpunkt: 26. Juli 2025, 14:55
DateTime now(2025, 7, 26, 14, 55, 0);

void test_createFolderName(void) {
    const char* result = createFolderName(now);
    TEST_ASSERT_EQUAL_STRING("2025", result);
}

void test_createFilename(void) {
    char buffer[32];
    createFilename(buffer, sizeof(buffer), now);
    TEST_ASSERT_EQUAL_STRING("2025/07261455.json", buffer);
}

void setUp(void) {}
void tearDown(void) {}

int main(int argc, char **argv) {
    UNITY_BEGIN();
    RUN_TEST(test_createFolderName);
    RUN_TEST(test_createFilename);
    return UNITY_END();
}
