import { logFrequencyPoints, peakingMagnitudeDb } from "../../audio/peakingResponse";
import { formatGain, formatHz } from "../../lib/format";
import { strings } from "../../lib/strings";

const MIN_HZ = 20;
const MAX_HZ = 20000;
const MIN_DB = -15;
const MAX_DB = 15;
const WIDTH = 800;
const HEIGHT = 280;
const LEFT = 46;
const RIGHT = 18;
const TOP = 18;
const BOTTOM = 32;
const FREQ_TICKS = [20, 50, 100, 200, 500, 1000, 2000, 5000, 10000, 20000];
const DB_TICKS = [12, 6, 0, -6, -12];

type Band = { frequencyHz: number; gainDb: number; q: number };

export function EqMoveGraph({ band }: { band: Band }) {
  const plotW = WIDTH - LEFT - RIGHT;
  const plotH = HEIGHT - TOP - BOTTOM;
  const points = logFrequencyPoints(MIN_HZ, MAX_HZ, 240).map((hz) => {
    const db = clampDb(peakingMagnitudeDb(hz, band));
    return `${xOf(hz, plotW).toFixed(2)},${yOf(db, plotH).toFixed(2)}`;
  });
  const line = `M ${points.join(" L ")}`;
  const zeroY = yOf(0, plotH);
  const fill = `M ${xOf(MIN_HZ, plotW)} ${zeroY} L ${points.join(" L ")} L ${xOf(MAX_HZ, plotW)} ${zeroY} Z`;
  const nodeX = xOf(band.frequencyHz, plotW);
  const nodeY = yOf(clampDb(band.gainDb), plotH);
  const label = `${formatHz(band.frequencyHz)} · ${formatGain(band.gainDb)}`;

  return (
    <figure className="overflow-hidden rounded-2xl border border-[#2a2a2a] bg-[#1b1b1b] shadow-[0_12px_28px_rgba(21,32,51,0.18)]">
      <figcaption className="flex items-center justify-between px-4 pt-3 text-sm">
        <span className="font-semibold text-[#e6d25a]">{strings.eqMove}</span>
        <span className="tabular font-medium text-[#d8d8d8]">{label}</span>
      </figcaption>
      <svg
        viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
        role="img"
        aria-label={`${strings.eqMove}: ${label}`}
        className="block h-auto w-full"
      >
        <rect x={LEFT} y={TOP} width={plotW} height={plotH} fill="#141414" />
        {FREQ_TICKS.map((hz) => (
          <line
            key={`f-${hz}`}
            x1={xOf(hz, plotW)}
            x2={xOf(hz, plotW)}
            y1={TOP}
            y2={TOP + plotH}
            stroke={hz === 1000 ? "#4a4a4a" : "#2e2e2e"}
            strokeWidth="1"
          />
        ))}
        {DB_TICKS.map((db) => (
          <line
            key={`db-${db}`}
            x1={LEFT}
            x2={LEFT + plotW}
            y1={yOf(db, plotH)}
            y2={yOf(db, plotH)}
            stroke={db === 0 ? "#5c5c5c" : "#2e2e2e"}
            strokeWidth={db === 0 ? 1.4 : 1}
          />
        ))}
        <path d={fill} fill="#e6d25a" fillOpacity="0.16" />
        <path d={line} fill="none" stroke="#e6d25a" strokeWidth="2.4" strokeLinejoin="round" />
        <circle cx={nodeX} cy={nodeY} r="7" fill="#1b1b1b" stroke="#f3e27a" strokeWidth="2.2" />
        <circle cx={nodeX} cy={nodeY} r="2.6" fill="#f3e27a" />
        {FREQ_TICKS.filter((hz) => hz !== 20 && hz !== 20000).map((hz) => (
          <text
            key={`fl-${hz}`}
            x={xOf(hz, plotW)}
            y={HEIGHT - 10}
            textAnchor="middle"
            fill="#9a9a9a"
            fontSize="11"
          >
            {tickHz(hz)}
          </text>
        ))}
        {DB_TICKS.map((db) => (
          <text
            key={`dl-${db}`}
            x={LEFT - 8}
            y={yOf(db, plotH) + 4}
            textAnchor="end"
            fill="#9a9a9a"
            fontSize="11"
          >
            {db > 0 ? `+${db}` : db}
          </text>
        ))}
      </svg>
    </figure>
  );
}

function xOf(hz: number, plotW: number): number {
  const clamped = Math.min(MAX_HZ, Math.max(MIN_HZ, hz));
  const t = (Math.log10(clamped) - Math.log10(MIN_HZ)) / (Math.log10(MAX_HZ) - Math.log10(MIN_HZ));
  return LEFT + t * plotW;
}

function yOf(db: number, plotH: number): number {
  const t = (MAX_DB - db) / (MAX_DB - MIN_DB);
  return TOP + t * plotH;
}

function clampDb(db: number): number {
  return Math.min(MAX_DB, Math.max(MIN_DB, db));
}

function tickHz(hz: number): string {
  return hz >= 1000 ? `${hz / 1000}k` : String(hz);
}
