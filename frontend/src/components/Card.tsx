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
      className={`rounded-2xl border border-line bg-panel p-6 shadow-[0_10px_30px_rgba(21,32,51,0.06)] ${className}`}
    >
      {children}
    </div>
  );
}
