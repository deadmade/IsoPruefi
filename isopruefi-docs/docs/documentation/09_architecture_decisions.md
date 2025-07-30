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

## Sources

[Documenting Architecture Desicions by Michael Nygard](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
