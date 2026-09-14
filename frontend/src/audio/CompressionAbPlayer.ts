import { getAudioBuffer } from "./AudioBufferCache";
import { AudioLoadError } from "./audioErrors";
import { getAudioContextGeneration, MASTER_GAIN, resumeAudioContext } from "./audioContext";

const FADE_IN = 0.01;
const FADE_OUT = 0.015;
const CROSSFADE = 0.01;

export type CompressionVariantRef = {
  slug: string;
  url: string;
};

export class CompressionAbPlayer {
  private ctx: AudioContext | null = null;
  private buffers = new Map<string, AudioBuffer>();
  private sources = new Map<string, AudioBufferSourceNode>();
  private gains = new Map<string, GainNode>();
  private master: GainNode | null = null;
  private listen: GainNode | null = null;
  private slugs: string[] = [];
  private active: string | null = null;
  private volume = 0.8;
  private playing = false;
  private lastVariants: CompressionVariantRef[] = [];
  private contextGeneration = 0;

  async load(variants: CompressionVariantRef[]): Promise<void> {
    this.stop(false);
    this.buffers.clear();
    this.lastVariants = variants;
    this.slugs = variants.map((variant) => variant.slug);
    this.active = this.slugs[0] ?? null;

    const ctx = await resumeAudioContext();
    this.ctx = ctx;
    this.contextGeneration = getAudioContextGeneration();

    const decoded = await Promise.all(
      variants.map(async (variant) => ({
        slug: variant.slug,
        buffer: await getAudioBuffer(variant.url)
      }))
    );

    const length = decoded[0]?.buffer.length;
    const sampleRate = decoded[0]?.buffer.sampleRate;
    if (
      length == null
      || decoded.some((item) => item.buffer.length !== length || item.buffer.sampleRate !== sampleRate)
    ) {
      throw new AudioLoadError("mismatch", "VARIANT_LENGTH_MISMATCH");
    }

    for (const item of decoded) {
      this.buffers.set(item.slug, item.buffer);
    }
  }

  setActive(slug: string): void {
    if (!this.slugs.includes(slug) || this.active === slug) {
      this.active = slug;
      return;
    }

    const previous = this.active;
    this.active = slug;
    if (!this.ctx || !this.playing) {
      return;
    }

    const now = this.ctx.currentTime;
    this.ramp(this.gains.get(previous ?? ""), 0, now, CROSSFADE);
    this.ramp(this.gains.get(slug), 1, now, CROSSFADE);
  }

  setVolume(volume: number): void {
    this.volume = Math.min(1, Math.max(0, volume));
    if (!this.ctx || !this.listen) {
      return;
    }
    this.ramp(this.listen, this.volume, this.ctx.currentTime, 0.02);
  }

  async play(): Promise<void> {
    if (this.lastVariants.length > 0 && this.contextGeneration !== getAudioContextGeneration()) {
      const active = this.active;
      await this.load(this.lastVariants);
      if (active) {
        this.setActive(active);
      }
    }
    if (this.buffers.size === 0) {
      throw new Error("Audio nije ucitan.");
    }

    const ctx = await resumeAudioContext();
    this.ctx = ctx;
    this.stop(false);
    this.buildGraph(ctx);

    const now = ctx.currentTime;
    for (const slug of this.slugs) {
      const buffer = this.buffers.get(slug);
      const gain = this.gains.get(slug);
      if (!buffer || !gain) {
        continue;
      }
      const source = ctx.createBufferSource();
      source.buffer = buffer;
      source.loop = true;
      source.connect(gain);
      source.start(now);
      this.sources.set(slug, source);
    }

    this.master!.gain.setValueAtTime(0, now);
    this.master!.gain.linearRampToValueAtTime(MASTER_GAIN, now + FADE_IN);
    this.playing = true;
  }

  stop(fade = true): void {
    const sources = [...this.sources.values()];
    const master = this.master;
    const ctx = this.ctx;
    this.sources.clear();
    this.gains.clear();
    this.master = null;
    this.listen = null;
    this.playing = false;
    if (sources.length === 0 || !ctx) {
      return;
    }

    const disconnect = () => {
      for (const source of sources) {
        try {
          source.disconnect();
        } catch {
          /* already disconnected */
        }
      }
    };

    if (fade && master) {
      const now = ctx.currentTime;
      this.ramp(master, 0, now, FADE_OUT);
      for (const source of sources) {
        source.onended = disconnect;
        source.stop(now + FADE_OUT);
      }
      return;
    }

    for (const source of sources) {
      try {
        source.stop();
      } catch {
        /* already stopped */
      }
    }
    disconnect();
  }

  dispose(): void {
    this.stop(false);
    this.buffers.clear();
    this.slugs = [];
    this.active = null;
    this.ctx = null;
  }

  get isPlaying(): boolean {
    return this.playing;
  }

  get activeSlug(): string | null {
    return this.active;
  }

  private buildGraph(ctx: AudioContext): void {
    this.master = ctx.createGain();
    this.listen = ctx.createGain();
    this.master.connect(this.listen);
    this.listen.connect(ctx.destination);
    this.master.gain.value = 0;
    this.listen.gain.value = this.volume;

    for (const slug of this.slugs) {
      const gain = ctx.createGain();
      gain.gain.value = slug === this.active ? 1 : 0;
      gain.connect(this.master);
      this.gains.set(slug, gain);
    }
  }

  private ramp(node: GainNode | undefined | null, target: number, now: number, duration: number): void {
    if (!node) {
      return;
    }
    node.gain.setValueAtTime(node.gain.value, now);
    node.gain.linearRampToValueAtTime(target, now + duration);
  }
}
