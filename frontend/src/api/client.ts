import { apiBaseUrl, useDevAuth } from "../auth/config";
import { getDevRole } from "../auth/devRole";
import { getAccessToken } from "../auth/token";
import type { ProblemDetails } from "./types";

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | null;

  constructor(status: number, message: string, problem: ProblemDetails | null) {
    super(message);
    this.status = status;
    this.problem = problem;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = await getAccessToken();
  const headers = new Headers(init?.headers);
  if (!headers.has("Content-Type") && init?.body) {
    headers.set("Content-Type", "application/json");
  }
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }
  if (useDevAuth && getDevRole() === "Admin") {
    headers.set("X-Dev-Role", "Admin");
  }

  const response = await fetch(`${apiBaseUrl}${path}`, { ...init, headers });
  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  const data = text ? (JSON.parse(text) as unknown) : null;

  if (!response.ok) {
    const problem = (data ?? {}) as ProblemDetails;
    throw new ApiError(response.status, problem.detail ?? problem.title ?? response.statusText, problem);
  }

  return data as T;
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "POST", body: body === undefined ? undefined : JSON.stringify(body) }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "PUT", body: body === undefined ? undefined : JSON.stringify(body) })
};
