# API Reference

## Interactive API Documentation

The IsoPrüfi REST API provides endpoints for managing temperature data, sensors, and system health monitoring. Explore the interactive API documentation below to test endpoints directly.

## Overview

**Base URL:** 
- Development: `https://backend.localhost`  
- Production: `https://aicon.dhbw-heidenheim.de:5001/backend`

**API Version:** v1

## Authentication

The API uses JWT Bearer token authentication for protected endpoints.

### Getting Started

1. **Login** using `/v1/Authentication/Login` to get your access token
2. **Add token** to requests using the `Authorization: Bearer <token>` header
3. **Refresh tokens** using `/v1/Authentication/Refresh` when they expire

## Key Features

### Temperature Data Management
- **Real-time data retrieval** from multiple sensor sources
- **Time-range filtering** with ISO 8601 timestamps
- **Unit conversion** between Celsius and Fahrenheit
- **Data quality validation** with plausibility checks

### Sensor Configuration  
- **MQTT topic management** for sensor integration
- **Location-based sensor mapping** (North, South, East, West)
- **Dynamic sensor registration** and configuration

### User Management
- **Role-based access control** (Admin/User roles)
- **Password management** and user registration
- **JWT token refresh** for seamless authentication

### Location Services
- **Postal code management** for weather data integration
- **Geographic coordinate mapping**
- **External weather service integration**

## Error Handling

The API uses standard HTTP status codes and returns detailed error information:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request", 
  "status": 400,
  "detail": "Start time must be before end time",
  "instance": "/api/v1/TemperatureData/GetTemperature"
}
```