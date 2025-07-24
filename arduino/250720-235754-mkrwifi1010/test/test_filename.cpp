#include <string>
#include <cstdio>
#include <iostream>
#include <cassert>

#define FILENAME_BUFFER_SIZE 32

// Simple struct as a replacement for DateTime (mock)
struct MockDateTime {
  int year, month, day, hour, minute;

  int year_() const { return year; }
  int month_() const { return month; }
  int day_() const { return day; }
  int hour_() const { return hour; }
  int minute_() const { return minute; }
};

// filename generation logic for testing
std::string generateFilename(const MockDateTime& now) {
  char folderName[8];
  snprintf(folderName, sizeof(folderName), "%04d", now.year_());

  char filename[FILENAME_BUFFER_SIZE];
  snprintf(filename, FILENAME_BUFFER_SIZE, "%s/%02d%02d%02d%02d.json",
           folderName, now.month_(), now.day_(), now.hour_(), now.minute_());
  
  return std::string(filename);
}

// TESTS
int main() {
  {
    MockDateTime testTime = {2024, 3, 15, 14, 30};
    std::string result = generateFilename(testTime);
    assert(result == "2024/03151430.json");
    std::cout << "Test 1 passed: " << result << std::endl;
  }
  {
    MockDateTime testTime = {2024, 12, 31, 23, 59};
    std::string result = generateFilename(testTime);
    assert(result.length() < FILENAME_BUFFER_SIZE);
    std::cout << "Test 2 passed: " << result << std::endl;
  }
  return 0;
}
