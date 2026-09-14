import { describe, expect, it } from "vitest";
import { strings } from "../lib/strings";
import { AudioLoadError, audioFailureMessage } from "./audioErrors";

describe("audioFailureMessage", () => {
  it("maps typed load failures to Croatian copy", () => {
    expect(audioFailureMessage(new AudioLoadError("decode"))).toBe(strings.audioDecodeFailed);
    expect(audioFailureMessage(new AudioLoadError("network"))).toBe(strings.audioNetworkFailed);
    expect(audioFailureMessage(new AudioLoadError("unauthorized"))).toBe(strings.audioAuthFailed);
    expect(audioFailureMessage(new AudioLoadError("mismatch"))).toBe(strings.audioVariantMismatch);
  });

  it("maps HTTP 401 to an expired-session message", () => {
    expect(audioFailureMessage({ status: 401 })).toBe(strings.audioAuthFailed);
  });
});
