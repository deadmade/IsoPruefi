# Database Schema

## Overview

IsoPrüfi uses a dual database approach:
- **PostgreSQL** with Entity Framework Core for relational data (users, settings, coordinates)
- **InfluxDB** for time-series sensor data and metrics

## Core Entities

### ApiUser
Extends ASP.NET Core Identity for user authentication.

### CoordinateMapping
Stores geographic coordinates for postal codes with usage tracking.

| Column | Type | Description |
|--------|------|-------------|
| PostalCode (PK) | int | Unique postal code identifier |
| Location | string | Location name |
| Latitude | double | Geographic latitude |
| Longitude | double | Geographic longitude |
| LastUsed | DateTime? | When postal code was last updated |
| LockedUntil | DateTime? | Temporary locked to get the current Temperature of the Location |

### TopicSetting
MQTT topic configuration with sensor metadata.

| Column | Type | Description |
|--------|------|-------------|
| TopicSettingId (PK) | int | Auto-generated identifier |
| CoordinateMappingId (FK) | int | Links to CoordinateMapping |
| DefaultTopicPath | string | MQTT topic prefix (max 100 chars) |
| GroupId | int | Group identifier |
| SensorTypeEnum | SensorType | Type of sensor |
| SensorName | string | Sensor name (max 50 chars) |
| SensorLocation | string | Sensor location (max 50 chars) |
| HasRecovery | bool | Recovery feature enabled |

## Enumerations

### SensorType
Available sensor types:
- `temp` - Temperature
- `spl` - Sound pressure level
- `hum` - Humidity
- `ikea` - IKEA sensor
- `co2` - CO2 sensor
- `mic` - Microphone

## Relationships

- `TopicSetting` → `CoordinateMapping` (Many-to-One)
- `ApiUser` uses ASP.NET Identity tables

## Migration Commands

```bash
cd isopruefi-backend
dotnet ef migrations add <MigrationName> --project ./Database/Database.csproj --startup-project ./Rest-API/Rest-API.csproj
```

## InfluxDB Schema

### Measurements

#### temperature
Sensor temperature readings from Arduino devices.

| Field/Tag | Type | Description |
|-----------|------|-------------|
| value (field) | double | Temperature measurement |
| sensor (tag) | string | Sensor identifier |
| sequence (tag) | string | Message sequence number |
| timestamp | DateTime | Measurement timestamp |

#### outside_temperature  
External weather data from APIs.

| Field/Tag | Type | Description |
|-----------|------|-------------|
| value (field) | double | Temperature in Celsius |
| value_fahrenheit (field) | double | Temperature in Fahrenheit |
| postalcode (field) | int | Associated postal code |
| place (tag) | string | Location name |
| website (tag) | string | Data source API |
| timestamp | DateTime | Measurement timestamp |

#### uptime
Arduino device availability tracking.

| Field/Tag | Type | Description |
|-----------|------|-------------|
| sensor (field) | string | Sensor identifier |
| timestamp | DateTime | Uptime check timestamp |

### Configuration

- **Database**: `IsoPruefi` (configurable)
- **Connection**: Token-based authentication
- **Client**: InfluxDB3 .NET client