import { apiBaseUrl } from "../auth/config";
import { getAccessToken } from "../auth/token";
import { ApiError } from "../api/client";
import type { ProblemDetails } from "../api/types";

export async function fetchAudio(url: string): Promise<ArrayBuffer> {
  const token = await getAccessToken();
  const headers = new Headers();
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const absolute = url.startsWith("http") ? url : `${apiBaseUrl}${url}`;
  const response = await fetch(absolute, { headers });
  if (!response.ok) {
    throw new ApiError(response.status, "Audio nije dostupan.", {
      status: response.status
    } satisfies ProblemDetails);
  }

  return response.arrayBuffer();
}
