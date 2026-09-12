import { Link } from "react-router-dom";
import type { LevelStatus, TreeResponse } from "../../api/types";
import { StatusIcon, statusLabel } from "../../components/StatusIcon";
import { strings } from "../../lib/strings";

const colW = 230;
const rowH = 108;

const tone: Record<LevelStatus, string> = {
  Locked: "border-locked/40 text-locked",
  Unlocked: "border-accent/60 text-ink",
  InProgress: "border-warn/70 text-ink",
  Completed: "border-good/70 text-ink"
};

export function ProgressionTree({
  tree,
  moduleSlug,
  sourceSlug
}: {
  tree: TreeResponse;
  moduleSlug: string;
  sourceSlug: string;
}) {
  const positions = new Map<string, { x: number; y: number }>();
  tree.segments.forEach((segment, column) => {
    segment.levels.forEach((level, row) => {
      positions.set(level.levelId, { x: column * colW + 108, y: row * rowH + 56 });
    });
  });

  const width = Math.max(tree.segments.length * colW, colW);
  const height = Math.max(...tree.segments.map((s) => s.levels.length), 1) * rowH + 24;

  const edges = tree.segments.flatMap((segment) =>
    segment.levels.flatMap((level) =>
      level.requiredLevelIds
        .map((id) => ({ from: positions.get(id), to: positions.get(level.levelId) }))
        .filter((edge): edge is { from: { x: number; y: number }; to: { x: number; y: number } } =>
          Boolean(edge.from && edge.to)
        )
    )
  );

  return (
    <div className="overflow-x-auto">
      <div className="relative" style={{ width, height: height + 48 }}>
        <div className="mb-2 flex" style={{ width }}>
          {tree.segments.map((segment) => (
            <div key={segment.key} className="text-xs uppercase tracking-wider text-muted" style={{ width: colW }}>
              {segment.name}
            </div>
          ))}
        </div>
        <svg className="absolute top-8 left-0" width={width} height={height} aria-hidden="true">
          {edges.map((edge, i) => (
            <path
              key={i}
              d={`M ${edge.from.x} ${edge.from.y} C ${edge.from.x} ${edge.from.y + 36}, ${edge.to.x} ${edge.to.y - 36}, ${edge.to.x} ${edge.to.y}`}
              className="fill-none stroke-line"
              strokeWidth="1.5"
            />
          ))}
        </svg>
        {tree.segments.map((segment, column) =>
          segment.levels.map((level, row) => {
            const canOpen = level.status !== "Locked";
            const action =
              level.status === "InProgress"
                ? strings.continueTest
                : level.status === "Completed"
                  ? strings.retryTest
                  : strings.startTest;
            const card = (
              <div
                className={`w-[200px] rounded-lg border bg-panel-2 p-3 ${tone[level.status]} ${canOpen ? "hover:bg-panel" : ""}`}
              >
                <div className="flex items-start gap-2">
                  <StatusIcon status={level.status} />
                  <div>
                    <p className="text-sm font-medium">{level.title}</p>
                    <p className="text-xs text-muted">{statusLabel(level.status)}</p>
                  </div>
                </div>
                <p className="tabular mt-2 text-xs text-muted">
                  {strings.best} {level.bestScore}/{level.questionCount} · {strings.passFrom} {level.passThreshold}
                  {level.attemptCount > 0 ? ` · ${level.attemptCount} ${strings.attempts}` : ""}
                </p>
                {canOpen && <p className="mt-2 text-xs text-accent">{action}</p>}
              </div>
            );

            return (
              <div
                key={level.levelId}
                className="absolute"
                style={{ left: column * colW + 8, top: row * rowH + 40 }}
              >
                {canOpen ? (
                  <Link to={`/modules/${moduleSlug}/sources/${sourceSlug}/test/${level.levelId}`}>{card}</Link>
                ) : (
                  <div aria-disabled="true">{card}</div>
                )}
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}
