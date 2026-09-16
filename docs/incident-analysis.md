# Incident analysis (Ollama)

The API can analyse an incident with a local LLM. The worker never calls the model, and monitoring continues if Ollama is down.

## Behaviour

- `POST /api/incidents/{id}/analyze` builds a prompt from stored evidence only: incident text, service status, recent health checks, container snapshot, optional nearby deployment, and similar resolved incidents.
- The model must return JSON with `summary`, `probableCause`, `severityAssessment`, `evidence`, `recommendedChecks`, `suggestedRemediation`, and `limitations`. Invalid JSON is stored as a failed analysis, not as facts.
- Historical incidents are labelled as examples, not current evidence.
- A deployment that finished within 30 minutes before detection is labelled as a possible correlation, not causation.
- `Ollama:Enabled` defaults to `false`. When disabled, analysis still stores an unavailable result so the API contract stays the same.

## Local Ollama

Install Ollama on the host and pull a model (`llama3.2` is the default name in config). Then set:

```text
Ollama__Enabled=true
Ollama__BaseUrl=http://127.0.0.1:11434
Ollama__Model=llama3.2
```

From Docker Compose, the API uses `http://host.docker.internal:11434`. Do not enable this until the host actually serves `/api/chat`.

## Resolve and similarity

- `POST /api/incidents/{id}/resolve` requires a resolution summary and optional root cause / actions taken.
- `GET /api/incidents/{id}/similar` ranks resolved incidents with Jaccard token overlap in application code (no pgvector). Threshold is 0.12.
