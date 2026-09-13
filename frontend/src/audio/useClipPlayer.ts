import { useEffect, useRef } from "react";
import { ClipPlayer } from "./ClipPlayer";

export function useClipPlayer(): ClipPlayer {
  const player = useRef<ClipPlayer | null>(null);
  player.current ??= new ClipPlayer();

  useEffect(() => {
    const current = player.current;
    return () => current?.dispose();
  }, []);

  return player.current;
}
