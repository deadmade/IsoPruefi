# Dokumentation der Docker Development Umgebung

Diese Dokumentation gibt einen Überblick über die verwendeten Docker-Container, deren Funktion sowie deren Adressen.

---

## Übersicht der Container

| Container                 | Image                           | Beschreibung                                                                                        | Adresse                                                                                    |
| ------------------------- | ------------------------------- | --------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| **traefik**               | `traefik:3.4.4`                 | Reverse-Proxy und Load Balancer für den Zugriff von außen auf unsere Container. (HTTPS Zertifikate) | [traefik.localhost](https://traefik.localhost), Ports: `80`, `443`, Dashboard-Port: `8432` |
| **influxdb**              | `influxdb:3.2.1-core`           | Zeitreihendatenbank (InfluxDB 3.x) zur Datenspeicherung                                             | Port: `8181`                                                                               |
| **influxdb-explorer**     | `influxdata/influxdb3-ui:1.0.3` | Weboberfläche zur Verwaltung und Abfrage von InfluxDB-Daten                                         | [explorer.localhost](https://explorer.localhost), Port: `8888`                             |
| **postgres**              | `postgres:alpine3.21`           | PostgreSQL Datenbank zur relationalen Datenspeicherung                                              | Port: `5432`                                                                               |
| **loki**                  | `grafana/loki:3.5.2`            | Logaggregation und -verwaltung                                                                      | Port: `3100`                                                                               |
| **prometheus**            | `prom/prometheus:v3.4.2`        | Monitoring-Tool zur Sammlung und Auswertung von Metriken                                            | Port: `9090`                                                                               |
| **alloy**                 | `grafana/alloy:v1.9.2`          | Observability-Plattform zur Integration von Loki und Prometheus                                     | Ports: `12345`, `4317`, `4318`                                                             |
| **grafana**               | `grafana/grafana:12.0.2`        | Webbasierte Visualisierung und Dashboard für Metriken und Logs                                      | [grafana.localhost](https://grafana.localhost)                                             |
| **isopruefi-frontend**    | eigener Build (React)           | Frontend-Anwendung                                                                                  | [frontend.localhost](https://frontend.localhost)                                           |
| **isopruefi-backend-api** | eigener Build (.NET REST-API)   | Backend REST API zur Anwendungslogik                                                                | [backend.localhost](https://backend.localhost)                                             |

---

## Netzwerke

Folgende Docker-Netzwerke werden verwendet, um die Container logisch voneinander zu trennen:

- `isopruefi-network`: Allgemeines Netzwerk, genutzt von Traefik.
- `database-network`: Netzwerk für Datenbanken (InfluxDB, PostgreSQL).
- `isopruefi-custom`: Netzwerk für benutzerdefinierte Services (Frontend, Backend).
- `loki`: Netzwerk für Observability-Tools (Loki, Grafana, Alloy).

---

## Details zu wichtigen Containern

### Traefik

Traefik dient als Reverse-Proxy, der sämtliche HTTP(S)-Anfragen entgegennimmt und an die passenden Docker-Container weiterleitet. Es verwaltet automatisch die TLS-Zertifikate und bietet ein Dashboard zur Verwaltung.

### Grafana

Grafana dient der Visualisierung und Analyse von Logs und Metriken. Es ist mit Loki (Logs) und Prometheus (Metriken) verbunden.

### InfluxDB und InfluxDB-Explorer

InfluxDB speichert Zeitreihendaten, während InfluxDB-Explorer eine komfortable Web-Oberfläche bietet, um auf diese Daten zuzugreifen.

### PostgreSQL

PostgreSQL speichert relationale Daten, die von der Backend-API verwendet werden.

