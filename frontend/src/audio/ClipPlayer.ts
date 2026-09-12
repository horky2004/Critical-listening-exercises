import { fetchAudio } from "./fetchAudio";
import { MASTER_GAIN, resumeAudioContext } from "./audioContext";

const FADE_IN = 0.01;
const FADE_OUT = 0.015;

export class ClipPlayer {
  private ctx: AudioContext | null = null;
  private buffer: AudioBuffer | null = null;
  private source: AudioBufferSourceNode | null = null;
  private master: GainNode | null = null;
  private listen: GainNode | null = null;
  private volume = 0.8;
  private playing = false;

  async load(url: string): Promise<void> {
    this.stop();
    const ctx = await resumeAudioContext();
    this.ctx = ctx;
    const bytes = await fetchAudio(url);
    this.buffer = await ctx.decodeAudioData(bytes.slice(0));
  }

  setVolume(volume: number): void {
    this.volume = Math.min(1, Math.max(0, volume));
    if (!this.ctx || !this.listen) {
      return;
    }
    const now = this.ctx.currentTime;
    this.listen.gain.setValueAtTime(this.listen.gain.value, now);
    this.listen.gain.linearRampToValueAtTime(this.volume, now + 0.02);
  }

  async play(): Promise<void> {
    if (!this.buffer) {
      throw new Error("Audio nije ucitan.");
    }
    const ctx = await resumeAudioContext();
    this.ctx = ctx;
    this.stop(false);
    const master = ctx.createGain();
    const listen = ctx.createGain();
    const source = ctx.createBufferSource();
    source.buffer = this.buffer;
    source.loop = true;
    source.connect(master);
    master.connect(listen);
    listen.connect(ctx.destination);
    const now = ctx.currentTime;
    master.gain.setValueAtTime(0, now);
    master.gain.linearRampToValueAtTime(MASTER_GAIN, now + FADE_IN);
    listen.gain.value = this.volume;
    source.start();
    this.source = source;
    this.master = master;
    this.listen = listen;
    this.playing = true;
  }

  stop(fade = true): void {
    const source = this.source;
    const master = this.master;
    const ctx = this.ctx;
    this.source = null;
    this.master = null;
    this.listen = null;
    this.playing = false;
    if (!source || !ctx) {
      return;
    }
    if (fade && master) {
      const now = ctx.currentTime;
      master.gain.setValueAtTime(master.gain.value, now);
      master.gain.linearRampToValueAtTime(0, now + FADE_OUT);
      source.stop(now + FADE_OUT);
    } else {
      try {
        source.stop();
      } catch {
        /* already stopped */
      }
    }
    source.disconnect();
  }

  dispose(): void {
    this.stop(false);
    this.buffer = null;
  }

  get isPlaying(): boolean {
    return this.playing;
  }
}
