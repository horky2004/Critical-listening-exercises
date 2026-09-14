export function ScoreDot({
  score,
  total,
  threshold,
  className = "size-6"
}: {
  score: number;
  total: number;
  threshold: number;
  className?: string;
}) {
  const ratio = total <= 0 ? 0 : Math.min(Math.max(score / total, 0), 1);
  const perfect = total > 0 && score >= total;
  const belowThreshold = score < threshold;
  const fill = perfect || !belowThreshold ? "var(--color-good)" : "var(--color-bad)";

  return (
    <span
      className={`inline-block shrink-0 rounded-full border border-line ${className}`}
      style={{
        background: perfect ? fill : `conic-gradient(${fill} ${ratio * 360}deg, var(--color-panel-2) 0deg)`
      }}
      title={`${score}/${total}`}
      aria-label={`${score}/${total}`}
    />
  );
}
