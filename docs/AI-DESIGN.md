# AI design

Incident analysis is an optional assistant. Monitoring, incidents, metrics, and resolution work with Ollama turned off.

## What the model sees

Only stored evidence: incident text, recent health rows, container snapshot, latest deployment, and similar *resolved* incidents labelled as historical examples.

## What the model must not do

It cannot execute remediations or arbitrary commands. Suggested checks are text. Remediation uses a fixed enum (`RestartContainer`, `RunHealthCheck`, `RefreshContainerStatus`, `CollectRecentLogs`) after a human approve.

## Hallucinations

JSON is schema-validated (`summary`, `probableCause`, required). Invalid output is stored as a failed analysis. The UI labels AI as hypothesis, not observed evidence.

## Why not Semantic Kernel

A single `/api/chat` call with JSON format is enough. SK would add orchestration we do not use.

## Similarity

Application-level Jaccard token overlap so InMemory tests stay offline. pgvector can wait until retrieval quality actually needs embeddings.
