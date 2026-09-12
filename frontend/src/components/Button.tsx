import type { ButtonHTMLAttributes } from "react";

type Props = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "primary" | "ghost" | "danger";
};

export function Button({ variant = "primary", className = "", ...props }: Props) {
  const look =
    variant === "primary"
      ? "bg-accent text-white shadow-sm hover:bg-accent-dim"
      : variant === "danger"
        ? "border border-bad/30 bg-white text-bad hover:bg-red-50"
        : "border border-line bg-white text-ink hover:border-accent/40 hover:bg-panel-2";

  return (
    <button
      className={`inline-flex items-center justify-center rounded-xl px-5 py-2.5 text-sm font-semibold tracking-tight transition duration-150 disabled:cursor-not-allowed disabled:opacity-40 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent ${look} ${className}`}
      {...props}
    />
  );
}
