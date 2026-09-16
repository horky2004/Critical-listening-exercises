export function ProgressBar({ value, max }: { value: number; max: number }) {
  const width = max === 0 ? 0 : Math.min(100, (value / max) * 100);
  return (
    <div
      className="h-1.5 overflow-hidden rounded-full bg-line/80"
      role="progressbar"
      aria-valuenow={value}
      aria-valuemax={max}
    >
      <div className="h-full rounded-full bg-accent shadow-[var(--glow-progress)] transition-[width] duration-300" style={{ width: `${width}%` }} />
    </div>
  );
}
