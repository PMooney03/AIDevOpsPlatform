# Security

## Secrets

Passwords and signing keys come from environment / `.env`. `.env` is gitignored. `.env.example` documents names only.

## Authentication

JWT Bearer when `Auth:Enabled=true`. Demo users (`viewer`, `operator`, `admin`) use unique local-only passwords documented in the README. They are for Compose only. Tests and `Auth:Enabled=false` use an in-process bypass identity so `dotnet test` stays hermetic.

## Roles

| Role | Access |
|---|---|
| Viewer | Read services, incidents, analysis, metrics, deployments |
| Operator | Viewer plus analyze, resolve, propose/approve/reject safe remediations |
| Administrator | Operator plus register services and ingest deployments |

## Remediation safety

- No `ExecuteCommand(string)`
- Container target is the **service's stored container name**, never a free-form id from the model
- Recommended → human approve → execute once
- Rejected and already-executed actions cannot run
- Results are stored on `remediation_actions`

## Other controls

Security headers on API responses. CORS is open in Development for the dashboard. Prometheus scrape endpoints are anonymous by design. Deployment POST is administrator-only.
