import { strings } from "../../lib/strings";

export function EqListenBar({
  playing,
  disabled,
  error,
  volume,
  onPlay,
  onStop,
  onVolumeChange
}: {
  playing: boolean;
  disabled?: boolean;
  error: string | null;
  volume: number;
  onPlay: () => void;
  onStop: () => void;
  onVolumeChange: (volume: number) => void;
}) {
  const percent = Math.round(volume * 100);

  return (
    <div className="rounded-2xl border border-line bg-panel p-5 shadow-[0_10px_30px_rgba(21,32,51,0.04)]">
      <div className="flex flex-wrap items-center gap-4">
        <button
          type="button"
          disabled={disabled}
          aria-label={playing ? strings.stop : strings.play}
          title={playing ? strings.stop : strings.play}
          onClick={() => (playing ? onStop() : onPlay())}
          className="inline-flex size-12 shrink-0 items-center justify-center rounded-full bg-accent text-white shadow-sm transition duration-150 hover:bg-accent-dim disabled:cursor-not-allowed disabled:opacity-40 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
        >
          {playing ? <StopIcon /> : <PlayIcon />}
        </button>
        <label className="flex min-w-[180px] flex-1 items-center gap-3">
          <SpeakerIcon muted={volume === 0} />
          <input
            type="range"
            min={0}
            max={100}
            step={1}
            value={percent}
            disabled={disabled}
            aria-label={strings.volume}
            className="h-2 w-full cursor-pointer appearance-none rounded-full bg-line accent-accent disabled:cursor-not-allowed disabled:opacity-40"
            onChange={(event) => onVolumeChange(Number(event.target.value) / 100)}
          />
          <span className="tabular w-10 text-right text-sm font-medium text-muted">{percent}%</span>
        </label>
      </div>
      {!playing && <p className="mt-3 text-sm text-muted">{strings.clickToHear}</p>}
      {error && <p className="mt-2 text-sm text-warn">{error}</p>}
    </div>
  );
}

function PlayIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className="ml-0.5 size-6 fill-current">
      <path d="M8 5.14v13.72a1 1 0 0 0 1.54.84l10.12-6.86a1 1 0 0 0 0-1.68L9.54 4.3A1 1 0 0 0 8 5.14Z" />
    </svg>
  );
}

function StopIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className="size-5 fill-current">
      <rect x="6.5" y="6.5" width="11" height="11" rx="1.5" />
    </svg>
  );
}

function SpeakerIcon({ muted }: { muted: boolean }) {
  return (
    <svg
      viewBox="0 0 24 24"
      aria-hidden="true"
      className={`size-5 shrink-0 ${muted ? "text-locked" : "text-muted"}`}
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinejoin="round"
    >
      <path d="M4.5 9.5h2.8L11 6.2v11.6L7.3 14.5H4.5a1 1 0 0 1-1-1v-3a1 1 0 0 1 1-1Z" />
      {!muted && (
        <>
          <path d="M15 9.2a3.4 3.4 0 0 1 0 5.6" />
          <path d="M17.4 7a6 6 0 0 1 0 10" />
        </>
      )}
    </svg>
  );
}
