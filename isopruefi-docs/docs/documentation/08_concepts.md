# Cross-cutting Concepts {#section-concepts}

> Dummy Text for section 8:

This section describes overarching concepts and solution patterns that are used throughout the IsoPrüfi system to ensure consistency, reusability, and architectural integrity.

---

## Domain Concepts

- **Insulation Performance (ΔT)**: The key metric is the temperature difference between indoor and outdoor sensors. This value is computed in real time and used for analysis and alerts.
- **Sensor Units**: Each Arduino is considered a logical sensor unit, tagged with a physical location and ID.
- **Data Sources**: Each data record is enriched with a source type (`sensor`, `weatherAPI`), a timestamp, and a confidence level (based on fallback or delay).

---

## Safety and Fault Tolerance Concepts

- **Redundant Logging**: Each Arduino writes to local SD cards to preserve data if the network fails. Data can later be synchronized.
- **Retained MQTT Messages**: MQTT topics are configured with `retain=true` so the latest sensor values are cached on the broker.
- **Stateless Services**: Backend services are designed to be stateless and independently deployable, which simplifies recovery after failure.

---

## Architecture and Design Patterns

- **Message-Driven Architecture**: Core communication is asynchronous via MQTT. Services subscribe to well-defined topics using a publish/subscribe model.
- **Microservice Pattern**: Backend services (WeatherWorker, MQTTReceiver, API) are independently deployable and loosely coupled.
- **API Gateway Pattern**: The Website Cluster communicates with backend services through the REST API cluster, shielding internal complexity.

---

## Development Concepts

- **Containerization**: All services run in Docker containers and are grouped logically (e.g., "MQTT Cluster", "Website Cluster").
- **Continuous Integration**: GitHub workflows enforce formatting, build checks and tests before merges.
- **Infrastructure as Code**: Deployment manifests are version-controlled (e.g. `docker-compose.yml`, Kubernetes manifests).

---

## Operational Concepts

- **Observability via Loki**: Logs from all services are collected via the Loki log system and visualized in Grafana.
- **Health Monitoring**: Each service exposes a `/health` endpoint and reports metrics via Prometheus-compatible format (optional).
- **Scaling**: Stateless services behind a load balancer can be scaled horizontally (e.g., Website Cluster, MQTTReceiver Cluster).

---

These cross-cutting concepts ensure consistency across all layers of the system and support the project’s core quality goals: persistence, availability, data integrity, and maintainability.