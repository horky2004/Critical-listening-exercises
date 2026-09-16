import { forwardRef, type ButtonHTMLAttributes } from "react";

type Props = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "primary" | "ghost" | "danger";
};

export const Button = forwardRef<HTMLButtonElement, Props>(function Button(
  { variant = "primary", className = "", ...props },
  ref
) {
  const look =
    variant === "primary"
      ? "bg-accent text-on-accent shadow-[var(--glow-accent)] hover:bg-accent-dim"
      : variant === "danger"
        ? "border border-bad/35 bg-bad/10 text-bad hover:bg-bad/18"
        : "border border-line bg-transparent text-ink hover:border-accent/40 hover:bg-panel-2";

  return (
    <button
      ref={ref}
      className={`inline-flex min-h-11 items-center justify-center rounded-xl px-5 py-2.5 text-sm font-semibold tracking-tight transition duration-150 disabled:cursor-not-allowed disabled:opacity-40 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent ${look} ${className}`}
      {...props}
    />
  );
});
