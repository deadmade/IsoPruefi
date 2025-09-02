# Docker Development Environment

This documentation provides an overview of the Docker containers used in both development and production environments, their functions, and access addresses.

---

## Development Environment

### Container Overview (Development)

| Container                 | Image                           | Description                                                                                | Access Address                                                                              |
|---------------------------|---------------------------------|--------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------|
| **traefik**               | `traefik:3.4.4`                 | Reverse proxy and load balancer for external access to containers (HTTPS certificates)    | [traefik.localhost](https://traefik.localhost), Ports: `80`, `443`, Dashboard: `8432`     |
| **influxdb3**             | `influxdb:3.2.1-core`           | Time series database (InfluxDB 3.x) for sensor data storage                               | Port: `8181`                                                                               |
| **influxdb-explorer**     | `influxdata/influxdb3-ui:1.0.3` | Web interface for managing and querying InfluxDB data                                      | [explorer.localhost](https://explorer.localhost), Port: `8888`                             |
| **postgres**              | `postgres:alpine3.21`           | PostgreSQL database for relational application data                                        | Port: `5432`                                                                               |
| **loki**                  | `grafana/loki:3.5.2`            | Log aggregation and management system                                                      | Port: `3100`                                                                               |
| **prometheus**            | `prom/prometheus:v3.4.2`        | Monitoring tool for collecting and evaluating metrics                                      | Port: `9090`                                                                               |
| **alloy**                 | `grafana/alloy:v1.9.2`          | Observability platform for integrating Loki and Prometheus                                | Ports: `12345`, `4317`, `4318`                                                             |
| **grafana**               | `grafana/grafana:12.0.2`        | Web-based visualization and dashboard for metrics and logs                                 | [grafana.localhost](https://grafana.localhost), Port: `3000`                               |
| **isopruefi-frontend-1**  | Custom build (React+TypeScript) | Frontend application (development build with hot reload)                                   | [frontend.localhost](https://frontend.localhost), Port: `5173`                             |
| **isopruefi-backend-api-1** | Custom build (.NET REST API)  | Backend REST API for application logic (development build)                                 | [backend.localhost](https://backend.localhost), Port: `8080`                               |
| **isopruefi-mqtt-receiver-1** | Custom build (.NET Worker)   | MQTT message receiver worker service                                                        | No external port (internal service)                                                        |
| **isopruefi-get-weather-worker-1** | Custom build (.NET Worker) | Weather data fetching worker service                                                        | No external port (internal service)                                                        |

### Development Setup Commands

```bash
# Start development environment
docker compose up -d

# View logs
docker compose logs -f

# Stop environment
docker compose down

# Rebuild services after code changes
docker compose up --build
```

---

## Production Environment

### Container Overview (Production)

| Container                     | Image                                                    | Description                                                | Access Address                                 |
|-------------------------------|----------------------------------------------------------|------------------------------------------------------------|-----------------------------------------------|
| **traefik**                   | `traefik:3.4.4`                                         | Production reverse proxy with SSL termination             | Ports: `5000` (HTTP), `5001` (HTTPS), `5002` (Dashboard) |
| **influxdb3**                 | `influxdb:3.2.1-core`                                   | Production time-series database                            | Port: `5006`                                 |
| **postgres**                  | `postgres:17.5`                                         | Production PostgreSQL database                             | Port: `5003`                                 |
| **loki**                      | `grafana/loki:3.5.2`                                    | Production log aggregation                                 | Internal only                                 |
| **prometheus**                | `prom/prometheus:v3.4.2`                                | Production metrics collection                              | Port: `5004`                                 |
| **alloy**                     | `grafana/alloy:v1.9.2`                                  | Production observability agent                             | Internal only                                 |
| **grafana**                   | `grafana/grafana:12.0.2`                                | Production dashboards and visualization                    | Port: `5005`                                 |
| **isopruefi-frontend-1/2**    | `ghcr.io/deadmade/isopruefi-frontend:latest`            | Production frontend (2 instances for high availability)   | Port: `5007` (instance 1), Load balanced via Traefik |
| **isopruefi-backend-api-1/2** | `ghcr.io/deadmade/isopruefi-rest-api:latest`            | Production REST API (2 instances for high availability)   | Load balanced via Traefik                     |
| **isopruefi-mqtt-receiver-1/2** | `ghcr.io/deadmade/isopruefi-mqtt-worker:latest`      | Production MQTT workers (2 instances for redundancy)      | Internal services                             |
| **isopruefi-get-weather-worker-1/2** | `ghcr.io/deadmade/isopruefi-weather-worker:latest` | Production weather workers (2 instances for redundancy)   | Internal services                             |

### Production Features

**High Availability:**
- Multiple instances of critical services (frontend, backend, workers)
- Health checks for all services
- Automatic service dependency management
- Load balancing via Traefik

**Monitoring & Observability:**
- Comprehensive logging with Loki
- Metrics collection with Prometheus
- Centralized dashboards with Grafana
- Health check endpoints for all services

**Security & Reliability:**
- Production-optimized container images
- Proper volume management for data persistence
- Environment-based configuration
- Backup capabilities (commented configuration available)

### Production Setup Commands

```bash
# Navigate to production directory
cd isopruefi-docker-live

# Start production environment
docker compose up -d

# Check service health
docker compose ps

# View service logs
docker compose logs [service-name]

# Scale services if needed
docker compose up -d --scale isopruefi-backend-api=3

# Stop production environment
docker compose down
```

---

## Network Architecture

### Docker Networks

Both environments use similar network topology:

- **isopruefi-network**: Main application network for frontend, backend, and databases
- **isopruefi-monitoring**: Dedicated network for observability stack (Loki, Prometheus, Grafana, Alloy)

### Service Communication

| Source Service | Target Service | Protocol | Purpose |
|----------------|----------------|----------|---------|
| Frontend | Backend API | HTTPS/REST | Data retrieval and user actions |
| MQTT Worker | InfluxDB | TCP/SQL | Store sensor data |
| Weather Worker | InfluxDB | TCP/SQL | Store weather data |
| Backend API | PostgreSQL | TCP/SQL | Application data operations |
| Backend API | InfluxDB | TCP/SQL | Time-series data queries |
| Alloy | Loki/Prometheus | HTTP/gRPC | Log and metrics collection |
| Grafana | Loki/Prometheus | HTTP | Dashboard data retrieval |

---

## Environment Differences

### Development Environment
- **Purpose**: Local development and testing
- **Build**: Services built from source code with hot reload
- **Domains**: `*.localhost` for easy local access
- **Volumes**: Source code mounted for live editing
- **Logging**: Console output for debugging
- **Single instances**: One container per service

### Production Environment  
- **Purpose**: Live deployment on DHBW server
- **Build**: Pre-built container images from GitHub Container Registry
- **Domains**: `aicon.dhbw-heidenheim.de` subpaths
- **Volumes**: Named volumes for data persistence
- **Logging**: Structured logging to Loki
- **Redundancy**: Multiple instances for high availability

---

## Configuration Management

### Environment Variables

Both environments use `.env` and `secrets.env` files:

**Development (`secrets.env`):**
```env
# Database connections
POSTGRES_PASSWORD=secret
POSTGRES_USER=Isopruefi
POSTGRES_DB=Isopruefi

# InfluxDB configuration
INFLUX_TOKEN=your-development-token
INFLUX_ORG=isopruefi
INFLUX_BUCKET=sensor-data

# Weather API
WEATHER_API_KEY=your-api-key
```

**Production (`secrets.env`):**
```env
# Production database passwords (stronger)
POSTGRES_PASSWORD=strong-production-password
POSTGRES_USER=isopruefi_prod

# Production InfluxDB token
INFLUX_TOKEN=production-token-with-limited-permissions

# Production API keys
WEATHER_API_KEY=production-weather-api-key
```

### Volume Management

**Development:**
- Uses bind mounts for source code
- Local directories for data persistence

**Production:**  
- Named Docker volumes for reliable data persistence
- Automated backup capabilities (configurable)

---

## Health Checks & Monitoring

### Health Check Endpoints

All production services include health checks:

```yaml
healthcheck:
  test: ["CMD", "curl", "http://localhost:8080/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

### Monitoring Stack Access

**Development:**
- Grafana: [grafana.localhost](https://grafana.localhost)
- Prometheus: `localhost:9090`
- Loki: `localhost:3100`

**Production:**
- Grafana: `aicon.dhbw-heidenheim.de:5005`
- Prometheus: `aicon.dhbw-heidenheim.de:5004`
- Loki: Internal only (accessed via Grafana)

---

## Troubleshooting

### Common Issues

**Development:**
- Port conflicts: Check if ports 80, 443, 5173, 8080 are available
- SSL certificate issues: Clear browser cache for `*.localhost` domains
- Hot reload not working: Ensure source code volumes are properly mounted

**Production:**
- Health check failures: Check service logs and dependencies
- Load balancer issues: Verify Traefik configuration and service labels
- Database connections: Ensure proper network connectivity and credentials

### Useful Commands

```bash
# Check container status and health
docker compose ps

# View resource usage
docker stats

# Restart unhealthy services
docker compose restart [service-name]

# Update production images
docker compose pull
docker compose up -d

# Backup volumes (production)
docker run --rm -v isopruefi-docker-live_postgres:/data -v $(pwd):/backup ubuntu tar czf /backup/postgres-backup.tar.gz -C /data .
```

For detailed troubleshooting, see our [troubleshooting guide](troubleshooting.md).

