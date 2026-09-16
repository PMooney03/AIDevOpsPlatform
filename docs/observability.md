# Observability

The platform emits structured JSON logs and Prometheus metrics from both the API and the worker. Prometheus scrapes those endpoints. Grafana is provisioned with three dashboards.

```text
Worker health checks  -->  metrics + JSON logs  -->  Prometheus  -->  Grafana
API HTTP requests     -->  metrics + JSON logs  -->  Prometheus  -->  Grafana
PostgreSQL            -->  incidents / health history (source of truth)
```

Logs use message templates with `ServiceId`, `IncidentId`, `HealthStatus`, HTTP status, and duration. Connection strings and passwords are never written to logs.

## Endpoints

| Process | URL |
|---|---|
| API health | http://localhost:8080/health |
| API metrics | http://localhost:8080/metrics |
| Worker metrics | http://localhost:9464/metrics |
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3000 |

Grafana local login is `admin` / `admin` unless you change `GRAFANA_ADMIN_PASSWORD` in `.env`. Anonymous viewer access is enabled for local demos.

## Metrics

Custom instruments live on meter `DevOps.Platform`:

| Instrument | Type | Meaning |
|---|---|---|
| `devops_health_checks` | counter | Every probe |
| `devops_health_checks_failed` | counter | Probe did not return HTTP 2xx |
| `devops_service_response_time` | histogram (ms) | Probe duration |
| `devops_incidents` | counter | Created or updated (`action`, `severity`) |
| `devops_services_monitored` | gauge | Monitoring enabled |
| `devops_services_healthy` | gauge | Current Healthy count |
| `devops_services_unhealthy` | gauge | Unhealthy + Offline |
| `devops_incidents_open` | gauge | Open or Investigating |

Prometheus adds `_total` to counters. The histogram is exported as `devops_service_response_time_milliseconds_*`.

ASP.NET and runtime metrics are also scraped.

## Dashboards

Provisioned under **AI DevOps Platform**:

- **Platform Overview** — monitored / healthy / unhealthy services and active incidents
- **Service Health** — latency, failed checks, status mix
- **Incident Metrics** — created over time and by severity

## Seeing a failure in Grafana

`demo-unhealthy` always returns HTTP 500. After Compose is up:

1. Confirm `GET /api/services` shows `payments-api` as `Unhealthy`.
2. Open http://localhost:8080/metrics and http://localhost:9464/metrics — worker gauges should show unhealthy services and open incidents.
3. Open Prometheus targets at http://localhost:9090/targets — both jobs should be UP.
4. Open Grafana and the three provisioned dashboards. **Incident Metrics** uses current open-incident gauges (`devops_incidents_open` and `devops_incidents_open_by_severity`), not only `created` counters, so a long-lived incident still shows up after worker restarts.
