export function ProgressBar({ value, max }: { value: number; max: number }) {
  const width = max === 0 ? 0 : Math.min(100, (value / max) * 100);
  return (
    <div
      className="h-1.5 overflow-hidden rounded-full bg-line/80"
      role="progressbar"
      aria-valuenow={value}
      aria-valuemax={max}
    >
      <div className="h-full rounded-full bg-accent shadow-[0_0_12px_rgba(62,224,198,0.45)] transition-[width] duration-300" style={{ width: `${width}%` }} />
    </div>
  );
}
