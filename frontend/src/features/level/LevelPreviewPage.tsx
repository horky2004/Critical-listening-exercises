import { useEffect, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ClipPlayer } from "../../audio/ClipPlayer";
import { EqPlaybackEngine } from "../../audio/EqPlaybackEngine";
import { ApiError } from "../../api/client";
import { usePreview } from "../../api/hooks";
import type { PreviewResponse } from "../../api/types";
import { Button } from "../../components/Button";
import { QueryState } from "../../components/QueryState";
import { Shell } from "../../components/Shell";
import { formatGain, formatHz } from "../../lib/format";
import { strings } from "../../lib/strings";

function previewError(error: Error | null): Error | null {
  if (!error) {
    return null;
  }
  if (error instanceof ApiError && error.problem?.type?.endsWith("level-locked")) {
    return new Error(strings.lockedLevel);
  }
  return error;
}

export function LevelPreviewPage() {
  const { moduleSlug = "", sourceSlug = "", levelId = "" } = useParams();
  const preview = usePreview(moduleSlug, sourceSlug, levelId);
  const navigate = useNavigate();

  return (
    <Shell>
      <Link
        to={`/modules/${moduleSlug}/sources/${sourceSlug}`}
        className="text-sm font-medium text-muted transition hover:text-accent"
      >
        ← {strings.backToTree}
      </Link>
      <QueryState
        isPending={preview.isPending}
        error={previewError(preview.error)}
        onRetry={() => void preview.refetch()}
      >
        {preview.data && (
          <PreviewBody
            preview={preview.data}
            onStartTest={() =>
              navigate(`/modules/${moduleSlug}/sources/${sourceSlug}/test/${preview.data.level.levelId}`)
            }
          />
        )}
      </QueryState>
    </Shell>
  );
}

function PreviewBody({
  preview,
  onStartTest
}: {
  preview: PreviewResponse;
  onStartTest: () => void;
}) {
  return (
    <div className="mx-auto mt-8 max-w-4xl space-y-8">
      <div>
        <p className="text-sm font-medium text-accent">
          {preview.module.name} · {preview.source.name}
        </p>
        <h1 className="mt-1 text-3xl font-semibold tracking-tight">{preview.level.title}</h1>
        <p className="mt-3 max-w-2xl text-sm leading-6 text-muted">
          {preview.mode === "eqBand" ? strings.previewHint : strings.previewHintCompression}
        </p>
      </div>
      {preview.mode === "eqBand" ? <EqPreview preview={preview} /> : <CompressionPreview preview={preview} />}
      <Button className="w-full sm:w-auto" onClick={onStartTest}>
        {strings.startTest}
      </Button>
    </div>
  );
}

function EqPreview({ preview }: { preview: PreviewResponse }) {
  const engine = useRef(new EqPlaybackEngine());
  const [frequency, setFrequency] = useState<number | null>(null);
  const [gain, setGain] = useState(preview.gainsDb?.[0] ?? 0);
  const [flat, setFlat] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const q = preview.q ?? 1;

  useEffect(() => {
    const player = engine.current;
    const url = preview.audio?.url;
    if (!url) {
      return;
    }
    void player.load(url).catch(() => setError(strings.audioDecodeFailed));
    return () => player.dispose();
  }, [preview.audio?.url]);

  async function ensurePlaying() {
    const player = engine.current;
    if (player.isPlaying) {
      return;
    }
    try {
      await player.play();
    } catch {
      setError(strings.audioDecodeFailed);
    }
  }

  async function hearBand(nextFrequency: number, nextGain: number) {
    setFrequency(nextFrequency);
    setGain(nextGain);
    setFlat(false);
    const player = engine.current;
    player.setBand({ frequencyHz: nextFrequency, gainDb: nextGain, q });
    player.setBypass(false);
    await ensurePlaying();
  }

  async function hearFlat() {
    setFrequency(null);
    setFlat(true);
    engine.current.setBypass(true);
    await ensurePlaying();
  }

  async function toggleFrequency(hz: number) {
    if (frequency === hz) {
      await hearFlat();
      return;
    }
    await hearBand(hz, gain);
  }

  const frequencies = preview.frequenciesHz ?? [];
  const gains = preview.gainsDb ?? [];
  const manyGains = gains.length > 1;

  return (
    <div className="space-y-5">
      {manyGains && (
        <div className="flex flex-wrap gap-2">
          {gains.map((value) => (
            <button
              key={value}
              type="button"
              onClick={() => {
                setGain(value);
                if (frequency !== null) {
                  void hearBand(frequency, value);
                }
              }}
              className={`rounded-full border px-4 py-2 text-sm font-semibold transition ${
                frequency !== null && gain === value
                  ? "border-accent bg-accent text-white"
                  : "border-line bg-white text-ink hover:border-accent/40"
              }`}
            >
              {formatGain(value)}
            </button>
          ))}
        </div>
      )}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        {frequencies.map((hz) => (
          <button
            key={hz}
            type="button"
            onClick={() => void toggleFrequency(hz)}
            className={`rounded-2xl border px-5 py-6 text-left transition duration-150 ${
              frequency === hz
                ? "border-accent bg-accent/10 shadow-[0_10px_24px_rgba(15,118,110,0.12)]"
                : "border-line bg-panel hover:border-accent/40 hover:shadow-[0_10px_24px_rgba(21,32,51,0.06)]"
            }`}
          >
            <span className="block text-xl font-semibold tracking-tight">{formatHz(hz)}</span>
            <span className="mt-1 block text-sm text-muted">{formatGain(gain)}</span>
          </button>
        ))}
      </div>
      {(frequency !== null || flat) && (
        <p className="text-sm font-medium text-muted">
          {strings.listenBand}: {frequency !== null ? `${formatHz(frequency)} · ${formatGain(gain)}` : strings.flat}
        </p>
      )}
      {error && <p className="text-sm text-warn">{error}</p>}
    </div>
  );
}

function CompressionPreview({ preview }: { preview: PreviewResponse }) {
  const player = useRef(new ClipPlayer());
  const [active, setActive] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => () => player.current.dispose(), []);

  async function hear(url: string, slug: string) {
    setActive(slug);
    try {
      await player.current.load(url);
      await player.current.play();
    } catch {
      setError(strings.audioDecodeFailed);
    }
  }

  return (
    <div className="grid gap-3 sm:grid-cols-2">
      {(preview.variants ?? []).map((variant) => (
        <button
          key={variant.variantSlug}
          type="button"
          onClick={() => void hear(variant.url, variant.variantSlug)}
          className={`rounded-2xl border px-5 py-4 text-left text-base font-semibold transition ${
            active === variant.variantSlug
              ? "border-accent bg-accent/10"
              : "border-line bg-panel hover:border-accent/40"
          }`}
        >
          {variant.label}
        </button>
      ))}
      {error && <p className="text-sm text-warn">{error}</p>}
    </div>
  );
}
