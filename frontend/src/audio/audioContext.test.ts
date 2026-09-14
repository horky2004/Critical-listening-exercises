import { describe, expect, it } from "vitest";
import { MASTER_GAIN, MASTER_GAIN_DB, isAudioContextInterrupted } from "./audioContext";

describe("master gain", () => {
  it("keeps configured headroom and stays below unity", () => {
    expect(MASTER_GAIN_DB).toBe(-12);
    expect(MASTER_GAIN).toBeCloseTo(10 ** (MASTER_GAIN_DB / 20));
    expect(MASTER_GAIN).toBeLessThan(1);
    expect(MASTER_GAIN).toBeGreaterThan(0);
  });
});

describe("audio context interruption", () => {
  it("detects a broken or closed output device", () => {
    expect(isAudioContextInterrupted("interrupted")).toBe(true);
    expect(isAudioContextInterrupted("closed")).toBe(true);
    expect(isAudioContextInterrupted("suspended")).toBe(false);
    expect(isAudioContextInterrupted("running")).toBe(false);
  });
});
