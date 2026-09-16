# Demo

`docker compose up --build` starts PostgreSQL, API, worker, **Ollama** (`llama3.1` 8B pulled on first start), Prometheus, Grafana, the React dashboard, nginx 500 (`demo-unhealthy`), crash-loop Alpine, and `demo-chaos`.

Dashboard: http://localhost:8081  
Login as `operator` / `LocalOperator-Devops9` (or `admin` / `LocalAdmin-Devops9`, `viewer` / `LocalViewer-Devops9`).

## Scenario 1 — HTTP 500

`payments-api` already targets `demo-unhealthy`. Wait for Unhealthy and an open incident. Click **Run analysis** — the API sends stored evidence to Ollama (`llama3.1`) and saves validated JSON. The model cannot run remediations.

## Scenario 2 — database symptom

```powershell
Invoke-RestMethod -Method Post http://localhost:8092/chaos/database
```

`demo-chaos` health returns 503 with a PostgreSQL message. Recover with `POST /chaos/recover`.

## Scenario 3 — container restarts

`crash-loop` uses `demo-restarting`. After the restart threshold, an incident appears. Propose `RestartContainer` (will bounce a container that exits again) or `RunHealthCheck`. Approval is required.

## Scenario 4 — deployment correlation

Seed data records a payments-api deployment about six minutes before “now”. Open the payments incident and read the latest deployment on the detail page. Analysis prompt includes potential correlation when the timestamps line up.

All `/chaos/*` routes are **DEVELOPMENT / DEMO ONLY**.
