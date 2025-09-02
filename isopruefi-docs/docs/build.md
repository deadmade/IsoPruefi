# Contribute & Build

## Prerequisites

Make sure you have installed the following packages globally:

- [Python](https://www.python.org/) - Needed for MkDocs (3.8 or higher)
- [Node Package Manager](https://www.npmjs.com/) - Used to install dependencies for pre-commit hooks
- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download) - Used for our REST API
- [Docker](https://www.docker.com/) - For containerized development

## Documentation Setup

To work on the documentation:

1. **Navigate to docs directory:**
   ```bash
   cd isopruefi-docs
   ```

2. **Install Python dependencies:**
   ```bash
   pip install -r requirements.txt
   ```

3. **Serve documentation locally:**
   ```bash
   mkdocs serve
   ```

4. **Build documentation:**
   ```bash
   mkdocs build
   ```

The documentation will be available at `http://localhost:8000` with hot reload for development.

## Initial Setup

!!! warning "Important"
    READ THIS GUIDE BEFORE CONTRIBUTING

Since our project is secured by two pre-commit hooks, it is important to set up the project correctly before contributing.

1. **Clone the project:**
   ```bash
   git clone https://github.com/deadmade/IsoPruefi.git
   ```

2. **Install dependencies:**
   ```bash
   npm i
   ```

3. **Initialize the project:**
   ```bash
   npm run init
   ```

Now it should be configured 🚀

## Development Environment

To get the development environment up and running:

1. **Start the containers:**
   ```bash
   docker compose up
   ```

2. **Create an InfluxDB admin token:**
   ```bash
   docker exec -it influxdb influxdb3 create token --admin
   ```

3. **Copy the generated token string.**

4. **Create configuration file:**
   
   Create `IsoPruefi/isopruefi-docker/influx/explorer/config/config.json`:
   ```json
   {
     "DEFAULT_INFLUX_SERVER": "http://host.docker.internal:8181",
     "DEFAULT_INFLUX_DATABASE": "IsoPrüfi",
     "DEFAULT_API_TOKEN": "your-token-here",
     "DEFAULT_SERVER_NAME": "IsoPrüfi"
   }
   ```

5. **Set user secrets:**
   ```bash
   dotnet user-secrets set "Influx:InfluxDBToken" "<Token>" --project isopruefi-backend\MQTT-Receiver-Worker\MQTT-Receiver-Worker.csproj
   ```

6. **Restart the containers**

## Arduino Setup

### Hardware Requirements

- [MKR WiFi 1010](https://docs.arduino.cc/hardware/mkr-wifi-1010/#features)
- [Analog Devices ADT7410 Breakout](https://learn.adafruit.com/adt7410-breakout?view=all)
- [DS3231 RTC](https://randomnerdtutorials.com/guide-for-real-time-clock-rtc-module-with-arduino-ds1307-and-ds3231/)
- [SD Card Module](https://randomnerdtutorials.com/guide-to-sd-card-module-with-arduino/)

### Software Setup

!!! warning "Important"
    Always open the Arduino firmware folder (`isopruefi-arduino/`) as a PlatformIO Project via "Open Project" or "Pick a folder" in the PlatformIO sidebar. Otherwise, dependencies from `platformio.ini` might not be detected.

1. **Install PlatformIO Extension** in Visual Studio Code

2. **Open the folder** `isopruefi-arduino/`

3. **Build and upload** using PlatformIO toolbar or terminal

4. **Verify board configuration** in `platformio.ini`:
   ```ini
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

### PlatformIO Commands

- **Build:** `pio run -e mkrwifi1010`
- **Upload:** `pio run -e mkrwifi1010 --target upload`
- **Run tests:** `pio test -e native`
- **Serial monitor:** Use the Serial Monitor (🔌) in VS Code

## Testing

- **Backend tests:** `cd isopruefi-backend && dotnet test`
- **Frontend tests:** `cd isopruefi-frontend && npm test`
- **Arduino tests:** `cd isopruefi-arduino && pio test -e native`

## Troubleshooting

See our [troubleshooting guide](troubleshooting.md) for common issues and solutions.

---

Happy Coding! 😊
