import { strings } from "../lib/strings";

export type AudioFailureKind = "network" | "unauthorized" | "decode" | "mismatch";

export class AudioLoadError extends Error {
  readonly kind: AudioFailureKind;

  constructor(kind: AudioFailureKind, message?: string) {
    super(message ?? kind);
    this.name = "AudioLoadError";
    this.kind = kind;
  }
}

function statusOf(cause: unknown): number | null {
  if (cause && typeof cause === "object" && "status" in cause && typeof cause.status === "number") {
    return cause.status;
  }
  return null;
}

export function audioFailureMessage(cause: unknown): string {
  if (cause instanceof AudioLoadError) {
    switch (cause.kind) {
      case "unauthorized":
        return strings.audioAuthFailed;
      case "network":
        return strings.audioNetworkFailed;
      case "mismatch":
        return strings.audioVariantMismatch;
      default:
        return strings.audioDecodeFailed;
    }
  }

  const status = statusOf(cause);
  if (status === 401) {
    return strings.audioAuthFailed;
  }
  if (status != null) {
    return strings.audioNetworkFailed;
  }

  if (cause instanceof Error && cause.message === "VARIANT_LENGTH_MISMATCH") {
    return strings.audioVariantMismatch;
  }

  return strings.audioDecodeFailed;
}
