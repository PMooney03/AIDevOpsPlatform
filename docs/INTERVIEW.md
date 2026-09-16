# Interview notes

**Why C#?** Strong typing, first-class background workers, EF Core, and a realistic internal-platform stack for .NET shops.

**Why a separate worker?** Health polling must not share the API request thread pool or fail because a dashboard user triggered a slow analysis.

**Why local AI?** No paid API, data stays on the machine, and the platform remains useful if the model is down.

**Hallucinations?** Structured JSON validation, evidence-only prompts, failed analyses stored as failures, UI split between observed evidence and AI.

**Human approval?** The model recommends text. Execution is a closed set of methods after `Approve`. Container names come from the service record.

**Incident deduplication?** One non-resolved incident per service; later checks update it.

**Ollama offline?** `UnavailableIncidentAnalyzer` / HTTP errors become stored unsuccessful analyses. Worker does not call Ollama.

**Overload protection?** `MaxConcurrentHealthChecks`, request timeouts, health-row retention.

**Deployments?** Recorded independently. If an incident starts within 30 minutes, analysis may mention a *potential correlation*, never causation.

**SignalR?** Polling every 10s is enough for a handful of demo services and keeps the host simple.
