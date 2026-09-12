import { useEffect, useRef } from "react";
import { EqPlaybackEngine } from "./EqPlaybackEngine";

export function useEqEngine(): EqPlaybackEngine {
  const engine = useRef<EqPlaybackEngine | null>(null);
  engine.current ??= new EqPlaybackEngine();

  useEffect(() => {
    const player = engine.current;
    return () => player?.dispose();
  }, []);

  return engine.current;
}
