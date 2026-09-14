export const MASTER_GAIN_DB = -12;
export const MASTER_GAIN = 10 ** (MASTER_GAIN_DB / 20);

type StateListener = (state: string) => void;

let context: AudioContext | null = null;
let generation = 0;
const listeners = new Set<StateListener>();

function notify(state: string) {
  for (const listener of listeners) {
    listener(state);
  }
}

function attach(ctx: AudioContext) {
  ctx.addEventListener("statechange", () => notify(ctx.state));
}

export function getAudioContextGeneration(): number {
  return generation;
}

export function getAudioContext(): AudioContext {
  if (!context || context.state === "closed") {
    context = new AudioContext();
    generation += 1;
    attach(context);
    notify(context.state);
  }
  return context;
}

export function subscribeAudioContextState(listener: StateListener): () => void {
  listeners.add(listener);
  listener(context?.state ?? "suspended");
  return () => {
    listeners.delete(listener);
  };
}

export function isAudioContextInterrupted(state: string): boolean {
  return state === "interrupted" || state === "closed";
}

export async function recreateAudioContext(): Promise<AudioContext> {
  if (context && context.state !== "closed") {
    try {
      await context.close();
    } catch {
      /* already closed */
    }
  }
  context = new AudioContext();
  generation += 1;
  attach(context);
  notify(context.state);
  return context;
}

export async function resumeAudioContext(): Promise<AudioContext> {
  let ctx = getAudioContext();
  if (ctx.state === "suspended" || ctx.state === "interrupted") {
    try {
      await ctx.resume();
    } catch {
      /* autoplay ili prekinuti uređaj */
    }
  }

  if (isAudioContextInterrupted(ctx.state)) {
    ctx = await recreateAudioContext();
    if (ctx.state === "suspended") {
      try {
        await ctx.resume();
      } catch {
        /* gesture još nije stigao */
      }
    }
  }

  return getAudioContext();
}
