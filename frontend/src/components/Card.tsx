import type { ReactNode } from "react";

export function Card({
  children,
  className = ""
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={`rounded-2xl border border-line/80 bg-panel p-6 shadow-[var(--surface-shine)] ${className}`}
    >
      {children}
    </div>
  );
}
