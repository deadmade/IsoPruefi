# Quality Requirements {#section-quality-scenarios}

## 1. Persistence

Temperature data must be reliably and permanently stored in the database, even in the event of temporary system or connection failures.

**Measurable Criteria:**

- **Data Loss Rate:** A maximum of **0.1%** of all recorded measurements may be lost.
- **Successful Write Operations:** At least **99.9%** of all database write operations must be completed without error.
- **Time to Final Persistence:** Temperature data must be permanently stored in the database within **5 seconds** after being recorded under the condition that there is a working connection.
- **Fallback Storage:** In case of missing connectivity, temperature data is written to the local SD card for up to 24h and synchronized once the connection is restored.
- **Retry and Confirmation:** Failed write operations to the central database are retried until confirmation is received.

**Testability:**

- Disconnect the system from the internet in a controlled way and verify that data is buffered on the SD card and later persisted in the database.
- Simulate database outages to check retry logic and final persistence.
- Run long-term operation tests with daily storage cycles (e.g., multiple days) to verify absence of data loss.

---

## 2. Data Integrity

The recorded data must be correct, complete, and plausible to enable a reliable evaluation of the building's insulation.

**Measurable Criteria:**

- **Inconsistent Data Rate:** Less than **0.05%** of all records may be duplicates, incorrect, or implausible.
- **Validation Error Rate:** A maximum of **0.1%** of data may be rejected by validation mechanisms.
- **Automatic plausibility checks:**

    - **Range validation:** Outdoor readings must stay between -30 °C and 45 °C, indoor readings between -10 °C and 35 °C. Values outside this range are logged as warnings.
    - **Jump detection:** Sudden jumps >10 °C between consecutive readings are flagged.
    - **Difference and mean analysis:** Consecutive differences and moving averages are tracked to detect anomalies.
    - **Statistical window checks:** Mean and standard deviation over a defined time window are used to identify abnormal fluctuations.

**Testability:**

- Inject out-of-range or implausible test data and verify that the system logs warnings or rejects values.
- Simulate sudden temperature jumps to ensure they are flagged.
- Compare sensor readings against expected ranges (indoor vs. outdoor).

---

## 3. Availability

The system must remain functional even in the event of partial failures, so that users can always access the temperature data. Each critical service is deployed redundantly with at least two instances. If one instance fails, Traefik automatically routes traffic to the backup instance. All containers expose health checks, and stateless design ensures fast restart and recovery.

The system is resilient against the following single-instance failures:

- Website (frontend): one instance down → second instance continues serving requests
- REST API: one instance down → second instance handles API traffic
- Weather Data Worker: one instance down → second instance continues scheduled tasks
- MQTT Receiver: one instance down → second instance continues message processing

**Measurable Criteria:**

- **System Availability:** ≥ 99.5% overall operational time (software side)
- **Frontend Data Availability:** ≥ 99.5% of the time, current or last available data is accessible via the UI
- **Resilience Mechanisms:**

    - Redundant service instances per cluster (frontend, backend, workers)
    - Traefik load balancer distributes traffic and enables failover
    - Stateless service design for automatic restart or replacement
    - Health checks for all major containers
    - Local SD storage at Arduino nodes ensures sensor data buffering during backend or network outages

**Testability:**

- Controlled shutdown of one instance per cluster (frontend, REST API, Weather Data Worker, MQTT Receiver) to verify automatic failover via Traefik
- Disable one database or monitoring component to confirm health checks and recovery strategies
- Simulate network outage between Arduino and backend to verify SD-card buffering and later synchronization
- Long-term monitoring of uptime metrics to confirm compliance with ≥ 99.5% availability