import { AudioLoadError } from "./audioErrors";
import { fetchAudio } from "./fetchAudio";
import { getAudioContextGeneration, resumeAudioContext } from "./audioContext";

const buffers = new Map<string, AudioBuffer>();
const inflight = new Map<string, Promise<AudioBuffer>>();
let boundGeneration = 0;

export function clearAudioBuffers(): void {
  buffers.clear();
  inflight.clear();
  boundGeneration = getAudioContextGeneration();
}

export async function getAudioBuffer(url: string): Promise<AudioBuffer> {
  const generation = getAudioContextGeneration();
  if (boundGeneration !== generation) {
    buffers.clear();
    boundGeneration = generation;
  }

  const cached = buffers.get(url);
  if (cached) {
    return cached;
  }

  const pending = inflight.get(url);
  if (pending) {
    return pending;
  }

  const work = (async () => {
    const ctx = await resumeAudioContext();
    const bytes = await fetchAudio(url);
    try {
      const buffer = await ctx.decodeAudioData(bytes.slice(0));
      buffers.set(url, buffer);
      return buffer;
    } catch {
      throw new AudioLoadError("decode");
    }
  })();

  inflight.set(url, work);
  try {
    return await work;
  } finally {
    inflight.delete(url);
  }
}
