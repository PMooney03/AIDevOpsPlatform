# Monitoring

The worker loads enabled services, probes health URLs with configured timeouts, and inspects associated containers through `IContainerMonitor`.

## Health rules

Configured in `Monitoring` (not constants): healthy 2xx under the latency threshold, degraded slow/partial failure, unhealthy after consecutive failures, offline after a configured unreachable period.

## Concurrency and retention

Each cycle runs at most `MaxConcurrentHealthChecks` probes. Health rows older than `HealthCheckRetentionDays` are deleted. Incidents are not auto-deleted.

## Docker

Incidents are opened for unexpected stop, restart-threshold crossings, and Docker unhealthy — not for first observation of a stopped container.

## Observability

JSON console logs with ServiceId/IncidentId. Prometheus scrapes API `:8080/metrics` and worker `:9464/metrics`. Grafana is provisioned from `monitoring/grafana`.
