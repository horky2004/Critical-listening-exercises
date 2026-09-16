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
      className={`rounded-2xl bg-panel p-6 shadow-[var(--card-shadow)] ${className}`}
    >
      {children}
    </div>
  );
}
