# Quality Characteristics

## 1. Persistence

Temperature data must be reliably and permanently stored, even in the event of temporary system or connection failures.

**Measurable Criteria:**
- **Data Loss Rate:** A maximum of **0.1%** of all recorded measurements may be lost.
- **Successful Write Operations:** At least **99.9%** of all database write operations must be completed without error.
- **Time to Final Persistence:** Temperature data must be permanently stored within **5 seconds** after being recorded.

**Testability:**
- **Offline Test:** Disconnect the system from the internet in a controlled way; data must be buffered and correctly persisted once the connection is restored.
- **Long-Term Scenario:** Simulate long-term operation with daily storage cycles (e.g., for more than 1 day).

---

## 2. Data Integrity

The recorded data must be correct, complete, and plausible to enable a reliable evaluation of the building's insulation.

**Measurable Criteria:**
- **Inconsistent Data Rate:** Less than **0.05%** of all records may be duplicates, incorrect, or implausible (e.g., timestamps in the future).
- **Validation Error Rate:** A maximum of **0.1%** of data may be rejected by validation mechanisms.
- **Average Timestamp Deviation (Backend ↔ Database):** The time difference between system timestamps and stored data must not exceed **2 seconds** on average.

**Testability:**
- Manual or automated validation with test data (e.g., intentionally incorrect timestamps or outliers).
- Comparison of timestamps between application modules (synchronization test).

---

## 3. Availability

The system must remain functional even in the event of partial failures, so that users can always access the current or most recently available temperature data.

**Measurable Criteria:**
- **System Availability:** The system must be fully operational (software side) at least **99.5%** of the total time.
- **Frontend Data Availability:** In at least **99.5%** of the time, current or last available data must be accessible in the user interface.
- **Downtimes:** A maximum of **2 unplanned outages per month**, or **1 planned/short downtime per week**.

**Testability:**
- Controlled shutdown of individual containers (e.g., website, weather data worker, MQTT receiver, REST API) to verify recovery mechanisms and availability handling.
