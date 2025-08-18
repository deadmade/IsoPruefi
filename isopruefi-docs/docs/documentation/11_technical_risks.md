# Risks and Technical Debts {#section-technical-risks}

> Insert SFMEA here

## 1. Description of the Process/System

**Overview of the Entire Product:**

- **Temperature Measurement and Transmission:**  
    - Involves temperature sensors, RTC modules, Arduino, SD module/card, and access to online weather data
- **Data Storage:**  
    - Utilizes a database for storing temperature data
- **Analysis/Evaluation:**  
    - Data is analyzed and evaluated, with results visualized via website or analytics tools

**Components Involved:**

- **Temperature Measurement:**  
    - Temperature sensors, RTC modules, Arduino, SD module/card, online weather data availability
- **Temperature Transmission:**  
    - Network availability, server infrastructure
- **Data Storage:**  
    - Database systems
- **Visualization/Analysis:**  
    - Data availability, website, analytics platforms

**Process Aspects:**

- Data flow throughout the system
- Handling of failure and recovery scenarios


---

## 2. Error Analysis

### Possible Errors

- Incorrect or missing data
- Unavailability of services or functions (e.g., website, Grafana)

### Causes

- Compatibility issues due to software or hardware updates
- Security vulnerabilities
- **Temperature Measurement:**
    - Sensor errors (e.g., incorrect calibration, hardware malfunction, sensor failure, power supply issues, incorrect interval configuration)
    - Misassignment of data (e.g., north/south confusion)
    - Weather service outages
- **Data Transmission:**
    - Network outages or connectivity issues
    - Duplicate data transmission
- **Data Storage:**
    - Incorrect or duplicate entries
    - Database corruption or failure
- **Visualization/Analysis:**
    - Website or Grafana unavailability
    - Incorrect data presented for visualization

### Impacts

- Gaps in data analysis
- Misinterpretation or incorrect assessment of results
- Lack of long-term evaluation or comparison basis
- No or limited access to collected data

---

## 3. Evaluation of Errors and Consequences

| Error                        | Probability of Occurrence | Severity         | Probability of Detection           | Risk Priority Number |
|------------------------------|:------------------------:|:---------------:|:----------------------------------:|:-------------------:|
| Sensor error                 | 2-3 (unlikely)           | 8-9 (severe)    | 2-3 (inevitable detection)         | 32-81               |
| Misassignment of data        | 3 (low)                  | 6-7 (disturbance)| 5-6 (only detected during targeted checks) | 90-126      |
| Weather service outage       | 1 (almost impossible)    | 8-9 (severe)    | 2-3 (inevitable detection)         | 16-27               |
| Network outage               | 2 (unlikely)             | 8-9 (severe)    | 2-3 (inevitable detection)         | 31-45               |
| Duplicate transmission       | 2 (unlikely)             | 2 (irrelevant) | 5-6 (only detected during targeted checks) | 20-24        |
| Incorrect/missing entries    | 2-3 (unlikely)           | 8-9 (severe)    | 3-4 (high probability of detection)| 48-108              |
| Database corruption          | 2-3 (unlikely)           | 8-9 (severe)    | 3-4 (high probability of detection)| 48-108              |
| Website/Grafana malfunction  | 1 (almost impossible)    | 8-9 (severe)    | 2-3 (inevitable detection)         | 16-27               |
| Power outage               | 3 (low)             | 8-9 (severe)    | 2-3 (inevitable detection)         | 48-81               |


---

## 4. Corrective actions

| Error                        | Risk Priority Number | Mitigation Measure                                   |
|------------------------------|:-------------------:|------------------------------------------------------|
| Sensor error                 | 32-81               | -             |
| Misassignment of data        | 90-126              | Implement data validation and labeling checks         |
| Weather service outage       | 16-27               | Use fallback data sources               |
| Network outage               | 31-45               | Local storage of data on the Arduino                  |
| Duplicate transmission       | 20-24               | -         |
| Incorrect/missing entries    | 48-108              | Input validation |
| Database corruption          | 48-108              | -                  |
| Website/Grafana malfunction  | 16-27               | Monitor uptime |

---

## 5. Technical debts

## Sources

[FMEA from the Orgahandbuch (Bundesministerium des Inneren)](https://www.orghandbuch.de/Webs/OHB/DE/Organisationshandbuch/6_MethodenTechniken/63_Analysetechniken/633_FehlermoeglichkeitUndEinflussanalyse/fehlermoeglichkeitundeinflussanalyse-node.html)
