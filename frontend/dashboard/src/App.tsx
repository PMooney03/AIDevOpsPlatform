import { FormEvent, useEffect, useMemo, useState } from "react";
import { Link, Navigate, Route, Routes, useNavigate, useParams } from "react-router-dom";
import {
  AnalysisResponse,
  api,
  canOperate,
  clearSession,
  DeploymentItem,
  getSession,
  HealthHistoryItem,
  IncidentItem,
  OverviewResponse,
  RemediationItem,
  RuntimeResponse,
  ServiceItem,
  setSession,
  SimilarIncident,
  statusTone
} from "./api";

const POLL_MS = 10000;

export default function App() {
  const session = getSession();
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/*" element={session ? <Shell /> : <Navigate to="/login" replace />} />
    </Routes>
  );
}

function Shell() {
  const session = getSession()!;
  const navigate = useNavigate();
  return (
    <div className="layout">
      <aside>
        <p className="brand">AI DevOps</p>
        <p className="muted">Operations platform</p>
        <nav>
          <Link to="/">Overview</Link>
          <Link to="/services">Services</Link>
          <Link to="/incidents">Incidents</Link>
          <Link to="/deployments">Deployments</Link>
        </nav>
        <div className="session">
          <span>{session.username}</span>
          <span className="pill muted">{session.role}</span>
          <button
            type="button"
            onClick={() => {
              clearSession();
              navigate("/login");
            }}
          >
            Sign out
          </button>
        </div>
      </aside>
      <main>
        <Routes>
          <Route path="/" element={<OverviewPage />} />
          <Route path="/services" element={<ServicesPage />} />
          <Route path="/services/:id" element={<ServiceDetailPage />} />
          <Route path="/incidents" element={<IncidentsPage />} />
          <Route path="/incidents/:id" element={<IncidentDetailPage />} />
          <Route path="/deployments" element={<DeploymentsPage />} />
        </Routes>
      </main>
    </div>
  );
}

function LoginPage() {
  const navigate = useNavigate();
  const [username, setUsername] = useState("operator");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    try {
      const login = await api.login(username, password);
      setSession(login);
      navigate("/");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Login failed");
    }
  }

  return (
    <div className="login">
      <form onSubmit={onSubmit} autoComplete="off">
        <h1>AI DevOps Operations Platform</h1>
        <p className="muted">
          Local demo login (not for production): username <code>operator</code>, password{" "}
          <code>LocalOperator-Devops9</code>
        </p>
        <label>
          Username
          <input
            autoComplete="username"
            value={username}
            onChange={(event) => setUsername(event.target.value)}
          />
        </label>
        <label>
          Password
          <input
            type="password"
            autoComplete="off"
            data-lpignore="true"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
        </label>
        {error ? <p className="error">{error}</p> : null}
        <button className="primary" type="submit">
          Sign in
        </button>
      </form>
    </div>
  );
}

function OverviewPage() {
  const [overview, setOverview] = useState<OverviewResponse | null>(null);
  const [incidents, setIncidents] = useState<IncidentItem[]>([]);
  usePolling(async () => {
    setOverview(await api.overview());
    setIncidents(await api.incidents("?status=Open"));
  });

  if (!overview) {
    return <p>Loading overview…</p>;
  }

  return (
    <section>
      <h1>System overview</h1>
      <div className="cards">
        <Stat label="Services" value={overview.totalServices} />
        <Stat label="Healthy" value={overview.healthy} tone="ok" />
        <Stat label="Degraded" value={overview.degraded} tone="warn" />
        <Stat label="Unhealthy" value={overview.unhealthy} tone="bad" />
        <Stat label="Open incidents" value={overview.openIncidents} />
        <Stat label="Critical" value={overview.criticalIncidents} tone="bad" />
      </div>
      <h2>Recent open incidents</h2>
      <IncidentTable items={incidents.slice(0, 8)} />
    </section>
  );
}

function ServicesPage() {
  const [services, setServices] = useState<ServiceItem[]>([]);
  usePolling(async () => setServices(await api.services()));
  return (
    <section>
      <h1>Services</h1>
      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Status</th>
            <th>Container</th>
            <th>Last check</th>
          </tr>
        </thead>
        <tbody>
          {services.map((service) => (
            <tr key={service.id}>
              <td>
                <Link to={`/services/${service.id}`}>{service.name}</Link>
              </td>
              <td>
                <span className={`pill ${statusTone(service.status)}`}>{service.status}</span>
              </td>
              <td>{service.containerName ?? "—"}</td>
              <td>{formatTime(service.lastHealthCheckAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}

function ServiceDetailPage() {
  const { id } = useParams();
  const [runtime, setRuntime] = useState<RuntimeResponse | null>(null);
  const [history, setHistory] = useState<HealthHistoryItem[]>([]);
  const [incidents, setIncidents] = useState<IncidentItem[]>([]);
  const [deployments, setDeployments] = useState<DeploymentItem[]>([]);

  usePolling(async () => {
    if (!id) {
      return;
    }
    setRuntime(await api.runtime(id));
    setHistory(await api.healthHistory(id));
    setIncidents(await api.incidents(`?serviceId=${id}`));
    setDeployments(await api.deployments(id));
  });

  if (!runtime) {
    return <p>Loading service…</p>;
  }

  return (
    <section>
      <h1>{runtime.serviceName}</h1>
      <div className="cards">
        <Stat label="Application" value={runtime.applicationStatus} tone={statusTone(runtime.applicationStatus)} />
        <Stat label="HTTP ms" value={runtime.latestHealth?.responseTimeMs ?? "—"} />
        <Stat label="Container" value={runtime.container ? (runtime.container.running ? "Running" : "Stopped") : "None"} />
        <Stat label="CPU" value={runtime.container?.cpuPercent?.toFixed(1) ?? "—"} />
        <Stat label="Memory" value={formatBytes(runtime.container?.memoryBytes)} />
        <Stat label="Restarts" value={runtime.container?.restartCount ?? "—"} />
      </div>
      <h2>Health history</h2>
      <table>
        <thead>
          <tr>
            <th>Time</th>
            <th>Status</th>
            <th>HTTP</th>
            <th>ms</th>
            <th>Message</th>
          </tr>
        </thead>
        <tbody>
          {history.map((row) => (
            <tr key={row.checkedAt}>
              <td>{formatTime(row.checkedAt)}</td>
              <td>{row.status}</td>
              <td>{row.httpStatusCode ?? "—"}</td>
              <td>{row.responseTimeMs}</td>
              <td>{row.message}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <h2>Incidents</h2>
      <IncidentTable items={incidents} />
      <h2>Deployments</h2>
      <DeploymentTable items={deployments} />
    </section>
  );
}

function IncidentsPage() {
  const [status, setStatus] = useState("");
  const [severity, setSeverity] = useState("");
  const [items, setItems] = useState<IncidentItem[]>([]);
  const query = useMemo(() => {
    const params = new URLSearchParams();
    if (status) params.set("status", status);
    if (severity) params.set("severity", severity);
    const text = params.toString();
    return text ? `?${text}` : "";
  }, [status, severity]);

  usePolling(async () => setItems(await api.incidents(query)), [query]);

  return (
    <section>
      <h1>Incidents</h1>
      <div className="filters">
        <select value={status} onChange={(event) => setStatus(event.target.value)}>
          <option value="">All statuses</option>
          <option>Open</option>
          <option>Investigating</option>
          <option>Resolved</option>
        </select>
        <select value={severity} onChange={(event) => setSeverity(event.target.value)}>
          <option value="">All severities</option>
          <option>Low</option>
          <option>Medium</option>
          <option>High</option>
          <option>Critical</option>
        </select>
      </div>
      <IncidentTable items={items} />
    </section>
  );
}

function IncidentDetailPage() {
  const { id } = useParams();
  const role = getSession()?.role;
  const [incident, setIncident] = useState<IncidentItem | null>(null);
  const [runtime, setRuntime] = useState<RuntimeResponse | null>(null);
  const [analysis, setAnalysis] = useState<AnalysisResponse | null>(null);
  const [similar, setSimilar] = useState<SimilarIncident[]>([]);
  const [deployments, setDeployments] = useState<DeploymentItem[]>([]);
  const [remediations, setRemediations] = useState<RemediationItem[]>([]);
  const [resolution, setResolution] = useState("");
  const [error, setError] = useState<string | null>(null);

  async function refresh() {
    if (!id) {
      return;
    }
    const current = await api.incident(id);
    setIncident(current);
    setRuntime(await api.runtime(current.serviceId));
    setSimilar(await api.similar(id));
    setDeployments(await api.deployments(current.serviceId));
    setRemediations(await api.remediations(id));
    try {
      setAnalysis(await api.analysis(id));
    } catch {
      setAnalysis(null);
    }
  }

  usePolling(refresh, [id]);

  if (!incident) {
    return <p>Loading incident…</p>;
  }

  return (
    <section>
      <h1>{incident.title}</h1>
      <p className="muted">
        {incident.severity} · {incident.status} · detected {formatTime(incident.detectedAt)}
      </p>
      <div className="split">
        <article>
          <h2>Observed evidence</h2>
          <p>{incident.description ?? "No description stored."}</p>
          <p>Application: {runtime?.applicationStatus ?? "unknown"}</p>
          <p>
            Container: {runtime?.container ? `${runtime.container.name} running=${runtime.container.running} restarts=${runtime.container.restartCount}` : "none"}
          </p>
          <p>Latest HTTP: {runtime?.latestHealth?.httpStatusCode ?? "—"} in {runtime?.latestHealth?.responseTimeMs ?? "—"} ms</p>
          <p>Latest deployment: {deployments[0] ? `${deployments[0].commitSha.slice(0, 8)} on ${deployments[0].branch}` : "none recorded"}</p>
        </article>
        <article>
          <h2>AI analysis</h2>
          <p className="muted">Model output is a hypothesis, not a confirmed fact.</p>
          {analysis ? (
            <>
              <p><strong>{analysis.succeeded ? "Succeeded" : "Unavailable"}</strong> {analysis.summary}</p>
              <p>Probable cause: {analysis.probableCause}</p>
              <ul>
                {analysis.limitations.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </>
          ) : (
            <p>No analysis stored yet.</p>
          )}
          {canOperate(role) ? (
            <button type="button" onClick={() => id && api.analyze(id).then(setAnalysis).catch((err) => setError(String(err)))}>
              Run analysis
            </button>
          ) : null}
        </article>
      </div>
      <h2>Similar incidents</h2>
      <table>
        <thead>
          <tr>
            <th>Service</th>
            <th>When</th>
            <th>Score</th>
            <th>Root cause</th>
            <th>Resolution</th>
          </tr>
        </thead>
        <tbody>
          {similar.map((item) => (
            <tr key={item.incidentId}>
              <td>
                <Link to={`/incidents/${item.incidentId}`}>{item.serviceName}</Link>
              </td>
              <td>{formatTime(item.detectedAt)}</td>
              <td>{(item.similarity * 100).toFixed(0)}%</td>
              <td>{item.rootCause}</td>
              <td>{item.resolution}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <h2>Remediation</h2>
      <p className="muted">Predefined actions only. Approval executes immediately; the model cannot run them.</p>
      {canOperate(role) ? (
        <div className="filters">
          {["RunHealthCheck", "RefreshContainerStatus", "CollectRecentLogs", "RestartContainer"].map((action) => (
            <button
              key={action}
              type="button"
              onClick={() => id && api.proposeRemediation(id, action).then(() => refresh()).catch((err) => setError(String(err)))}
            >
              Propose {action}
            </button>
          ))}
        </div>
      ) : null}
      <table>
        <thead>
          <tr>
            <th>Action</th>
            <th>Status</th>
            <th>Approver</th>
            <th>Result</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {remediations.map((item) => (
            <tr key={item.id}>
              <td>{item.actionType}</td>
              <td>{item.status}</td>
              <td>{item.approvedBy ?? "—"}</td>
              <td className="wrap">{item.result ?? item.failureReason ?? item.description}</td>
              <td>
                {canOperate(role) && item.status === "Recommended" ? (
                  <>
                    <button type="button" onClick={() => api.approveRemediation(item.id).then(() => refresh())}>
                      Approve
                    </button>
                    <button type="button" onClick={() => api.rejectRemediation(item.id).then(() => refresh())}>
                      Reject
                    </button>
                  </>
                ) : null}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {incident.status !== "Resolved" && canOperate(role) ? (
        <form
          className="resolve"
          onSubmit={(event) => {
            event.preventDefault();
            api.resolve(incident.id, { resolution }).then(() => refresh()).catch((err) => setError(String(err)));
          }}
        >
          <h2>Resolve</h2>
          <textarea value={resolution} onChange={(event) => setResolution(event.target.value)} required />
          <button type="submit">Resolve incident</button>
        </form>
      ) : (
        <p>Resolution: {incident.resolution ?? "—"}</p>
      )}
      {error ? <p className="error">{error}</p> : null}
    </section>
  );
}

function DeploymentsPage() {
  const [rows, setRows] = useState<{ service: string; items: DeploymentItem[] }[]>([]);
  usePolling(async () => {
    const services = await api.services();
    const grouped = await Promise.all(
      services.map(async (service) => ({ service: service.name, items: await api.deployments(service.id) }))
    );
    setRows(grouped.filter((group) => group.items.length > 0));
  });

  return (
    <section>
      <h1>Deployments</h1>
      {rows.map((group) => (
        <div key={group.service}>
          <h2>{group.service}</h2>
          <DeploymentTable items={group.items} />
        </div>
      ))}
    </section>
  );
}

function IncidentTable({ items }: { items: IncidentItem[] }) {
  return (
    <table>
      <thead>
        <tr>
          <th>Title</th>
          <th>Severity</th>
          <th>Status</th>
          <th>Detected</th>
        </tr>
      </thead>
      <tbody>
        {items.map((item) => (
          <tr key={item.id}>
            <td>
              <Link to={`/incidents/${item.id}`}>{item.title}</Link>
            </td>
            <td>{item.severity}</td>
            <td>
              <span className={`pill ${statusTone(item.status)}`}>{item.status}</span>
            </td>
            <td>{formatTime(item.detectedAt)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function DeploymentTable({ items }: { items: DeploymentItem[] }) {
  return (
    <table>
      <thead>
        <tr>
          <th>Commit</th>
          <th>Branch</th>
          <th>Build</th>
          <th>Tests</th>
          <th>Deploy</th>
          <th>Started</th>
        </tr>
      </thead>
      <tbody>
        {items.map((item) => (
          <tr key={item.id}>
            <td>{item.commitSha.slice(0, 10)}</td>
            <td>{item.branch}</td>
            <td>{item.buildStatus}</td>
            <td>{item.testStatus}</td>
            <td>{item.deploymentStatus}</td>
            <td>{formatTime(item.startedAt)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function Stat({ label, value, tone }: { label: string; value: string | number; tone?: string }) {
  return (
    <div className="stat">
      <span className="muted">{label}</span>
      <strong className={tone ?? ""}>{value}</strong>
    </div>
  );
}

function usePolling(loader: () => Promise<void>, deps: unknown[] = []) {
  useEffect(() => {
    let cancelled = false;
    const run = async () => {
      try {
        if (!cancelled) {
          await loader();
        }
      } catch {
        /* keep last good frame */
      }
    };
    void run();
    const handle = window.setInterval(() => void run(), POLL_MS);
    return () => {
      cancelled = true;
      window.clearInterval(handle);
    };
  }, deps);
}

function formatTime(value?: string | null): string {
  return value ? new Date(value).toLocaleString() : "—";
}

function formatBytes(value?: number | null): string {
  if (!value) {
    return "—";
  }
  return `${(value / (1024 * 1024)).toFixed(1)} MiB`;
}
