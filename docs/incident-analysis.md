# Incident analysis (Ollama)

The API analyses incidents with Ollama in Compose. The worker never calls the model, and monitoring continues if Ollama is down.

## Behaviour

- `POST /api/incidents/{id}/analyze` builds a prompt from stored evidence only: incident text, service status, recent health checks, container snapshot, optional nearby deployment, and similar resolved incidents.
- The model must return JSON with `summary`, `probableCause`, `severityAssessment`, `evidence`, `recommendedChecks`, `suggestedRemediation`, and `limitations`. Invalid JSON is stored as a failed analysis, not as facts.
- Historical incidents are labelled as examples, not current evidence.
- A deployment that finished within 30 minutes before detection is labelled as a possible correlation, not causation.
- Compose sets `Ollama:Enabled=true` and talks to the `ollama` service at `http://ollama:11434`. `dotnet test` and GitHub Actions keep Ollama off.

## Compose (default)

`docker compose up --build` starts `devops-ollama`, waits until it is healthy, then `ollama-init` pulls and warms `${OLLAMA_MODEL:-llama3.2}` (~2 GB). The API does not start until that finishes. Model size vs hardware is in the README **Local LLM** table. Compose defaults to 3B because Docker Desktop on Windows usually runs Ollama on **CPU**.

```text
OLLAMA_MODEL=llama3.1
OLLAMA_TIMEOUT=00:10:00
```

Disable the model without removing the rest of the stack:

```text
OLLAMA_ENABLED=false
```

## Host Ollama instead of the Compose service

Install Ollama on Windows, `ollama pull llama3.2`, then:

```text
OLLAMA_ENABLED=true
OLLAMA_BASE_URL=http://host.docker.internal:11434
OLLAMA_MODEL=llama3.2
```

Do not point Compose at the host until `http://127.0.0.1:11434/api/tags` responds.

## Resolve and similarity

- `POST /api/incidents/{id}/resolve` requires a resolution summary and optional root cause / actions taken.
- `GET /api/incidents/{id}/similar` ranks resolved incidents with Jaccard token overlap in application code (no pgvector). Threshold is 0.12.
