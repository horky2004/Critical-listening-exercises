import type { LevelStatus } from "../api/types";
import { strings } from "../lib/strings";

const labels: Record<LevelStatus, string> = {
  Locked: strings.statusLocked,
  Unlocked: strings.statusUnlocked,
  InProgress: strings.statusInProgress,
  Completed: strings.statusCompleted
};

export function StatusIcon({ status, className = "" }: { status: LevelStatus; className?: string }) {
  return (
    <span className={`inline-flex items-center gap-2 ${className}`} title={labels[status]}>
      <svg viewBox="0 0 20 20" className="mt-0.5 h-5 w-5 shrink-0" aria-hidden="true">
        {status === "Locked" && (
          <rect x="3" y="3" width="14" height="14" rx="1" className="fill-none stroke-locked" strokeWidth="1.8" />
        )}
        {status === "Unlocked" && (
          <circle cx="10" cy="10" r="7" className="fill-none stroke-accent" strokeWidth="1.8" />
        )}
        {status === "InProgress" && (
          <polygon points="10,3 18,17 2,17" className="fill-none stroke-warn" strokeWidth="1.8" />
        )}
        {status === "Completed" && (
          <>
            <circle cx="10" cy="10" r="7" className="fill-good/15 stroke-good" strokeWidth="1.8" />
            <path d="M6 10.5 9 13.5 14 7" className="fill-none stroke-good" strokeWidth="1.8" />
          </>
        )}
      </svg>
      <span className="sr-only">{labels[status]}</span>
    </span>
  );
}

export function statusLabel(status: LevelStatus): string {
  return labels[status];
}
