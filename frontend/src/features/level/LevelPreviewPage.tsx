import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useEqEngine } from "../../audio/useEqEngine";
import { useListenHotkeys } from "../../audio/useListenHotkeys";
import { useListenVolume } from "../../audio/useListenVolume";
import { ApiError } from "../../api/client";
import { usePreview } from "../../api/hooks";
import type { PreviewResponse } from "../../api/types";
import { Button } from "../../components/Button";
import { QueryState } from "../../components/QueryState";
import { Shell } from "../../components/Shell";
import { CompressionVariantListen } from "../listen/CompressionVariantListen";
import { EqListenBar } from "../listen/EqListenBar";
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
    <div className="mx-auto mt-2 max-w-4xl space-y-8">
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
      <Button className="w-full" onClick={onStartTest}>
        {strings.startTest}
      </Button>
    </div>
  );
}

function EqPreview({ preview }: { preview: PreviewResponse }) {
  const engine = useEqEngine();
  const [frequency, setFrequency] = useState<number | null>(null);
  const [gain, setGain] = useState(preview.gainsDb?.[0] ?? 0);
  const [flat, setFlat] = useState(false);
  const [playing, setPlaying] = useState(false);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [volume, setVolume] = useListenVolume();
  const q = preview.q ?? 1;

  useEffect(() => {
    const url = preview.audio?.url;
    if (!url) {
      return;
    }
    let cancelled = false;
    void engine
      .load(url)
      .then(() => {
        if (!cancelled) {
          setReady(true);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setError(strings.audioDecodeFailed);
        }
      });
    return () => {
      cancelled = true;
    };
  }, [engine, preview.audio?.url]);

  function selectBand(nextFrequency: number, nextGain: number) {
    setFrequency(nextFrequency);
    setGain(nextGain);
    setFlat(false);
    engine.setBand({ frequencyHz: nextFrequency, gainDb: nextGain, q }, { smooth: true });
    engine.setBypass(false);
  }

  function selectFlat() {
    setFrequency(null);
    setFlat(true);
    engine.setBypass(true);
  }

  function toggleFrequency(hz: number) {
    if (frequency === hz) {
      selectFlat();
      return;
    }
    selectBand(hz, gain);
  }

  async function play() {
    if (frequency !== null) {
      engine.setBand({ frequencyHz: frequency, gainDb: gain, q }, { smooth: true });
      engine.setBypass(false);
    } else {
      engine.setBypass(true);
      setFlat(true);
    }
    engine.setVolume(volume);
    try {
      await engine.play();
      setPlaying(true);
    } catch {
      setError(strings.audioDecodeFailed);
    }
  }

  function stop() {
    engine.stop();
    setPlaying(false);
  }

  useListenHotkeys({
    enabled: ready,
    onTogglePlay: () => (playing ? stop() : void play())
  });

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
                if (frequency !== null) {
                  selectBand(frequency, value);
                  return;
                }
                setGain(value);
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
            onClick={() => toggleFrequency(hz)}
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
      <EqListenBar
        playing={playing}
        disabled={!ready}
        error={error}
        volume={volume}
        onPlay={() => void play()}
        onStop={stop}
        onVolumeChange={(next) => {
          setVolume(next);
          engine.setVolume(next);
        }}
      />
    </div>
  );
}

function CompressionPreview({ preview }: { preview: PreviewResponse }) {
  return <CompressionVariantListen variants={preview.variants ?? []} />;
}
