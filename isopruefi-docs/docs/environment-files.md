# Environment Files

## Overview

The IsoPruefi project uses environment files to manage configuration across different deployment environments:

- **Development Environment**: `secrets.env` (project root)
- **Live Environment**: `isopruefi-docker-live/secrets.env`
- **Backup Configuration**: `isopruefi-docker-live/backup/*.env`

---

## Main Environment Files (`secrets.env`)

### Development Environment (`./secrets.env`)

Used by the development Docker Compose setup for local development with localhost domains.

```bash
# ===== Database Configuration =====
# PostgreSQL Database Settings
POSTGRES_HOST=postgres
POSTGRES_PORT=5432
POSTGRES_DATABASE=Isopruefi
POSTGRES_USERNAME=Isopruefi
POSTGRES_PASSWORD=secret

# Admin user for initial system setup
Admin__UserName=admin
Admin__Email=admin@localhost.dev
Admin__Password=DevAdmin123!

# Connection String (uses above variables)
ConnectionStrings__DefaultConnection=Host=${POSTGRES_HOST};Port=${POSTGRES_PORT};Database=${POSTGRES_DATABASE};Username=${POSTGRES_USERNAME};Password=${POSTGRES_PASSWORD}

# ===== Authentication =====
# JWT Configuration for API authentication
Jwt__ValidIssuer=localhost
Jwt__ValidAudience=localhost
Jwt__Secret=your-secure-256-bit-secret-key-replace-this-in-production

# ===== Time-Series Database =====
# InfluxDB Settings for time-series data storage
Influx__InfluxDBHost=http://influxdb3:8181
Influx__InfluxDBToken=
Influx__InfluxDBDatabase=IsoPruefi

# ===== MQTT Configuration =====
# MQTT Broker settings for IoT sensor communication
Mqtt__BrokerHost=aicon.dhbw-heidenheim.de
Mqtt__BrokerPort=1883

# ===== Weather API Configuration =====
# External weather service URLs and location
Weather__OpenMeteoApiUrl=https://api.open-meteo.com/v1/forecast?latitude=48.678&longitude=10.1516&models=icon_seamless&current=temperature_2m
Weather__BrightSkyApiUrl=https://api.brightsky.dev/current_weather?lat=48.67&lon=10.1516
Weather__Location=Heidenheim

# ===== Frontend =====
VITE_API_BASE_URL=https://backend.localhost
```

### Live Environment (`isopruefi-docker-live/secrets.env`)

Used by the production Docker Compose setup with additional production-specific configurations.

```bash
# ===== Database Configuration =====
# PostgreSQL Database Settings
POSTGRES_HOST=postgres
POSTGRES_PORT=5432
POSTGRES_DB=Isopruefi          # Note: Different variable name for production
POSTGRES_USER=Isopruefi        # Note: Different variable name for production  
POSTGRES_PASSWORD=

# Admin user for initial system setup
Admin__UserName=admin
Admin__Email=admin@localhost.dev
Admin__Password=DevAdmin123!

# Connection String (uses above variables)
ConnectionStrings__DefaultConnection=Host=${POSTGRES_HOST};Port=${POSTGRES_PORT};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}

# ===== Authentication =====
# JWT Configuration for API authentication
Jwt__ValidIssuer=localhost
Jwt__ValidAudience=localhost
Jwt__Secret=your-secure-256-bit-secret-key-replace-this-in-production

# ===== Time-Series Database =====
# InfluxDB Settings for time-series data storage
Influx__InfluxDBHost=http://influxdb3:8181
Influx__InfluxDBToken=
Influx__InfluxDBDatabase=IsoPruefi

# ===== MQTT Configuration =====
# MQTT Broker settings for IoT sensor communication
Mqtt__BrokerHost=aicon.dhbw-heidenheim.de
Mqtt__BrokerPort=1883

# ===== Weather API Configuration =====
# External weather service URLs and location
Weather__OpenMeteoApiUrl=https://api.open-meteo.com/v1/forecast?latitude=48.678&longitude=10.1516&models=icon_seamless&current=temperature_2m
Weather__BrightSkyApiUrl=https://api.brightsky.dev/current_weather?lat=48.67&lon=10.1516
Weather__NominatimApiUrl=https://nominatim.openstreetmap.org/search?format=jsonv2&postalcode=
Weather__Location=Heidenheim

# ===== Grafana Configuration =====
# Grafana admin password for production access
GF_SECURITY_ADMIN_PASSWORD=
```

---

## Configuration Variables Reference

### Database Configuration

| Variable | Description | Dev Value | Live Value | Required |
|----------|-------------|-----------|------------|----------|
| `POSTGRES_HOST` | PostgreSQL hostname | `postgres` | `postgres` | ✅ |
| `POSTGRES_PORT` | PostgreSQL port | `5432` | `5432` | ✅ |
| `POSTGRES_DATABASE`/`POSTGRES_DB` | Database name | `Isopruefi` | `Isopruefi` | ✅ |
| `POSTGRES_USERNAME`/`POSTGRES_USER` | Database username | `Isopruefi` | `Isopruefi` | ✅ |
| `POSTGRES_PASSWORD` | Database password | `secret` | `secret` | ✅ |

### Authentication & Security

| Variable | Description | Example Value | Required |
|----------|-------------|---------------|----------|
| `Admin__UserName` | Initial admin username | `admin` | ✅ |
| `Admin__Email` | Initial admin email | `admin@localhost.dev` | ✅ |
| `Admin__Password` | Initial admin password | `DevAdmin123!` | ✅ |
| `Jwt__ValidIssuer` | JWT token issuer | `localhost` | ✅ |
| `Jwt__ValidAudience` | JWT token audience | `localhost` | ✅ |
| `Jwt__Secret` | JWT signing secret (256-bit) | `your-secure-256-bit-secret-key...` | ✅ |

### InfluxDB Configuration

| Variable | Description | Example Value | Required |
|----------|-------------|---------------|----------|
| `Influx__InfluxDBHost` | InfluxDB connection URL | `http://influxdb3:8181` | ✅ |
| `Influx__InfluxDBToken` | InfluxDB authentication token | `apiv3_...` | ❌ (Dev), ✅ (Live) |
| `Influx__InfluxDBDatabase` | InfluxDB database name | `IsoPruefi` | ✅ |

### MQTT Configuration

| Variable | Description | Example Value | Required |
|----------|-------------|---------------|----------|
| `Mqtt__BrokerHost` | MQTT broker hostname | `aicon.dhbw-heidenheim.de` | ✅ |
| `Mqtt__BrokerPort` | MQTT broker port | `1883` | ✅ |

### Weather API Configuration

| Variable | Description | Purpose | Required |
|----------|-------------|---------|----------|
| `Weather__OpenMeteoApiUrl` | Open-Meteo API endpoint with coordinates | Primary weather data source | ✅ |
| `Weather__BrightSkyApiUrl` | BrightSky API endpoint with coordinates | Secondary weather data source | ✅ |
| `Weather__NominatimApiUrl` | OpenStreetMap geocoding service | Location lookup (Live only) | ❌ |
| `Weather__Location` | Human-readable location name | Display purposes | ✅ |

### Frontend Configuration

| Variable | Description | Example Value | Required |
|----------|-------------|---------------|----------|
| `VITE_API_BASE_URL` | Backend API base URL for frontend | `https://backend.localhost` | ✅ |

### Grafana Configuration (Live Only)

| Variable | Description | Example Value | Required |
|----------|-------------|---------------|----------|
| `GF_SECURITY_ADMIN_PASSWORD` | Grafana admin user password | `1234` | ✅ (Live) |