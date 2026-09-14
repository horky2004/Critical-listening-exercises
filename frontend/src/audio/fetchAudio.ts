import { authorizedFetch } from "../auth/authorizedFetch";
import { AudioLoadError } from "./audioErrors";

export async function fetchAudio(url: string): Promise<ArrayBuffer> {
  try {
    const response = await authorizedFetch(url);
    if (response.status === 401) {
      throw new AudioLoadError("unauthorized");
    }
    if (!response.ok) {
      throw new AudioLoadError("network");
    }
    return response.arrayBuffer();
  } catch (cause) {
    if (cause instanceof AudioLoadError) {
      throw cause;
    }
    throw new AudioLoadError("network");
  }
}
