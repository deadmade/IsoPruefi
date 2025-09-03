# Database Schema

## Overview

IsoPrüfi uses Entity Framework Core with PostgreSQL for data persistence. The schema includes user management, geographic mapping, and MQTT topic configuration.

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