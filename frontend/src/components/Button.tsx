import type { ButtonHTMLAttributes } from "react";

type Props = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "primary" | "ghost" | "danger";
};

export function Button({ variant = "primary", className = "", ...props }: Props) {
  const look =
    variant === "primary"
      ? "bg-accent text-bg hover:brightness-110"
      : variant === "danger"
        ? "border border-bad/50 text-bad hover:bg-bad/10"
        : "border border-line text-ink hover:border-accent/50";

  return (
    <button
      className={`inline-flex items-center justify-center rounded-md px-4 py-2 text-sm font-medium disabled:opacity-40 ${look} ${className}`}
      {...props}
    />
  );
}
