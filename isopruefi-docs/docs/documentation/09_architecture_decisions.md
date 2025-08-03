# Architecture Decisions {#section-design-decisions}

## ADR Template

### Title: 
ADR xx: _Short Description_

### Context: 
This section shortly describes the problem or context, the possible options and possibly their pros and cons.

### Decision: 
This section describes our solution and explains our decision.

### Status:
For example accepted, deprecated or superseded.

### Consequences:
This section describes the resulting context, after applying the decision. All consequences should be listed here, not just the "positive" ones. A particular decision may have positive, negative, and neutral consequences, but all of them affect the team and project in the future.


## ADR 0: Example

### Context:
Our frontend project currently uses plain JavaScript (React). Recently, we've encountered recurring bugs caused by the lack of type checking (e.g., undefined errors on objects, incorrect API responses).
The team discussed several options to improve code quality and maintainability:

| Option        | Pros      | Cons      |
|---------------|:----------|:----------|
| Continue with JS | No migration | Less reliable |
| Adopt TypeScript | Strong typing | Migration effort |
| Static Analysis Tool | Lighter | Less widely used |

### Decision:
We decided to gradually adopt TypeScript, starting with new files and later migrating existing modules. The benefits (better error prevention, maintainability, developer productivity) clearly outweigh the migration cost.

### Status:
Accepted (25.07.2025)

### Consequences: 
Positive:

- Fewer runtime errors due to static type checking

- Improved auto-completion and tooling support

- Clearer interfaces for team collaboration

Negative:

- Migration effort for existing JavaScript files

- Developers need to learn TypeScript

- CI builds may initially slow down due to additional checks

Neutral:

- Build configuration needs to be updated (e.g., Babel, tsconfig.json)

- Slightly higher onboarding curve for new developers


## ADR 1: Backend Technology Stack

### Context:
Need robust backend technology for REST API and worker services. Team has existing familiarity with C# development.

### Decision:
Use .NET 9 with C# for all backend services (REST API, MQTT Receiver, Weather Worker).

### Status:
Accepted

### Consequences:

Positive:

- Team familiarity reduces development time
- Strong typing prevents runtime errors
- Excellent tooling and debugging support
- Modern async/await support for I/O operations

Negative:

- Platform dependency (though mitigated by containers)
- Larger memory footprint than some alternatives

## ADR 2: Microservices Architecture

### Context:
System has distinct responsibilities: API serving, MQTT message processing, and weather data fetching. Need modularity and independent scaling.

### Decision:
Split backend into separate services: REST API, MQTT Receiver Worker, Weather Data Worker.

### Status:
Accepted

### Consequences:
Positive:

- Clear separation of concerns
- Independent scaling and deployment
- Fault isolation between services

Negative:

- Increased deployment complexity
- Network communication overhead between services

## ADR 3: Dual Database Strategy

### Context:
System needs both structured application data (users, authentication) and time-series sensor data with different access patterns.

### Decision:
Use PostgreSQL for application data and InfluxDB for time-series sensor data.

### Status:
Accepted

### Consequences:
Positive:

- PostgreSQL optimized for relational data and transactions
- InfluxDB optimized for time-series queries and compression
- Each database serves its specific use case efficiently

Negative:

- Two databases to maintain and backup

## ADR 4: Observability Stack

### Context:
Distributed microservices architecture requires comprehensive monitoring, logging, and alerting capabilities.

### Decision:
Implement Grafana for dashboards, Loki for log aggregation, and Prometheus for metrics collection.

### Status:
Accepted

### Consequences:
Positive:

- Complete observability into system health and performance
- Industry-standard tools with good integration
- Unified dashboard for all monitoring data

Negative:

- Additional infrastructure to maintain

## ADR 5: Traefik as Reverse Proxy

### Context:
Multiple services need unified entry point, SSL termination, and service discovery in containerized environment.

### Decision:
Use Traefik as reverse proxy with automatic service discovery and HTTPS termination.

### Status:
Accepted

### Consequences:
Positive:

- Automatic service discovery via Docker labels
- Built-in SSL certificate management
- Load balancing capabilities

Negative:

- Single point of failure if not properly configured
- Additional configuration complexity

## ADR 6: JWT Authentication Strategy

### Context:
REST API requires secure authentication mechanism. Need stateless authentication for microservices architecture.

### Decision:
Implement JWT token-based authentication with Entity Framework for user management.

### Status:
Accepted

### Consequences:
Positive:

- Stateless authentication scales well
- Standard approach with good library support
- Tokens can carry user claims

Negative:

- Token revocation complexity
- Requires secure token storage on client side

## ADR 7: Docker Compose for Development Environment

### Context:
Complex multi-service architecture needs consistent development environment setup across team members.

### Decision:
Use Docker Compose to orchestrate all services for local development.

### Status:
Accepted

### Consequences:
Positive:

- Consistent development environment
- Easy service dependency management
- Simplified onboarding for new developers

Negative:

- Requires Docker knowledge from all developers
- Resource intensive on development machines

## ADR 8: frontend

### Context:
The IsoPruefi requires a proper frontend to display charts based on the measured temperature data.

Originally, the frontend was auto-generated using Docker and based on a JavaScript React setup. 
Later, it was decided to migrate to a TypeScript-based React app, due to the advantages 
TypeScript offers in terms of type safety and compiler support.

However, the chosen setup was built using Create React App (CRA), 
which led to problems with documentation generation using TypeDoc, due to version incompatibility.

### Decision:
It was decided to replace CRA support with the Vite-based TS React project.

### Conclusion:
Accepted.

### Consequences:
Neutral: 

- The frontend part of IsoPruefi runs completely on Vite

Positive:

- TypeDoc works correctly and the frontend documentation is generated.
- The startup time improved comparing to CRA

## ADR 9: Hardware Platform Decision (board, sensors)

### Context:
The aim of our project IsoPrüfi is to evaluate the thermal insulation performance of buildings by comparing indoor and outdoor temperature data and visualizing the results through a web interface.

The microcontroller hardware was predefined: we were provided with the Arduino MKR1010 and a temperature sensor (Analog Devices ADT7410 Breakout). Based on the functional requirements of the system, we extended the setup with:

- A Real-Time Clock (RTC) with battery backup (DS3231)
- An SD card module
- Two identical hardware units for parallel measurements on the north and south sides of the building 

These components were selected to fulfill the need for offline data buffering, accurate timestamping, and reliable long-term measurements.

### Decision:
We used the Arduino MKR1010, as it was provided and meets the basic requirements (WiFi, sufficient RAM, low-power mode).
We deliberately added:

- An RTC module, to ensure precise timestamping regardless of power loss
- An SD card module for local data buffering in case of network or MQTT broker disconnection
- Two identical devices, to allow side-by-side comparison

### Status:
Accepted 

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

## ADR 10: Development Environment Decision – PlatformIO for Arduino scripting and unit testing with Unity

### Context:
We developed firmware for the Arduino MKR1010 to collect and buffer temperature data. To support structured development, modularization, and automated testing, we needed a build and test environment that:

- Supports the Arduino MKR1010 and SAMD21-based boards
- Enables integration of external libraries and custom source structure
- Allows automated builds and unit testing (preferably on PC)

We considered the following options:

| Option        | Pros      | Cons      |
|---------------|:----------|:----------|
| Arduino IDE | Easy to use, official support | No native testing, inflexible project structure |
| PlatformIO + Unity | IDE integration, native/unit testing, modular build	 | Slight learning curve, more setup |

### Decision:
We chose PlatformIO as our development environment and used Unity (with PlatformIO’s native target) for writing and executing unit tests. This setup allows us to:

- Use modern C++ structure and dependency management
- Build and flash firmware consistently
- Run platform-independent unit tests on PC (outside the Arduino board)

### Status:
Accepted 

### Consequences:
Positive:

- Reproducible builds and consistent project structure
- Platform-independent unit tests for business logic using Unity and native target
- Seamless integration into VS Code
- Easier onboarding and maintenance with centralized configuration (platformio.ini)

Negative:

- Additional setup effort for non-Arduino users (e.g., Unity, test runners)
- Developers must learn PlatformIO’s structure (src/lib/test)

Neutral:

- The PlatformIO toolchain abstracts away the underlying GCC setup
- Unit tests cannot cover board-specific behavior (e.g., Wire, SD, RTC) directly without mocks




## Sources

[Documenting Architecture Decisions by Michael Nygard](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
