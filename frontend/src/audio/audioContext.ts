let context: AudioContext | null = null;

export function getAudioContext(): AudioContext {
  context ??= new AudioContext();
  return context;
}

export async function resumeAudioContext(): Promise<AudioContext> {
  const ctx = getAudioContext();
  if (ctx.state === "suspended") {
    await ctx.resume();
  }
  return ctx;
}

const masterLinear = 10 ** (-12 / 20);

export const MASTER_GAIN = masterLinear;
