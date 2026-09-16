# Container monitoring

The worker inspects associated Docker containers through `IContainerMonitor`. Domain code never references Docker.DotNet.

## Association

When registering a service, set `containerName` to the Compose container name:

```json
{
  "name": "payments-api",
  "baseUrl": "http://demo-unhealthy",
  "healthEndpoint": "/health",
  "containerName": "demo-unhealthy",
  "monitoringEnabled": true
}
```

`GET /api/services/{id}/runtime` returns application health plus the last container snapshot (running, restart count, Docker health, CPU %, memory).

## Incidents

Created only for unexpected events:

- container was running and then stopped or disappeared
- restart count increases and reaches the configured threshold (default 3)
- Docker health status is `unhealthy`

Ordinary first observations and a disabled Docker client do not open incidents.

## Local demo

Compose mounts `/var/run/docker.sock` into `devops-worker` and starts `demo-restarting` (Alpine that exits every 3 seconds). After migrate, the API seeds:

- `payments-api` → container `demo-unhealthy`
- `crash-loop` → container `demo-restarting`

To see a stop event:

```powershell
docker stop demo-unhealthy
```

Then `GET /api/services` / `GET /api/incidents` and Grafana.

Unit tests use `FakeContainerMonitor`. They do not need a Docker daemon.
