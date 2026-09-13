import { useEffect, useRef } from "react";
import { CompressionAbPlayer } from "./CompressionAbPlayer";

export function useCompressionAb(): CompressionAbPlayer {
  const player = useRef<CompressionAbPlayer | null>(null);
  player.current ??= new CompressionAbPlayer();

  useEffect(() => {
    const current = player.current;
    return () => current?.dispose();
  }, []);

  return player.current;
}
