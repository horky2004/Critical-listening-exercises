import { useEffect, useState } from "react";
import { isAudioContextInterrupted, subscribeAudioContextState } from "./audioContext";

export function useAudioRuntime() {
  const [state, setState] = useState("suspended");

  useEffect(() => subscribeAudioContextState(setState), []);

  return {
    state,
    running: state === "running",
    suspended: state === "suspended",
    interrupted: isAudioContextInterrupted(state)
  };
}
