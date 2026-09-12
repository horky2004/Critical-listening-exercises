import { fetchAudio } from "./fetchAudio";
import { resumeAudioContext } from "./audioContext";

const buffers = new Map<string, AudioBuffer>();
const inflight = new Map<string, Promise<AudioBuffer>>();

export async function getAudioBuffer(url: string): Promise<AudioBuffer> {
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
    const buffer = await ctx.decodeAudioData(bytes.slice(0));
    buffers.set(url, buffer);
    return buffer;
  })();

  inflight.set(url, work);
  try {
    return await work;
  } finally {
    inflight.delete(url);
  }
}
