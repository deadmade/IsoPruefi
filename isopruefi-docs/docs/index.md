# Welcome to IsoPrüfi
We are happy that you are here 🥳🎉

![IsoPrüfi Logo](images/isopruefi.png)

IsoPrüfi is an IoT-based system for testing building insulation effectiveness by monitoring temperature differences between indoor and outdoor environments.

## Quick Overview

The system uses Arduino-based sensors to collect temperature data from multiple locations, processes it through a containerized backend, and presents insights via a web interface.

**Key Features:**
- Real-time temperature monitoring with Arduino sensors
- Automated data collection and analysis
- Web-based dashboard for visualization
- Containerized architecture for easy deployment
- Offline data buffering with SD card storage

## Getting Started

- **[Setup & Build](build.md)** - Initial project setup and development environment
- **[Docker Environment](docker-dev.md)** - Container overview and local development
- **[API Reference](api-reference.md)** - REST API documentation
- **[Guidelines](guidelines.md)** - Development conventions and best practices

## Architecture

Built with modern technologies:
- **Backend**: .NET REST API, MQTT message broker
- **Frontend**: React with TypeScript
- **Database**: PostgreSQL + InfluxDB for time-series data
- **Hardware**: Arduino MKR WiFi 1010 with temperature sensors
- **Infrastructure**: Docker containers with Traefik load balancer

## Documentation Structure

- **Getting Started** - Setup guides and quick start
- **Development** - Guidelines, API docs, and troubleshooting
- **Architecture Documentation** - Technical design using arc42 template

---

Ready to contribute? Start with our [contributing guide](contributing.md) or jump into the [build setup](build.md).