import { getAudioBuffer } from "./AudioBufferCache";
import { MASTER_GAIN, resumeAudioContext } from "./audioContext";

export type EqBand = {
  frequencyHz: number;
  gainDb: number;
  q: number;
};

const FADE_IN = 0.01;
const FADE_OUT = 0.015;
const CROSSFADE = 0.01;

export class EqPlaybackEngine {
  private ctx: AudioContext | null = null;
  private buffer: AudioBuffer | null = null;
  private source: AudioBufferSourceNode | null = null;
  private filter: BiquadFilterNode | null = null;
  private wet: GainNode | null = null;
  private dry: GainNode | null = null;
  private master: GainNode | null = null;
  private listen: GainNode | null = null;
  private bypass = false;
  private band: EqBand = { frequencyHz: 1000, gainDb: 0, q: 1 };
  private volume = 0.8;
  private playing = false;

  async load(url: string): Promise<void> {
    const ctx = await resumeAudioContext();
    this.ctx = ctx;
    this.buffer = await getAudioBuffer(url);
  }

  setBand(band: EqBand, options?: { smooth?: boolean }): void {
    this.band = band;
    if (!this.ctx || !this.filter || !this.playing) {
      return;
    }

    const now = this.ctx.currentTime;
    if (options?.smooth) {
      this.filter.frequency.setTargetAtTime(band.frequencyHz, now, 0.01);
      this.filter.gain.setTargetAtTime(band.gainDb, now, 0.01);
      this.filter.Q.setTargetAtTime(band.q, now, 0.01);
      return;
    }

    this.ramp(this.master, 0, now, FADE_OUT);
    this.applyBand(now + FADE_OUT);
    if (this.master) {
      this.master.gain.setValueAtTime(0, now + FADE_OUT);
      this.master.gain.linearRampToValueAtTime(MASTER_GAIN, now + FADE_OUT + FADE_IN);
    }
  }

  setVolume(volume: number): void {
    this.volume = Math.min(1, Math.max(0, volume));
    if (!this.ctx || !this.listen) {
      return;
    }
    this.ramp(this.listen, this.volume, this.ctx.currentTime, 0.02);
  }

  setBypass(bypass: boolean): void {
    this.bypass = bypass;
    if (!this.ctx || !this.wet || !this.dry) {
      return;
    }

    const now = this.ctx.currentTime;
    this.ramp(this.wet, bypass ? 0 : 1, now, CROSSFADE);
    this.ramp(this.dry, bypass ? 1 : 0, now, CROSSFADE);
  }

  async play(): Promise<void> {
    if (!this.buffer) {
      throw new Error("Audio nije ucitan.");
    }

    const ctx = await resumeAudioContext();
    this.ctx = ctx;
    this.stopSource(false);
    this.buildGraph(ctx);

    const source = ctx.createBufferSource();
    source.buffer = this.buffer;
    source.loop = true;
    source.connect(this.filter!);
    source.connect(this.dry!);
    this.source = source;
    this.applyBand(ctx.currentTime);
    this.setBypass(this.bypass);

    const now = ctx.currentTime;
    this.master!.gain.setValueAtTime(0, now);
    this.master!.gain.linearRampToValueAtTime(MASTER_GAIN, now + FADE_IN);
    source.start();
    this.playing = true;
  }

  stop(): void {
    this.stopSource(true);
  }

  dispose(): void {
    this.stopSource(false);
    this.buffer = null;
    this.ctx = null;
  }

  get isPlaying(): boolean {
    return this.playing;
  }

  private buildGraph(ctx: AudioContext): void {
    this.filter = ctx.createBiquadFilter();
    this.filter.type = "peaking";
    this.wet = ctx.createGain();
    this.dry = ctx.createGain();
    this.master = ctx.createGain();
    this.filter.connect(this.wet);
    this.wet.connect(this.master);
    this.dry.connect(this.master);
    this.listen = ctx.createGain();
    this.master.connect(this.listen);
    this.listen.connect(ctx.destination);
    this.wet.gain.value = this.bypass ? 0 : 1;
    this.dry.gain.value = this.bypass ? 1 : 0;
    this.master.gain.value = 0;
    this.listen.gain.value = this.volume;
  }

  private applyBand(at: number): void {
    if (!this.filter) {
      return;
    }
    this.filter.frequency.setValueAtTime(this.band.frequencyHz, at);
    this.filter.gain.setValueAtTime(this.band.gainDb, at);
    this.filter.Q.setValueAtTime(this.band.q, at);
  }

  private ramp(node: GainNode | null, target: number, now: number, duration: number): void {
    if (!node) {
      return;
    }
    node.gain.setValueAtTime(node.gain.value, now);
    node.gain.linearRampToValueAtTime(target, now + duration);
  }

  private stopSource(fade: boolean): void {
    const source = this.source;
    const master = this.master;
    const ctx = this.ctx;
    this.source = null;
    this.playing = false;
    if (!source || !ctx) {
      return;
    }

    source.onended = () => {
      try {
        source.disconnect();
      } catch {
        /* already disconnected */
      }
    };

    if (fade && master) {
      const now = ctx.currentTime;
      this.ramp(master, 0, now, FADE_OUT);
      source.stop(now + FADE_OUT);
      return;
    }

    try {
      source.stop();
    } catch {
      /* already stopped */
    }
  }
}
