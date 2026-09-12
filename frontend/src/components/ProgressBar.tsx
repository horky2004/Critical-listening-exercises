export function ProgressBar({ value, max }: { value: number; max: number }) {
  const width = max === 0 ? 0 : Math.min(100, (value / max) * 100);
  return (
    <div className="h-1.5 overflow-hidden rounded-full bg-line" role="progressbar" aria-valuenow={value} aria-valuemax={max}>
      <div className="h-full bg-accent" style={{ width: `${width}%` }} />
    </div>
  );
}
