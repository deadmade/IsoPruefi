# Docker Environment Documentation

This documentation provides a comprehensive overview of the Docker environments used in the IsoPruefi project, including development and live deployments, container configurations, and environment file management.

---

## Development Environment

The development environment is configured via `docker-compose.yml` in the project root and provides local development with automatic certificate generation for localhost domains.

### Container Overview (Development)

| Container                          | Image                           | Description                                                                                | Access                                                                                     |
|------------------------------------|---------------------------------|--------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------|
| **traefik**                        | `traefik:3.4.4`                 | Reverse proxy and load balancer with automatic HTTPS certificates                         | [traefik.localhost](https://traefik.localhost), Ports: `80`, `443`, Dashboard: `8432`    |
| **influxdb3**                      | `influxdb:3.2.1-core`           | Time series database (InfluxDB 3.x) for sensor and telemetry data                        | Port: `8181`                                                                              |
| **influxdb-explorer**              | `influxdata/influxdb3-ui:1.0.3` | Web interface for managing and querying InfluxDB data                                     | [explorer.localhost](https://explorer.localhost), Port: `8888`                            |
| **postgres**                       | `postgres:alpine3.21`           | PostgreSQL database for relational data storage                                           | Port: `5432`                                                                              |
| **loki**                           | `grafana/loki:3.5.2`            | Log aggregation and storage system                                                        | Port: `3100`                                                                              |
| **prometheus**                     | `prom/prometheus:v3.4.2`        | Monitoring system for metrics collection and alerting                                     | Port: `9090`                                                                              |
| **alloy**                          | `grafana/alloy:v1.9.2`          | Observability data collector for Loki and Prometheus integration                          | Ports: `12345`, `4317`, `4318`                                                            |
| **grafana**                        | `grafana/grafana:12.0.2`        | Visualization and dashboarding for metrics and logs                                       | [grafana.localhost](https://grafana.localhost)                                            |
| **isopruefi-frontend-1**           | Built from `./isopruefi-frontend` | React frontend application (development build)                                           | [frontend.localhost](https://frontend.localhost)                                          |
| **isopruefi-backend-api-1**        | Built from `./isopruefi-backend`  | .NET REST API for application logic                                                      | [backend.localhost](https://backend.localhost)                                            |
| **isopruefi-mqtt-receiver-1**      | Built from `./isopruefi-backend`  | MQTT message receiver and processor                                                       | Internal service (no external access)                                                     |
| **isopruefi-get-weather-worker-1** | Built from `./isopruefi-backend`  | Weather data collection worker service                                                    | Internal service (no external access)                                                     |

---

## Live/Production Environment

The live environment is configured via `isopruefi-docker-live/docker-compose.yml` and uses pre-built Docker images from GitHub Container Registry with load balancing and health checks.

### Container Overview (Live)

| Container                          | Image                                                | Description                                                                                | Access                                        |
|------------------------------------|------------------------------------------------------|--------------------------------------------------------------------------------------------|-----------------------------------------------|
| **traefik**                        | `traefik:3.4.4`                                     | Production reverse proxy with custom domain routing                                       | Ports: `5000` (HTTP), `5001` (HTTPS), `5002` (Dashboard) |
| **influxdb3**                      | `influxdb:3.2.1-core`                               | Production time series database with persistent volumes                                    | Port: `5006`                                 |
| **postgres**                       | `postgres:17.5`                                     | Production PostgreSQL with persistent volumes                                             | Port: `5003`                                 |
| **loki**                           | `grafana/loki:3.5.2`                                | Production log aggregation with persistent storage                                        | Internal service                              |
| **prometheus**                     | `prom/prometheus:v3.4.2`                            | Production metrics collection with persistent storage                                     | Port: `5004`                                 |
| **alloy**                          | `grafana/alloy:v1.9.2`                              | Production observability data collector                                                   | Internal service                              |
| **grafana**                        | `grafana/grafana:12.0.2`                            | Production dashboards with persistent configuration                                       | Port: `5005`                                 |
| **isopruefi-frontend-1/2**         | `ghcr.io/deadmade/isopruefi-frontend:latest`        | Load-balanced React frontend instances                                                    | Port: `5007` (frontend-1 only), Path: `/frontend` |
| **isopruefi-backend-api-1/2**      | `ghcr.io/deadmade/isopruefi-rest-api:latest`        | Load-balanced .NET API instances                                                          | Path: `/backend` (via Traefik)               |
| **isopruefi-mqtt-receiver-1/2**    | `ghcr.io/deadmade/isopruefi-mqtt-worker:latest`     | Redundant MQTT receiver instances                                                         | Internal services                             |
| **isopruefi-get-weather-worker-1/2** | `ghcr.io/deadmade/isopruefi-weather-worker:latest` | Redundant weather data collection instances                                               | Internal services                             |

### Live Environment URLs

When deployed on the production server (`aicon.dhbw-heidenheim.de`), the services are accessible via:

- **Traefik Dashboard**: `https://traefik.aicon.dhbw-heidenheim.de:5002`
- **Frontend Application**: `https://aicon.dhbw-heidenheim.de:5001/frontend`
- **Backend API**: `https://aicon.dhbw-heidenheim.de:5001/backend`  
- **Grafana Dashboards**: `http://aicon.dhbw-heidenheim.de:5005`
- **Prometheus Metrics**: `http://aicon.dhbw-heidenheim.de:5004`
- **PostgreSQL**: `aicon.dhbw-heidenheim.de:5003`
- **InfluxDB**: `http://aicon.dhbw-heidenheim.de:5006`

> **Note**: HTTPS is available on port `5001`, HTTP on port `5000`. The frontend and backend use path-based routing with Traefik handling SSL termination.

---

## Networks

### Development Networks
- `isopruefi-network`: Main network for application services and Traefik
- `isopruefi-monitoring`: Dedicated network for observability tools (Loki, Prometheus, Grafana, Alloy)

### Live Networks  
- `isopruefi-network`: Main production network for all services
- `isopruefi-monitoring`: Production observability network

---

## Key Differences: Development vs Live

### Development Environment
- **Build Strategy**: Local builds from source code with hot-reload capabilities
- **Domains**: Uses `*.localhost` domains with automatic certificate generation
- **Scaling**: Single instance of each service
- **Data**: Bind mounts for development data persistence
- **Environment**: `ASPNETCORE_ENVIRONMENT=Development`

### Live Environment
- **Build Strategy**: Pre-built images from GitHub Container Registry
- **Domains**: Custom domain routing with production certificates
- **Scaling**: Multiple instances with load balancing for high availability
- **Data**: Named Docker volumes for production data persistence  
- **Health Checks**: Comprehensive health monitoring for all services
- **Environment**: `ASPNETCORE_ENVIRONMENT=Docker`
- **Backup**: Automated backup system (currently commented out)

---

## Service Details

### Traefik Configuration
- **Development**: Automatic HTTPS with local certificates for `*.localhost` domains
- **Live**: Production routing with custom domain support and SSL termination

### Database Services
- **InfluxDB**: Time-series data storage for sensor readings and telemetry
- **PostgreSQL**: Relational data for user management, configuration, and application state

### Observability Stack
- **Loki**: Centralized logging with automatic log collection via Docker labels
- **Prometheus**: Metrics collection and alerting
- **Grafana**: Unified dashboards for logs and metrics visualization
- **Alloy**: Data collection and forwarding to Loki/Prometheus

### Application Services
- **Frontend**: React-based web application for data visualization and control
- **Backend API**: .NET REST API providing core application logic
- **MQTT Receiver**: Handles incoming sensor data via MQTT protocol
- **Weather Worker**: Collects external weather data from multiple APIs

