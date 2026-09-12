import { useState } from "react";

const STORAGE_KEY = "cll.listenVolume";
export const DEFAULT_LISTEN_VOLUME = 0.8;

function readStoredVolume(): number {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (raw == null) {
      return DEFAULT_LISTEN_VOLUME;
    }
    const value = Number(raw);
    return Number.isFinite(value) ? Math.min(1, Math.max(0, value)) : DEFAULT_LISTEN_VOLUME;
  } catch {
    return DEFAULT_LISTEN_VOLUME;
  }
}

export function useListenVolume(): [number, (volume: number) => void] {
  const [volume, setVolume] = useState(readStoredVolume);

  function update(next: number) {
    const clamped = Math.min(1, Math.max(0, next));
    setVolume(clamped);
    try {
      sessionStorage.setItem(STORAGE_KEY, String(clamped));
    } catch {
      /* private mode */
    }
  }

  return [volume, update];
}
