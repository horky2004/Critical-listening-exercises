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

export const MASTER_GAIN_DB = -12;
export const MASTER_GAIN = 10 ** (MASTER_GAIN_DB / 20);
