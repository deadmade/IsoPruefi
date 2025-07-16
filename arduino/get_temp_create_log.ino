#include <Wire.h>
#include <RTClib.h>
#include <SdFat.h>
#include "Adafruit_ADT7410.h"

// Real-time clock (RTC)
RTC_DS3231 rtc;

// SD card
SdFat sd;
const uint8_t chipSelect = 4;

// Temperature sensor
Adafruit_ADT7410 tempsensor = Adafruit_ADT7410();

// FAT timestamp provider for SD library
void dateTime(uint16_t* date, uint16_t* time) {
  DateTime now = rtc.now();
  *date = FAT_DATE(now.year(), now.month(), now.day());
  *time = FAT_TIME(now.hour(), now.minute(), now.second());
}

// Store last logged minute
int lastLoggedMinute = -1;

void setup() {
  Serial.begin(9600);
  while (!Serial);

  // Initialize RTC
  if (!rtc.begin()) {
    Serial.println("RTC not found!");
    while (1);
  }

  if (rtc.lostPower()) {
    Serial.println("RTC lost power – setting time!");
    rtc.adjust(DateTime(F(__DATE__), F(__TIME__)));
  }

  // Initialize SD card
  SdFile::dateTimeCallback(dateTime);
  if (!sd.begin(chipSelect, SD_SCK_MHZ(25))) {
    Serial.println("SD card initialization failed.");
    while (1);
  }

  // Initialize temperature sensor
  if (!tempsensor.begin()) {
    Serial.println("ADT7410 not found!");
    while (1);
  }

  delay(250); // Wait for sensor to be ready
  tempsensor.setResolution(ADT7410_16BIT);

  Serial.println("Setup complete.");
}

void loop() {
  DateTime now = rtc.now();

  // Log once per new minute
  if (now.minute() != lastLoggedMinute) {
    lastLoggedMinute = now.minute();

    // Read temperature in Celsius and Fahrenheit
    float c = tempsensor.readTempC();
    float f = c * 9.0 / 5.0 + 32;

    Serial.print("Logging temp at ");
    Serial.print(now.timestamp());
    Serial.print(": ");
    Serial.print(c);
    Serial.println(" °C");

    // Folder name: 4-digit year (e.g., "2025")
    char folderName[8];
    snprintf(folderName, sizeof(folderName), "%04d", now.year());

    // Create folder if it doesn't exist
    if (!sd.exists(folderName)) {
      if (!sd.mkdir(folderName)) {
        Serial.println("Failed to create folder.");
        return;
      }
    }

    // Filename: MMDDHHMM.TXT (e.g., 07151630.TXT)
    char filename[FILENAME_BUFFER_SIZE];
    snprintf(filename, FILENAME_BUFFER_SIZE, "%s/%02d%02d%02d%02d.TXT",
             folderName, now.month(), now.day(), now.hour(), now.minute());

    // Create and write new file
    File file = sd.open(filename, FILE_WRITE);
    if (file) {
      file.println("Timestamp,Celsius,Fahrenheit");
      file.print(now.timestamp());
      file.print(",");
      file.print(c, 2);
      file.print(",");
      file.println(f, 2);
      file.close();
      Serial.println("File written successfully.");
    } else {
      Serial.println("Failed to create file.");
    }
  }

  // Small delay to avoid busy loop
  delay(1000);
}