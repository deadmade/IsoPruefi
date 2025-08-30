# Architecture Decisions {#section-design-decisions}

## ADR 1: Backend Technology Stack

### Status:

Accepted (July 2025)

### Context:

System needs robust backend technology for REST API and worker services. Team has existing familiarity with C#
development.

### Decision:

Use .NET 9 with C# for all backend services (REST API, MQTT Receiver, Weather Worker).

### Alternatives Considered:

| Option  | Pros                                  | Cons                                                                   |
|---------|---------------------------------------|------------------------------------------------------------------------|
| Node.js | Rapid iteration, huge ecosystem       | Different stack; weaker static typing by default; less team experience |
| Go      | High perf/concurrency; small binaries | Less team experience; different tooling                                |
| Python  | Rich libs; fast prototyping           | Lower throughput; weaker typing by default                             |

### Consequences:

Positive:

- Team familiarity reduces development time
- Strong typing prevents runtime errors
- Excellent tooling and debugging support
- Modern async/await support for I/O operations

Negative:

- Platform dependency (though mitigated by containers)
- Larger memory footprint than some alternatives

Neutral:

- Containerization standardizes runtime

---

## ADR 2: Microservices Architecture

### Status:

Accepted (July 2025)

### Context:

System has distinct responsibilities: API serving, MQTT message processing, and weather data fetching. Need modularity
and independent scaling.

### Decision:

Split backend into separate services: REST API, MQTT Receiver Worker, Weather Data Worker.

### Alternatives Considered:

| Option           | Pros                             | Cons                                                    |
|------------------|----------------------------------|---------------------------------------------------------|
| Monolith         | Simple deploy; easy local dev    | No independent scaling; fault blast radius              |
| Modular monolith | Clear boundaries in one process  | Still one deploy unit; limited isolation                |
| Serverless       | No servers to manage; auto-scale | Cold starts; platform coupling; ops visibility variance |

### Consequences:

Positive:

- Clear separation of concerns
- Independent scaling and deployment
- Fault isolation between services

Negative:

- Increased deployment complexity
- Network communication overhead between services

Neutral:

- Requires basic observability to manage complexity

---

## ADR 3: Dual Database Strategy

### Status:

Accepted (July 2025)

### Context:

System needs both structured application data (users, authentication) and time-series sensor data with different access
patterns.

### Decision:

Use PostgreSQL for application data and InfluxDB for time-series sensor data.

### Alternatives Considered:

| Option                   | Pros                      | Cons                                               |
|--------------------------|---------------------------|----------------------------------------------------|
| PostgreSQL + TimescaleDB | One stack; SQL everywhere | Ops complexity; perf tuning for time series needed |
| InfluxDB only            | Optimized for time series | Awkward relational modeling; joins missing         |
| SQLite + InfluxDB Lite   | Simple, lightweight       | Limited concurrency; feature gaps                  |

### Consequences:

Positive:

- PostgreSQL optimized for relational data and transactions
- InfluxDB optimized for time-series queries and compression
- Each database serves its specific use case efficiently

Negative:

- Two databases to maintain and backup

Neutral:

- Extract, Transform, Load (ETL) between stores is minimal

---

## ADR 4: Observability Stack

### Status:

Accepted (July 2025)

### Context:

Distributed microservices architecture requires comprehensive monitoring, logging, and alerting capabilities.

### Decision:

Loki for logs, Prometheus for metrics, Grafana for dashboards, Alloy as agent.

### Alternatives Considered:

| Option                      | Pros                                | Cons                                 |
|-----------------------------|-------------------------------------|--------------------------------------|
| ELK (Elasticsearch, Kibana) | Powerful search/analytics           | Heavier footprint; more ops effort   |
| OTel collector + vendor     | Standards-based; flexible pipelines | Vendor lock-in and/or cost           |
| Managed cloud observability | Minimal ops                         | Ongoing costs; data residency limits |

### Consequences:

Positive:

- Complete observability into system health and performance
- Industry-standard tools with good integration
- Unified dashboard for all monitoring data

Negative:

- Additional infrastructure to maintain

Neutral:

- Can swap components later

---

## ADR 5: Traefik as Reverse Proxy

### Status:

Accepted (July 2025)

### Context:

Multiple services need unified entry point, SSL termination, and service discovery in containerized environment.

### Decision:

Use Traefik as reverse proxy with automatic service discovery and HTTPS termination.

### Alternatives Considered:

| Option  | Pros                          | Cons                                     |
|---------|-------------------------------|------------------------------------------|
| Nginx   | Mature; high performance      | Manual routing/config; no auto-discovery |
| Caddy   | Simple TLS; easy config       | Fewer discovery features                 |
| HAProxy | Very fast; robust LB features | More manual config; fewer HTTP niceties  |

### Consequences:

Positive:

- Automatic service discovery via Docker labels
- Built-in SSL certificate management
- Load balancing capabilities

Negative:

- Single point of failure if not properly configured
- Additional configuration complexity

Neutral:

- Replaceable by Nginx if needed

---

## ADR 6: JWT Authentication Strategy

### Status:

Accepted (July 2025)

### Context:

REST API requires secure authentication mechanism. Need stateless authentication for microservices architecture.

### Decision:

Implement JWT token-based authentication with Entity Framework for user management.

### Alternatives Considered:

| Option               | Pros                          | Cons                                    |
|----------------------|-------------------------------|-----------------------------------------|
| Server-side sessions | Simple; revocation is trivial | Stateful; sticky sessions; scale limits |
| OAuth2/OIDC proxy    | Standards-based; SSO ready    | More moving parts; infra complexity     |
| API keys             | Simple; easy for machines     | Poor granularity; rotation burdens      |

### Consequences:

Positive:

- Stateless authentication scales well
- Standard approach with good library support
- Tokens can carry user claims

Negative:

- Token revocation complexity
- Requires secure token storage on client side

Neutral:

- Token TTL balances risk and UX

---

## ADR 7: Docker Compose for Development Environment

### Status:

Accepted (July 2025)

### Context:

Complex multi-service architecture needs consistent development environment setup across team members.

### Decision:

Use Docker Compose to orchestrate all services for local development.

### Alternatives Considered:

| Option            | Pros                   | Cons                             |
|-------------------|------------------------|----------------------------------|
| Dev Containers    | Great DX; reproducible | Editor-coupled; learning curve   |
| Kind/Minikube     | Closer to k8s          | Heavier locally; slower feedback |
| Scripts/Makefiles | Minimal tooling        | Fragile; drift across machines   |

### Consequences:

Positive:

- Consistent development environment
- Easy service dependency management
- Simplified onboarding for new developers

Negative:

- Requires Docker knowledge from all developers
- Resource intensive on development machines

Neutral:

- Can migrate to Kubernetes later

---

## ADR 8: Frontend

### Status:

Accepted (July 2025)

### Context:

System needs a frontend to display charts from measured/collected temperature data and to generate API docs with
TypeDoc.

### Decision:

> [!info]- v0
> JavaScript React app via Docker. Reason: quick start.
> Issue: Schema changes not caught at build time caused runtime UI errors (no static typing).

> [!info]- v1
> TypeScript React with Create React App (CRA). Reason: typing and better tooling.  
> Issue: TypeDoc generation failed due to CRA/tooling version conflicts.

React + TypeScript built with Vite for the frontend.

### Alternatives Considered:

| Option         | Pros                           | Cons                            |
|----------------|--------------------------------|---------------------------------|
| CRA (TS)       | Familiar, out-of-the-box setup | Tooling conflicts with TypeDoc  |
| Next.js        | SSR/ISR, ecosystem             | Unneeded complexity for our use |
| Custom Webpack | Full control                   | More maintenance                |

### Consequences:

Positive:

- TypeDoc works
- faster startup
- lean tooling

Negative:

- Some devs must learn Vite

Neutral:

- No server-side rendering (SSR) required

---

## ADR 9: Hardware Platform Decision (board, sensors)

### Status:

Accepted (July 2025)

### Context:

MKR1010 and ADT7410 were provided. Requirements: offline buffering, precise time, dual sites.

### Decision:

Use Arduino MKR1010 with RTC DS3231 and SD card; deploy two identical units.

### Alternatives Considered:

| Option               | Pros                               | Cons                                   |
|----------------------|------------------------------------|----------------------------------------|
| ESP32 boards         | Wi-Fi integrated; strong community | Different toolchain; requalification   |
| Different sensors    | Potential accuracy/cost benefits   | Revalidation effort; integration risk  |
| Single hardware unit | Simpler setup                      | No north/south comparison; less robust |

### Consequences:

Positive:

- Local data persistence via SD card enables offline data storage for ≤24h
- Timestamp reliability through RTC with battery
- Compact hardware, low power, WiFi-ready (MKR1010)

Negative:

- RTC and SD modules require additional wiring and SPI/I2C handling
- Time must be synchronized manually once (e.g., via compile-time setting or initial sync)

Neutral:

- The Arduino MKR1010 was predefined, not evaluated
- Final visualization and backend will depend on further platform choices (e.g., MQTT, REST, database)

---

## ADR 10: Arduino Development Environment Decision

### Status:

Accepted (July 2025)

### Context:

Arduino firmware needs modular builds and host-side unit tests.

### Decision:

PlatformIO for builds; Unity with native target for tests.

### Alternatives Considered:

| Option          | Pros                   | Cons                                  |
|-----------------|------------------------|---------------------------------------|
| Arduino IDE     | Easy; official         | No native tests; inflexible structure |
| CMake toolchain | Flexible; IDE-agnostic | More setup; custom plumbing           |
| Ceedling        | Solid C test framework | Extra integration effort              |

### Consequences:

Positive:

- Reproducible builds and consistent project structure
- PC-native unit tests for business logic (Unity, native target)
- Seamless VS Code integration
- Use of modern C++ structure and dependency management

Negative:

- Additional setup effort for non-Arduino users (e.g., Unity, test runners)
- Developers must learn PlatformIO’s structure (src/lib/test)

Neutral:

- The PlatformIO toolchain abstracts away the underlying GCC setup
- Unit tests cannot cover board-specific behavior (e.g., Wire, SD, RTC) directly without mocks

## Sources

[Documenting Architecture Decisions by Michael Nygard](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
