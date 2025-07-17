# Introduction and Goals {#section-introduction-and-goals}

## Aim of our project IsoPrüfi:

Our project aims to test the effectiveness of building insulation based on outside temperature and present the data clearly using diagrams.

## Features
### Must-Have

- A website for a user-friendly presentation of temperature comparison diagrams

- Reliable sensors that measure interior temperature

- The ability to retrieve outside temperature data

- Clusterization of containers that we create ourselves

### Should-Have
- Sensors should be capable of storing temperature data for a period of one day, even in the absence of an internet connection or synchronization with the server

- A website should be used to offer configuration options

### Could-Have
- Database clustering

### Won't Have
- The containers will only run on one server, however they are designed to function independently of each other

- Since this is a software project, we won't implement any resilience on the hardware side


## Requirements Overview {#_requirements_overview}

### Functional Requirements

- The system must provide three data sources: two for indoor measurements and one for outdoor measurements
- Data should be updated every 60 seconds
- Each data point must include both temperature and timestamp
- Users must be able to view diagrams and evaluations of the collected data
- Users should have access to historical data to observe long-term trends
- The system must use containers for deployment
- In case of no database connection, the temperature data will be saved on an SD card for up to 24 h

### Non-Functional Requirements

- The system should achieve an availability of 99.5%
- The system must remain reliable even if one container fails
- Data must be persistently stored in the database
- Automated unit tests must cover core functionalities, including correct data transmission, successful data storage, and simulation of failure scenarios

### Constraints

- The project will be hosted on a single server provided by Prof. Hänisch
- Indoor temperature measurement hardware is supplied by the university
- At least two data sources are required, with at least one being an Arduino device
- The hardware and database are not specifically designed for high reliability
- The submission deadline is 05.09.2025
- The final system must run in a clean environment with no prior setup required
- Weekly meetings with a coach are scheduled for project discussions

## Quality Goals {#_quality_goals}

| Quality Goal   |Description                                                                                            |
|----------------|-------------------------------------------------------------------------------------------------------|
| Persistence    | Sensor readings must be logged centrally (database) and  locally (SD card), if offline -> No data loss |
| Data Integrity | Data must include timestamps and checksums to prevent corruption or duplication                      |
| Availability   | The system must remain partially operational during network outages and recover automatically        |

## Stakeholders {#_stakeholders}

| Role/Name | Expectations | Influence |
|-----------|--------------|-------------------|
| Developer | Solution that is easy to maintain and fulfills all requirements for the project | Quality of Code, clean Architecture, final product |
| Supervisor | Correct methodology, clear documentation and tracability of results | Sets expectations and reviews the final product |
| Coaches | Clear documentation, preparation of meetings and clear presentation of the results for each meeting | Review of the final product and Support for the implementation |
| User/Owner | Want to reduce their heating costs through stable temperature measurements and correct assessment of the building's isolation | Requires easy usability and trustworthy temperature data |
| Systemadministrator | Stable infrastructure, easy deployments and clear logs for easy maintenance | Configuration of the system |

