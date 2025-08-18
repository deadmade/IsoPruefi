# Documentation of the Docker development environment

This documentation provides an overview of the Docker containers used, as well as their function and their addresses.

---

## Overview of the containers

| Container                 | Image                           | Description                                                                                        | Adress                                                                                    |
| ------------------------- | ------------------------------- | --------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| **traefik**               | `traefik:3.4.4`                 | Reverse proxy and load balancer for external access to our containers (HTTPS certificates) | [traefik.localhost](https://traefik.localhost), Ports: `80`, `443`, Dashboard-Port: `8432` |
| **influxdb**              | `influxdb:3.2.1-core`           | Time series database (InfluxDB 3.x) for data storage                                             | Port: `8181`                                                                               |
| **influxdb-explorer**     | `influxdata/influxdb3-ui:1.0.3` | Web interface for managing and querying InfluxDB data                                         | [explorer.localhost](https://explorer.localhost), Port: `8888`                             |
| **postgres**              | `postgres:alpine3.21`           | PostgreSQL database for relational data storage                                              | Port: `5432`                                                                               |
| **loki**                  | `grafana/loki:3.5.2`            | Log aggregation and management                                                                      | Port: `3100`                                                                               |
| **prometheus**            | `prom/prometheus:v3.4.2`        | Monitoring tool for collecting and evaluating metrics                                            | Port: `9090`                                                                               |
| **alloy**                 | `grafana/alloy:v1.9.2`          | Observability platform for the integration of Loki and Prometheus                                     | Ports: `12345`, `4317`, `4318`                                                             |
| **grafana**               | `grafana/grafana:12.0.2`        | Web-based visualization and dashboard for metrics and logs                                      | [grafana.localhost](https://grafana.localhost)                                             |
| **isopruefi-frontend**    | own Build (React)           | Frontend Application                                                                                  | [frontend.localhost](https://frontend.localhost)                                           |
| **isopruefi-backend-api** | own Build (.NET REST-API)   | Backend REST API for application logic                                                    | [backend.localhost](https://backend.localhost)                                             |

---

## Networks

The following Docker networks are used to logically separate the containers from each other:

- `isopruefi-network`: General network, used by Traefik
- `database-network`: Network for databases (InfluxDB, PostgreSQL)
- `isopruefi-custom`: Network for user-defined services (frontend, backend)
- `loki`: Network for observability tools (Loki, Grafana, Alloy)

---

## Details of important containers

### Traefik

Traefik serves as a reverse proxy that receives all HTTP(S) requests and forwards them to the appropriate Docker containers. It automatically manages the TLS certificates and provides a dashboard for administration.

### Grafana

Grafana is used to visualize and analyze logs and metrics. It is connected to Loki (logs) and Prometheus (metrics).

### InfluxDB und InfluxDB-Explorer

InfluxDB stores time series data, while InfluxDB Explorer provides a convenient web interface to access this data.

### PostgreSQL

PostgreSQL stores relational data used by the backend API.

