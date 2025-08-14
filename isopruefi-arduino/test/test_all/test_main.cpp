#define COMBINED_TEST_MAIN
#include "platform.h"
#include <unity.h>

// Include all test modules
#include "../test_storage/test_main.cpp"
#include "../test_core/test_main.cpp"
#include "../test_sensor_clean/test_main.cpp"
#include "../test_mqtt_clean/test_main.cpp"
#include "../test_network_clean/test_main.cpp"

// Declare external test runner functions
extern void run_storage_tests();
extern void run_core_tests();
extern void run_sensor_tests();
extern void run_mqtt_tests();
extern void run_network_tests();

int main(int argc, char **argv) {
    UNITY_BEGIN();
    
    // Run all test suites
    printf("\n=== Running Storage Tests ===\n");
    run_storage_tests();
    
    printf("\n=== Running Core Tests ===\n");
    run_core_tests();
    
    printf("\n=== Running Sensor Tests ===\n");
    run_sensor_tests();
    
    printf("\n=== Running MQTT Tests ===\n");
    run_mqtt_tests();
    
    printf("\n=== Running Network Tests ===\n");
    run_network_tests();
    
    printf("\n=== All Tests Complete ===\n");
    return UNITY_END();
}