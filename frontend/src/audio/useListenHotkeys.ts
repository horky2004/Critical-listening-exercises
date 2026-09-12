import { useEffect, useRef } from "react";

function isTypingTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) {
    return false;
  }
  return Boolean(target.closest("input, textarea, select, [contenteditable='true']"));
}

export function useListenHotkeys({
  enabled,
  onTogglePlay,
  onToggleCompare
}: {
  enabled: boolean;
  onTogglePlay: () => void;
  onToggleCompare?: () => void;
}): void {
  const play = useRef(onTogglePlay);
  const compare = useRef(onToggleCompare);
  play.current = onTogglePlay;
  compare.current = onToggleCompare;

  useEffect(() => {
    if (!enabled) {
      return;
    }

    function onKeyDown(event: KeyboardEvent) {
      if (event.repeat || isTypingTarget(event.target)) {
        return;
      }
      if (event.code === "Space") {
        event.preventDefault();
        play.current();
        return;
      }
      if (event.code === "Tab" && compare.current) {
        event.preventDefault();
        compare.current();
      }
    }

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [enabled]);
}
