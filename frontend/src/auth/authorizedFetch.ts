import { getDevRole } from "./devRole";
import { apiBaseUrl, useDevAuth } from "./config";
import { getAccessToken } from "./token";

export async function authorizedFetch(url: string, init?: RequestInit): Promise<Response> {
  const absolute = url.startsWith("http") ? url : `${apiBaseUrl}${url}`;

  async function once(forceRefresh: boolean): Promise<Response> {
    const token = await getAccessToken(forceRefresh);
    const headers = new Headers(init?.headers);
    if (token) {
      headers.set("Authorization", `Bearer ${token}`);
    }
    if (useDevAuth && getDevRole() === "Admin") {
      headers.set("X-Dev-Role", "Admin");
    }
    return fetch(absolute, { ...init, headers });
  }

  const response = await once(false);
  if (response.status !== 401 || useDevAuth) {
    return response;
  }
  return once(true);
}
