import type { ReactNode } from "react";
import { strings } from "../lib/strings";
import { Button } from "./Button";

export function QueryState({
  isPending,
  error,
  onRetry,
  children
}: {
  isPending: boolean;
  error: Error | null;
  onRetry?: () => void;
  children: ReactNode;
}) {
  if (isPending) {
    return <p className="text-sm text-muted">{strings.loading}</p>;
  }

  if (error) {
    return (
      <div className="space-y-3">
        <p className="text-bad">{error.message || strings.error}</p>
        {onRetry && (
          <Button variant="ghost" onClick={onRetry}>
            {strings.retry}
          </Button>
        )}
      </div>
    );
  }

  return children;
}
