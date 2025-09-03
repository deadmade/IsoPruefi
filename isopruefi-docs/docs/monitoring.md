# Monitoring & Observability

## Overview

IsoPrüfi uses Grafana, Prometheus, and Loki for comprehensive monitoring and observability.

## Components

### Grafana Dashboards
- **Location**: `/grafana/`
- **Features**:
  - System metrics visualization
  - Application performance monitoring
  - Custom uptime dashboard
  - Alerting

### Prometheus Metrics
- **Config**: `/loki/prometheus.yml`
- **Scrapes**: Application health endpoints
- **Alerts**: Defined in `alerts.yml`

### Loki Logging
- **Config**: `/loki/loki-config.yaml`
- **Agent**: Grafana Alloy for log collection (`config.alloy`)
- **Centralized**: All application and system logs

## Health Checks

### Monitoring Targets
- Application uptime
- Database connectivity
- MQTT broker status
- Worker process health

## Alerting

Configure alerts in `/loki/alerts.yml`:
- Service down alerts
- High error rate notifications
- Resource utilization warnings

## Log Analysis

Access logs through Grafana's Explore feature:
- Filter by service/component
- Search for error patterns
- Trace request flows