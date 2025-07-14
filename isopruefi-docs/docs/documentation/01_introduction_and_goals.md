# Introduction and Goals {#section-introduction-and-goals}

## Aim of our project IsoPrüfi:

Our project aims to test the effectiveness of building insulation based on outside temperature and present the data clearly using diagrams.

## Features:
### Must-Have:

- A website for a user-friendly presentation of temperature comparison diagrams

- Reliable sensors that measure interior temperature

- The ability to retrieve outside temperature data

- Clusterization of containers that we create ourselves

### Should-Have:
- Sensors should be capable of storing temperature data for a period of one day, even in the absence of an internet connection or synchronization with the server

- A website should be used to offer configuration options

### Could-Have:
- Database clustering

### Won't Have:
- The containers will only run on one server, however they are designed to function independently of each other

- Since this is a software project, we won't implement any resilience on the hardware side


## Requirements Overview {#_requirements_overview}

**tbd by Mara**

## Quality Goals {#_quality_goals}

| Quality Goal   |Description                                                                                            |
|----------------|-------------------------------------------------------------------------------------------------------|
| Persistence    | Sensor readings must be logged centrally (database) and  locally (SD card), if offline. No data loss. |
| Data Integrity | Data must include timestamps and checksums to prevent corruption or duplication.                      |
| Availability   | The system must remain partially operational during network outages and recover automatically.        |

## Stakeholders {#_stakeholders}

- sollen wir da uns aufführen und die Coaches + Schütz?

| Role/Name | Contact      | Expectations      |
|-----------|--------------|-------------------|
| *Role-1*  | *Contact-1*  | *Expectation-1*   |
| *Role-2*  | *Contact-2*  | *Expectation-2*   |

