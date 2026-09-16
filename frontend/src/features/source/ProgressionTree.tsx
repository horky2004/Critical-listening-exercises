import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import type { LevelStatus, TreeResponse } from "../../api/types";
import { ScoreDot } from "../../components/ScoreDot";
import { StatusIcon, statusLabel } from "../../components/StatusIcon";
import { strings } from "../../lib/strings";

const rowH = 148;
const padX = 12;

const tone: Record<LevelStatus, string> = {
  Locked: "border-dashed border-line/100 bg-panel-2/100 text-muted shadow-none",
  Unlocked: "border-accent/55 bg-panel text-ink shadow-[inset_0_1px_0_rgba(255,255,255,0.04)] hover:-translate-y-0.5 hover:border-accent/50 hover:shadow-[0_16px_34px_rgba(62,224,198,0.12)]",
  InProgress: "border-warn/55 bg-panel text-ink shadow-[inset_0_1px_0_rgba(255,255,255,0.04)] hover:-translate-y-0.5",
  Completed: "border-good/90 bg-panel text-ink shadow-[inset_0_1px_0_rgba(255,255,255,0.04)] hover:-translate-y-0.5"
};

const badge: Record<LevelStatus, string> = {
  Locked: "bg-line/45 text-muted",
  Unlocked: "bg-accent/15 text-accent",
  InProgress: "bg-warn/15 text-warn",
  Completed: "bg-good/15 text-good"
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
  const box = useRef<HTMLDivElement>(null);
  const [width, setWidth] = useState(1200);

  useEffect(() => {
    const el = box.current;
    if (!el) {
      return;
    }
    const sync = () => setWidth(el.clientWidth);
    sync();
    const observer = new ResizeObserver(sync);
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  const columns = Math.max(tree.segments.length, 1);
  const colW = width / columns;
  const cardW = Math.max(colW - padX * 2, 200);

  const positions = new Map<string, { x: number; y: number }>();
  tree.segments.forEach((segment, column) => {
    segment.levels.forEach((level, row) => {
      positions.set(level.levelId, {
        x: column * colW + colW / 2,
        y: row * rowH + 78
      });
    });
  });

  const height = Math.max(...tree.segments.map((s) => s.levels.length), 1) * rowH + 24;

  const edges = tree.segments.flatMap((segment) =>
    segment.levels.flatMap((level) =>
      level.requiredLevelIds
        .map((id) => ({
          from: positions.get(id),
          to: positions.get(level.levelId),
          open: level.status !== "Locked"
        }))
        .filter(
          (edge): edge is { from: { x: number; y: number }; to: { x: number; y: number }; open: boolean } =>
            Boolean(edge.from && edge.to)
        )
    )
  );

  return (
    <div ref={box} className="w-full overflow-x-auto">
      <div className="relative min-w-full" style={{ height: height + 56 }}>
        <div className="mb-3 grid" style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }}>
          {tree.segments.map((segment) => (
            <div key={segment.key} className="px-3 text-xs font-semibold uppercase tracking-[0.16em] text-muted text-center">
              {segment.name}
            </div>
          ))}
        </div>
        <svg className="absolute top-8 left-0" width={width} height={height} aria-hidden="true">
          {edges.map((edge, i) => (
            <path
              key={i}
              d={`M ${edge.from.x} ${edge.from.y} C ${edge.from.x} ${edge.from.y + 40}, ${edge.to.x} ${edge.to.y - 40}, ${edge.to.x} ${edge.to.y}`}
              className={`fill-none ${edge.open ? "stroke-good/55" : "stroke-line"}`}
              strokeWidth="2"
            />
          ))}
        </svg>
        {tree.segments.map((segment, column) =>
          segment.levels.map((level, row) => {
            const canOpen = level.status !== "Locked";
            const action = strings.openLevel;
            const card = (
              <div
                className={`relative rounded-2xl border-4 p-4 transition duration-150 ${tone[level.status]} ${canOpen ? "" : "opacity-90"}`}
                style={{ width: cardW }}
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="flex min-w-0 items-start gap-2.5">
                    <StatusIcon status={level.status} />
                    <div className="min-w-0">
                      <p className="text-base font-semibold tracking-tight">{level.title}</p>
                      <span
                        className={`mt-1 inline-flex rounded-full px-2 py-0.5 text-[11px] font-medium ${badge[level.status]}`}
                      >
                        {statusLabel(level.status)}
                      </span>
                    </div>
                  </div>
                  <div className="shrink-0 text-right">
                    <div className="flex items-center justify-end gap-2">
                      <p
                        className="tabular text-lg font-semibold tracking-tight text-ink"
                        aria-label={`${strings.bestAttempt} ${level.bestScore}/${level.questionCount}`}
                      >
                        {level.bestScore}/{level.questionCount}
                      </p>
                      <ScoreDot
                        score={level.bestScore}
                        total={level.questionCount}
                        threshold={level.passThreshold}
                      />
                    </div>
                    <p className="tabular mt-0.5 text-[10px] text-muted/80">
                      {strings.attempts}: {level.attemptCount}
                    </p>
                  </div>
                </div>
                {canOpen && (
                  <p
                    aria-hidden
                    className="pointer-events-none absolute inset-x-0 bottom-3 text-center text-sm font-semibold text-accent opacity-0 transition-opacity duration-150 group-hover:opacity-100 group-focus-visible:opacity-100"
                  >
                    {action}
                  </p>
                )}
              </div>
            );

            return (
              <div
                key={level.levelId}
                className="absolute"
                style={{ left: column * colW + padX, top: row * rowH + 44 }}
              >
                {canOpen ? (
                  <Link
                    to={`/modules/${moduleSlug}/sources/${sourceSlug}/levels/${level.levelId}`}
                    className="group block rounded-2xl focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
                  >
                    {card}
                  </Link>
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
