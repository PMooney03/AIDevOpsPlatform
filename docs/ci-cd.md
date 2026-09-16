# CI/CD and deployments

GitHub Actions restores, builds, and tests the solution on every push and pull request. Docker images are built as a compile check; they are not pushed to a registry.

## Recording a deployment

`POST /api/deployments` stores commit, branch, and stage statuses (`Pending`, `Running`, `Succeeded`, `Failed`). `GET /api/services/{id}/deployments` lists them newest first.

Incident analysis includes the latest deployment for that service. If the incident started within 30 minutes of the deployment completing (or starting if it has no completion time), the prompt notes a possible correlation.

Demo seed data includes a succeeded payments-api deployment about six minutes before “now” so local analysis can show that correlation when an open incident exists.
