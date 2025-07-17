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

Functional Requirements:
- The system should provide three data sources: two on the inside and one on the outside
- The data should be updated every 60 seconds
- Each datapoint includes the temperature and time
- The users should be able to see diagramms and an evaluation of the data
- The users can see historical data to observe long term changes
- The system should use containers for deployment

Non-functional:
- The system has an availability of 99.5%
- The system should be reliable even if one of the containers fails
- The data should be persistently saved in the database
- There should be automated unit tests for the core functionalities (correct transmission and successfull saving of data, simulation of failure scenarios)

Constraints:
- The project will be hosted on one server which is hosted by Prof. Hänisch
- The hardware that is used for measuring the indoor temperature is provided by the university
- The project specifies that there are at least two data sources of which at least one is an Arduino
- The Hardware and database are not specifically designed to be reliable
- The date of submissions is the 05.09.2025
- The final system has to run on a clean environment with no prior setup
- Weekly meetings with a coach for discussion of the project

## Quality Goals {#_quality_goals}

| Quality Goal   |Description                                                                                            |
|----------------|-------------------------------------------------------------------------------------------------------|
| Persistence    | Sensor readings must be logged centrally (database) and  locally (SD card), if offline. No data loss. |
| Data Integrity | Data must include timestamps and checksums to prevent corruption or duplication.                      |
| Availability   | The system must remain partially operational during network outages and recover automatically.        |

## Stakeholders {#_stakeholders}

| Role/Name | Expectations | Influence |
|-----------|--------------|-------------------|
| Developer | Solution that is easy to maintain and fulfills all requirements for the project | Quality of Code, clean Architecture, final product |
| Supervisor | Correct methodology, clear documentation and tracability of results | Sets expectations and reviews the final product |
| Coaches | Clear documentation, preparation of meetings and clear presentation of the results for each meeting | Review of the final product and Support for the implementation |
| User/Owner | Want to reduce their heating costs through stable temperature measurements and correct assessment of the building's isolation | Requires easy usability and trustworthy temperature data |
| Systemadministrator | Stable infrastructure, easy deployments and clear logs for easy maintenance | Configuration of the system |

