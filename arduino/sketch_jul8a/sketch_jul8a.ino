#include <Wire.h>
#include "Adafruit_ADT7410.h"

#include <ArduinoMqttClient.h>
#if defined(ARDUINO_SAMD_MKRWIFI1010) || defined(ARDUINO_SAMD_NANO_33_IOT) || defined(ARDUINO_AVR_UNO_WIFI_REV2)
  #include <WiFiNINA.h>
#elif defined(ARDUINO_SAMD_MKR1000)
  #include <WiFi101.h>
#elif defined(ARDUINO_ARCH_ESP8266)
  #include <ESP8266WiFi.h>
#elif defined(ARDUINO_PORTENTA_H7_M7) || defined(ARDUINO_NICLA_VISION) || defined(ARDUINO_ARCH_ESP32) || defined(ARDUINO_GIGA) || defined(ARDUINO_OPTA)
  #include <WiFi.h>
#elif defined(ARDUINO_PORTENTA_C33)
  #include <WiFiC3.h>
#elif defined(ARDUINO_UNOR4_WIFI)
  #include <WiFiS3.h>
#endif

// Create the ADT7410 temperature sensor object
Adafruit_ADT7410 tempsensor = Adafruit_ADT7410();

#include "secrets.h"
///////please enter your sensitive data in the Secret tab/arduino_secrets.h
char ssid[] = SECRET_SSID;    // your network SSID (name)
char pass[] = SECRET_PASS;    // your network password (use for WPA, or use as key for WEP)

WiFiClient wifiClient;
MqttClient mqttClient(wifiClient);

const char broker[] = "10.43.100.127";
int        port     = 1883;
const char topic[]  = "dhbw/ai/si2023/2/";
const char sensorIdOne[] = "SensorEINSCH";
const char sensorIdTwo[] = "SensorZWEI";


const long interval = 1000;
unsigned long previousMillis = 0;

int count = 0;

void setup() {
  Serial.begin(115200);
  while (!Serial) {
    ; // wait for serial port to connect. Needed for native USB port only
  }

  Serial.println("CheckTemp");
  CheckTempSens();

  Serial.println("ConnectWifi");
  ConnectToWifi();

  Serial.println("ConnectMQTT");
  ConnectToMqtt();
}


void loop() {
 float c = GetTemp();
 Serial.println(c);

 SendToMqtt(c, "temp");

  //wait 1000ms (1s)
  delay(1000);
}

void CheckTempSens()
{
  // Make sure the sensor is found, you can also pass in a different i2c
  // address with tempsensor.begin(0x49) for example
  if (!tempsensor.begin()) {
    Serial.println("Couldn't find ADT7410!");
    while (1);
  }

  // sensor takes 250 ms to get first readings
  delay(250);
  tempsensor.setResolution(ADT7410_16BIT);
}

float GetTemp()
{
  float c = tempsensor.readTempC();
  return c;
}

void ConnectToWifi()
{
  // attempt to connect to WiFi network:
  Serial.print("Attempting to connect to WPA SSID: ");
  Serial.println(ssid);
  while (WiFi.begin(ssid, pass) != WL_CONNECTED) {
    // failed, retry
    Serial.print(".");
    delay(5000);
  }

  Serial.println("You're connected to the network");
  Serial.println();
}

void ConnectToMqtt()
{
  Serial.print("Attempting to connect to the MQTT broker: ");
  Serial.println(broker);

  if (!mqttClient.connect(broker, port)) {
    Serial.print("MQTT connection failed! Error code = ");
    Serial.println(mqttClient.connectError());

    while (1);
  }

  Serial.println("You're connected to the MQTT broker!");
  Serial.println();
}

void SendToMqtt(float c, char* sensorType)
{
  // call poll() regularly to allow the library to send MQTT keep alives which
  // avoids being disconnected by the broker
  mqttClient.poll();

  // to avoid having delays in loop, we'll use the strategy from BlinkWithoutDelay
  // see: File -> Examples -> 02.Digital -> BlinkWithoutDelay for more info
  unsigned long currentMillis = millis();
  
  if (currentMillis - previousMillis >= interval) {
    // save the last time a message was sent
    previousMillis = currentMillis;

    char fullTopic[64]; // Adjust size as needed

      // Construct "topic/sensorType/sensorId"
    strcpy(fullTopic, topic);
    strcat(fullTopic, "/");
    strcat(fullTopic, sensorType);
    strcat(fullTopic, "/");
    strcat(fullTopic, sensorIdOne);

    mqttClient.beginMessage(fullTopic);

      // Construct JSON payload
    mqttClient.print("{\"temperature\":");
    mqttClient.print(c);
    mqttClient.print(",\"sequence\":");
    mqttClient.print(count);
    mqttClient.print("}");

    mqttClient.endMessage();

    mqttClient.endMessage();


    Serial.println();

    count++;
  }
}