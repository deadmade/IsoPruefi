# Contribute & Build

## How do I contribute as a developer?

<p style="color: red;"><b>READ THIS GUIDE BEFORE CONTRIBUTING</b></p>

Since our project is secured by two pre-commit hocks, it is important to set up the project correctly before contributing.

This is done as followed:

Clone the project

```git clone https://github.com/deadmade/IsoPruefi.git```

Make sure you have installed the following packages globally.

- <a href="https://www.python.org/">Python</a>: Needed for MkDocs
- <a href="https://www.npmjs.com/">Node Package Manager</a>: Used to install needed dependencies for pre-commit hooks
- <a href="https://dotnet.microsoft.com/en-us/download">.NET 9.0 SDK</a>: Used for our Rest-API
- <a href="https://www.docker.com/">Docker</a>

After you've cloned the repo make sure to install all needed packages for the hooks via:

```npm i```

and run:

```npm run init```

Now it should be configured 🚀

To get the development environment up and running, follow these steps:

1. Open a terminal, navigate to the `IsoPrüfi` directory, and run:

   ```bash
   docker compose up
   ```

2. Once the containers are running, create an admin token for InfluxDB:

   ```bash
   docker exec -it influxdb influxdb3 create token --admin
   ```

3. Copy the generated token string.

4. Create a `config.json` file at the following location:

   ```
   IsoPruefi/isopruefi-docker/influx/explorer/config
   ```

5. Add the following content to `config.json`, replacing `"your-token-here"` with the copied token:

   ```json
   {
     "DEFAULT_INFLUX_SERVER": "http://host.docker.internal:8181",
     "DEFAULT_INFLUX_DATABASE": "IsoPrüfi",
     "DEFAULT_API_TOKEN": "your-token-here",
     "DEFAULT_SERVER_NAME": "IsoPrüfi"
   }
   ```

6. Run dotnet user-secrets set "Influx:InfluxDBToken" "<Token>" --project <Path to .csproj>
   
7. Pase InfluxDBToken in secrets.env

8. Restart the Containers

---

## Arduino Set Up

### Hardware

- <a href="https://docs.arduino.cc/hardware/mkr-wifi-1010/#features">MKR WiFi 1010</a>
- <a href="https://learn.adafruit.com/adt7410-breakout?view=all">Analog Devices ADT7410 Breakout</a>
- <a href="https://randomnerdtutorials.com/guide-for-real-time-clock-rtc-module-with-arduino-ds1307-and-ds3231/">DS3231 RTC</a>
- <a href="https://randomnerdtutorials.com/guide-to-sd-card-module-with-arduino/">SD Card Module</a>

### Software

`⚠️ Important: Always open the Arduino firmware folder (e.g., code/arduino/) as a PlatformIO Project (via Open Project or Pick a folder in the PlatformIO sidebar).
Otherwise, dependencies from platformio.ini might not be detected and you may see false errors in the editor.`

To work on the Arduino/PlatformIO part of the project:

1. Install the PlatformIO Extension in Visual Studio Code

2. Open the folder IsoPruefi/isopruefi-arduino

3. Build and upload the firmware using the PlatformIO toolbar or PlatformIO terminal

4. Make sure your board is connected and properly selected in platformio.ini

```
[env:mkrwifi1010]
platform = atmelsam
board = mkrwifi1010
framework = arduino
lib_deps = 
	arduino-libraries/WiFiNINA
	adafruit/Adafruit ADT7410 Library
	adafruit/RTClib
	arduino-libraries/ArduinoMqttClient
	greiman/SdFat
	gyverlibs/UnixTime
	bblanchon/ArduinoJson@^7.4.2
```

Tips:

- PlatformIO installs the required libraries automatically on first build

- To run the programm run `pio run -e mkrwifi1010` in the PlatformIO terminal

- To flash the Arudion with new code run `pio run -e mkrwifi1010 --target upload` in the PlatformIO terminal

- The main firmware entry point is located at src/main.cpp

- Use the Serial Monitor (🔌) to debug via USB

- To run the all unit tests run `pio test -e native` in the PlatformIO terminal

---

Happy Coding 😊
