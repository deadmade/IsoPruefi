# MQTT Protocol Documentation

## Overview

IsoPrüfi uses MQTT for sensor data communication between Arduino devices and the backend system. The protocol handles real-time data transmission with recovery capabilities for offline scenarios.

## Topic Structure

### Standard Topic Format
```
{topicPrefix}/{sensorType}/{sensorId}
```

**Example**: `dhbw/ai/si2023/temp/Sensor_One`

### Topic Components
- **topicPrefix**: Configurable prefix (default: `dhbw/ai/si2023/`)
- **sensorType**: Type of sensor (`temp`, `hum`, `co2`, `spl`, `mic`, `ikea`)
- **sensorId**: Unique sensor identifier

## Message Formats

### Standard Sensor Reading
```json
{
  "timestamp": 1672531200,
  "value": [23.5],
  "sequence": 1234
}
```

### Recovery Data (Bulk Upload)
```json
{
  "timestamp": 1672531200,
  "value": [23.5],
  "sequence": 1234,
  "meta": {
    "t": [1672531100, 1672531160, 1672531220],
    "v": [23.1, 23.3, 23.4],
    "s": [1231, 1232, 1233]
  }
}
```

## Data Fields

| Field | Type | Description |
|-------|------|-------------|
| timestamp | long | Unix timestamp (seconds since epoch) |
| value | float[] | Sensor readings (array for multi-point sensors) |
| sequence | int | Sequential counter for message ordering |
| meta.t | long[] | Recovery timestamps |
| meta.v | float[] | Recovery values |
| meta.s | int[] | Recovery sequence numbers |


## Error Handling

- Failed publishes trigger local storage
- Recovery data sent in batches (max 3 files per loop)
- 60-second timeout for recovery operations
- Automatic reconnection on connection loss