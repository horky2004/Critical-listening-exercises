import { useEffect, useRef, type RefObject } from "react";

function isTypingTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) {
    return false;
  }
  return Boolean(target.closest("input, textarea, select, [contenteditable='true']"));
}

export function useListenHotkeys({
  enabled,
  onTogglePlay,
  onToggleCompare,
  advanceTarget
}: {
  enabled: boolean;
  onTogglePlay: () => void;
  onToggleCompare?: () => void;
  advanceTarget?: RefObject<HTMLElement | null>;
}): void {
  const play = useRef(onTogglePlay);
  const compare = useRef(onToggleCompare);
  const advance = useRef(advanceTarget);
  play.current = onTogglePlay;
  compare.current = onToggleCompare;
  advance.current = advanceTarget;

  useEffect(() => {
    if (!enabled) {
      return;
    }

    function onKeyDown(event: KeyboardEvent) {
      if (event.repeat || isTypingTarget(event.target) || event.metaKey || event.ctrlKey || event.altKey) {
        return;
      }
      if (event.code === "Space") {
        event.preventDefault();
        play.current();
        return;
      }
      if (event.code !== "Tab" || event.shiftKey) {
        return;
      }

      const continueButton = advance.current?.current;
      if (compare.current) {
        event.preventDefault();
        compare.current();
      }
      if (continueButton) {
        event.preventDefault();
        continueButton.focus();
      }
    }

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [enabled]);
}
