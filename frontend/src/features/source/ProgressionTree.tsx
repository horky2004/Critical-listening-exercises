import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import type { LevelStatus, TreeResponse } from "../../api/types";
import { StatusIcon, statusLabel } from "../../components/StatusIcon";
import { strings } from "../../lib/strings";

const rowH = 188;
const padX = 12;

const tone: Record<LevelStatus, string> = {
  Locked: "border-line bg-white/80 text-ink/70 shadow-none",
  Unlocked: "border-accent/25 bg-panel text-ink shadow-[0_10px_28px_rgba(21,32,51,0.07)] hover:-translate-y-0.5 hover:border-accent/50 hover:shadow-[0_16px_34px_rgba(15,118,110,0.12)]",
  InProgress: "border-warn/35 bg-panel text-ink shadow-[0_10px_28px_rgba(21,32,51,0.07)] hover:-translate-y-0.5",
  Completed: "border-good/30 bg-panel text-ink shadow-[0_10px_28px_rgba(21,32,51,0.07)] hover:-translate-y-0.5"
};

const badge: Record<LevelStatus, string> = {
  Locked: "bg-line text-muted",
  Unlocked: "bg-accent/10 text-accent",
  InProgress: "bg-orange-50 text-warn",
  Completed: "bg-emerald-50 text-good"
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
        .map((id) => ({ from: positions.get(id), to: positions.get(level.levelId) }))
        .filter((edge): edge is { from: { x: number; y: number }; to: { x: number; y: number } } =>
          Boolean(edge.from && edge.to)
        )
    )
  );

  return (
    <div ref={box} className="w-full overflow-x-auto">
      <div className="relative min-w-full" style={{ height: height + 56 }}>
        <div className="mb-3 grid" style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }}>
          {tree.segments.map((segment) => (
            <div key={segment.key} className="px-3 text-xs font-semibold uppercase tracking-[0.16em] text-muted">
              {segment.name}
            </div>
          ))}
        </div>
        <svg className="absolute top-8 left-0" width={width} height={height} aria-hidden="true">
          {edges.map((edge, i) => (
            <path
              key={i}
              d={`M ${edge.from.x} ${edge.from.y} C ${edge.from.x} ${edge.from.y + 40}, ${edge.to.x} ${edge.to.y - 40}, ${edge.to.x} ${edge.to.y}`}
              className="fill-none stroke-[#c5ced8]"
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
                className={`h-full rounded-2xl border p-4 transition duration-150 ${tone[level.status]} ${canOpen ? "" : "opacity-70"}`}
                style={{ width: cardW }}
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="flex items-start gap-2.5">
                    <StatusIcon status={level.status} />
                    <div>
                      <p className="text-base font-semibold tracking-tight">{level.title}</p>
                      <span
                        className={`mt-1 inline-flex rounded-full px-2 py-0.5 text-[11px] font-medium ${badge[level.status]}`}
                      >
                        {statusLabel(level.status)}
                      </span>
                    </div>
                  </div>
                </div>
                <div className="mt-4 grid grid-cols-2 gap-2 text-xs text-muted">
                  <p className="tabular rounded-lg bg-panel-2 px-2.5 py-2">
                    <span className="block text-[11px]">{strings.bestAttempt}</span>
                    <span className="mt-0.5 block font-semibold text-ink">
                      {level.bestScore}/{level.questionCount}
                    </span>
                  </p>
                  <p className="tabular rounded-lg bg-panel-2 px-2.5 py-2">
                    <span className="block text-[11px]">{strings.attempts}</span>
                    <span className="mt-0.5 block font-semibold text-ink">
                      {level.attemptCount} · {strings.passFrom} {level.passThreshold} / {level.questionCount}
                    </span>
                  </p>
                </div>
                {canOpen && <p className="mt-3 text-sm font-semibold text-accent">{action}</p>}
              </div>
            );

            return (
              <div
                key={level.levelId}
                className="absolute"
                style={{ left: column * colW + padX, top: row * rowH + 44 }}
              >
                {canOpen ? (
                  <Link to={`/modules/${moduleSlug}/sources/${sourceSlug}/levels/${level.levelId}`}>{card}</Link>
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
