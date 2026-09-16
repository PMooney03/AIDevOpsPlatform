export type Role = "Viewer" | "Operator" | "Administrator";

export interface LoginResponse {
  token: string;
  username: string;
  role: Role;
  expiresAt: string;
}

export interface OverviewResponse {
  totalServices: number;
  healthy: number;
  degraded: number;
  unhealthy: number;
  offline: number;
  unknown: number;
  openIncidents: number;
  criticalIncidents: number;
}

export interface ServiceItem {
  id: string;
  name: string;
  description?: string | null;
  status: string;
  lastHealthCheckAt?: string | null;
  monitoringEnabled: boolean;
  containerName?: string | null;
}

export interface RuntimeResponse {
  serviceId: string;
  serviceName: string;
  applicationStatus: string;
  latestHealth?: {
    responseTimeMs: number;
    httpStatusCode?: number | null;
    message?: string | null;
    checkedAt: string;
    status: string;
  } | null;
  container?: {
    name: string;
    running: boolean;
    restartCount: number;
    health: string;
    cpuPercent?: number | null;
    memoryBytes?: number | null;
    observedAt?: string | null;
  } | null;
}

export interface IncidentItem {
  id: string;
  serviceId: string;
  title: string;
  description?: string | null;
  severity: string;
  status: string;
  detectedAt: string;
  resolvedAt?: string | null;
  resolution?: string | null;
  rootCause?: string | null;
  actionsTaken?: string | null;
}

export interface AnalysisResponse {
  succeeded: boolean;
  modelName?: string | null;
  summary: string;
  probableCause: string;
  severityAssessment: string;
  evidence: string[];
  recommendedChecks: string[];
  suggestedRemediation: string[];
  limitations: string[];
  failureReason?: string | null;
  createdAt: string;
}

export interface SimilarIncident {
  incidentId: string;
  serviceName: string;
  detectedAt: string;
  similarity: number;
  rootCause?: string | null;
  resolution?: string | null;
}

export interface DeploymentItem {
  id: string;
  commitSha: string;
  branch: string;
  buildStatus: string;
  testStatus: string;
  deploymentStatus: string;
  startedAt: string;
  completedAt?: string | null;
}

export interface RemediationItem {
  id: string;
  actionType: string;
  description: string;
  status: string;
  approvedBy?: string | null;
  result?: string | null;
  failureReason?: string | null;
}

export interface HealthHistoryItem {
  checkedAt: string;
  status: string;
  responseTimeMs: number;
  httpStatusCode?: number | null;
  message?: string | null;
}

const TOKEN_KEY = "devops.token";
const USER_KEY = "devops.user";

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function getSession(): { username: string; role: Role } | null {
  const raw = localStorage.getItem(USER_KEY);
  return raw ? (JSON.parse(raw) as { username: string; role: Role }) : null;
}

export function setSession(login: LoginResponse): void {
  localStorage.setItem(TOKEN_KEY, login.token);
  localStorage.setItem(USER_KEY, JSON.stringify({ username: login.username, role: login.role }));
}

export function clearSession(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
}

export function canOperate(role?: Role): boolean {
  return role === "Operator" || role === "Administrator";
}

function summarizeError(body: string, status: number): string {
  if (body.includes("502") || body.includes("Bad Gateway")) {
    return "The API is still starting. Wait a few seconds and sign in again.";
  }
  try {
    const parsed = JSON.parse(body) as { title?: string; errors?: Record<string, string[]> };
    if (parsed.errors) {
      return Object.values(parsed.errors).flat().join(" ");
    }
    if (parsed.title) {
      return parsed.title;
    }
  } catch {
    /* not JSON */
  }
  return body.replace(/<[^>]+>/g, " ").replace(/\s+/g, " ").trim() || `Request failed (${status})`;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers);
  headers.set("Accept", "application/json");
  const token = getToken();
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }
  if (init?.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(path, { ...init, headers });
  if (response.status === 401) {
    clearSession();
    throw new Error("Authentication required.");
  }
  if (response.status === 502 || response.status === 503 || response.status === 504) {
    throw new Error("The API is still starting. Wait a few seconds and sign in again.");
  }
  if (!response.ok) {
    const text = await response.text();
    throw new Error(summarizeError(text, response.status));
  }
  if (response.status === 204) {
    return undefined as T;
  }
  return (await response.json()) as T;
}

export const api = {
  login: (username: string, password: string) =>
    request<LoginResponse>("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ username, password })
    }),
  overview: () => request<OverviewResponse>("/api/overview"),
  services: () => request<ServiceItem[]>("/api/services"),
  service: (id: string) => request<ServiceItem>(`/api/services/${id}`),
  runtime: (id: string) => request<RuntimeResponse>(`/api/services/${id}/runtime`),
  healthHistory: (id: string) => request<HealthHistoryItem[]>(`/api/services/${id}/health/history?limit=20`),
  incidents: (query = "") => request<IncidentItem[]>(`/api/incidents${query}`),
  incident: (id: string) => request<IncidentItem>(`/api/incidents/${id}`),
  analyze: (id: string) => request<AnalysisResponse>(`/api/incidents/${id}/analyze`, { method: "POST" }),
  analysis: (id: string) => request<AnalysisResponse | undefined>(`/api/incidents/${id}/analysis`),
  similar: (id: string) => request<SimilarIncident[]>(`/api/incidents/${id}/similar`),
  resolve: (id: string, body: { resolution: string; rootCause?: string; actionsTaken?: string }) =>
    request<IncidentItem>(`/api/incidents/${id}/resolve`, { method: "POST", body: JSON.stringify(body) }),
  deployments: (serviceId: string) => request<DeploymentItem[]>(`/api/services/${serviceId}/deployments`),
  remediations: (incidentId: string) => request<RemediationItem[]>(`/api/incidents/${incidentId}/remediations`),
  proposeRemediation: (incidentId: string, actionType: string) =>
    request<RemediationItem>(`/api/incidents/${incidentId}/remediations`, {
      method: "POST",
      body: JSON.stringify({ actionType })
    }),
  approveRemediation: (id: string) => request<RemediationItem>(`/api/remediations/${id}/approve`, { method: "POST" }),
  rejectRemediation: (id: string) =>
    request<RemediationItem>(`/api/remediations/${id}/reject`, {
      method: "POST",
      body: JSON.stringify({ reason: "Rejected from dashboard" })
    })
};

export function statusTone(status: string): string {
  const value = status.toLowerCase();
  if (value === "healthy" || value === "resolved" || value === "succeeded" || value === "completed") {
    return "ok";
  }
  if (value === "degraded" || value === "investigating" || value === "recommended") {
    return "warn";
  }
  if (value === "unhealthy" || value === "offline" || value === "critical" || value === "failed") {
    return "bad";
  }
  return "muted";
}
