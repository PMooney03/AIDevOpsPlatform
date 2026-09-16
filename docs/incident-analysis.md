# Incident analysis (Ollama)

The API analyses incidents with Ollama in Compose. The worker never calls the model, and monitoring continues if Ollama is down.

## Behaviour

- `POST /api/incidents/{id}/analyze` builds a prompt from stored evidence only: incident text, service status, recent health checks, container snapshot, optional nearby deployment, and similar resolved incidents.
- The model must return JSON with `summary`, `probableCause`, `severityAssessment`, `evidence`, `recommendedChecks`, `suggestedRemediation`, and `limitations`. Invalid JSON is stored as a failed analysis, not as facts.
- Historical incidents are labelled as examples, not current evidence.
- A deployment that finished within 30 minutes before detection is labelled as a possible correlation, not causation.
- Compose sets `Ollama:Enabled=true` and talks to the `ollama` service at `http://ollama:11434`. `dotnet test` and GitHub Actions keep Ollama off.

## Compose (default)

`docker compose up --build` starts `devops-ollama`, waits until it is healthy, then `ollama-init` pulls `${OLLAMA_MODEL:-llama3.1}` (~5 GB the first time). The API does not start until that pull finishes. 8B is the default for a 32 GB workstation; `llama3.2` (3B) still works if you want a smaller download.

Disable the model without removing the rest of the stack:

```text
OLLAMA_ENABLED=false
```

## Host Ollama instead of the Compose service

Install Ollama on Windows, `ollama pull llama3.1`, then:

```text
OLLAMA_ENABLED=true
OLLAMA_BASE_URL=http://host.docker.internal:11434
OLLAMA_MODEL=llama3.1
```

Do not point Compose at the host until `http://127.0.0.1:11434/api/tags` responds.

## Resolve and similarity

- `POST /api/incidents/{id}/resolve` requires a resolution summary and optional root cause / actions taken.
- `GET /api/incidents/{id}/similar` ranks resolved incidents with Jaccard token overlap in application code (no pgvector). Threshold is 0.12.
