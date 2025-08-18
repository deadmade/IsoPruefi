# Solution Strategy {#section-solution-strategy}

> Dummy Text for Section 4:
> Focus section 9 instead of 4
> this text as a fundation for section 9

This section summarizes the key architectural decisions and strategies that shape the IsoPrüfi system design. The aim is to create a robust, distributed and modular monitoring system for building insulation performance.

---

## Technology Decisions

- **Sensor Layer**: Temperature sensors connected to Arduino boards form the hardware data acquisition layer.
- **Communication Protocol**: MQTT is used for lightweight, publish/subscribe communication between Arduinos and the backend.
- **Backend Language**: All backend services are implemented in **C# (.NET 9)** for strong typing, maintainability, and modern async support.
- **Data Storage**: A **central SQL-based database cluster** is used for structured persistence. Sensor data is also logged locally on SD cards as fallback.
- **External Data**: A **WeatherDataWorker** fetches temperature and humidity data from a third-party weather API to provide reference data.
- **Visualization**: Frontend users access data through a **Website Cluster**, consisting of an overview page and admin panel. Optionally, **Grafana** dashboards visualize time series.
- **Observability**: Logs from all services are collected using **Loki**, accessible via Grafana.

---

## System Decomposition Strategy

- The system is containerized and modular. It is structured into clusters with clear responsibilities: 
  - Data ingestion (MQTT Receiver)
  - External data enrichment (WeatherWorker)
  - API and UI interface (REST + Website Cluster)
  - Data storage (DB Cluster)
- A **load balancer** distributes traffic between frontend components and ensures horizontal scalability.

---

## Achieving Key Quality Goals

| Quality Goal   | Strategy                                                                                   |
|----------------|---------------------------------------------------------------------------------------------|
| **Persistence**     | Dual logging (local SD + central DB), MQTT retain flags, retries on failure            |
| **Availability**    | Decoupled services, stateless components, load balancing, fallback to local storage    |
| **Data Integrity**  | Timestamped payloads, standardized MQTT topic schema, input validation and filtering   |
| **Testability**     | Modular services with unit and integration test coverage, CI-ready design              |

---

## Organizational / Development Process Decisions

- **Source Control**: GitHub with structured branches and CI pipelines
- **Documentation**: Based on **arc42 template**, managed in MkDocs
- **Container Management**: Container groups follow naming conventions; teams deploy only within their cluster scope
- **Architecture Decisions**: Documented using ADRs (Architecture Decision Records)

---

This solution strategy ensures flexibility, scalability, and fault tolerance while keeping the system observable and maintainable. It supports the key requirement of uninterrupted, trustworthy insulation monitoring—even under partial failures.
