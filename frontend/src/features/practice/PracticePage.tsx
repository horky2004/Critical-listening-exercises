import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { usePractice } from "../../api/hooks";
import type { PracticeResponse } from "../../api/types";
import { audioFailureMessage } from "../../audio/audioErrors";
import { useEqEngine } from "../../audio/useEqEngine";
import { useListenHotkeys } from "../../audio/useListenHotkeys";
import { useListenVolume } from "../../audio/useListenVolume";
import { QueryState } from "../../components/QueryState";
import { Shell } from "../../components/Shell";
import { EqExerciseLayout } from "../../components/MnemonicCheatSheet";
import { CompressionVariantListen } from "../listen/CompressionVariantListen";
import { EqListenBar } from "../listen/EqListenBar";
import { formatGain, formatHz } from "../../lib/format";
import { strings } from "../../lib/strings";

export function PracticePage() {
  const { moduleSlug = "", sourceSlug = "" } = useParams();
  const practice = usePractice(moduleSlug, sourceSlug);

  return (
    <Shell>
      <Link
        to={`/modules/${moduleSlug}/sources/${sourceSlug}`}
        className="xl:absolute xl:left-10 xl:top-25 mb-2 inline-block text-sm font-medium text-muted transition hover:text-accent"
      >
        ← {strings.backToTree}
      </Link>
      <QueryState
        isPending={practice.isPending}
        error={practice.error}
        onRetry={() => void practice.refetch()}
      >
        {practice.data && <PracticeBody practice={practice.data} />}
      </QueryState>
    </Shell>
  );
}

function PracticeBody({ practice }: { practice: PracticeResponse }) {
  if (practice.mode === "eqBand" && practice.audio) {
    return <EqPractice practice={practice} />;
  }

  if (practice.mode === "compressionVariants" && practice.variants?.length) {
    return <CompressionPractice practice={practice} />;
  }

  return (
    <div className="mx-auto mt-8 max-w-2xl">
      <h1 className="text-3xl font-semibold tracking-tight">{strings.practice}</h1>
      <p className="mt-3 text-sm text-muted">{strings.practiceSoon}</p>
    </div>
  );
}

function CompressionPractice({ practice }: { practice: PracticeResponse }) {
  return (
    <div className="mx-auto mt-2 max-w-4xl space-y-8">
      <div>
        <p className="text-sm font-semibold uppercase tracking-[0.16em] text-warn">{strings.practice}</p>
        <p className="mt-2 text-sm font-medium text-accent">
          {practice.module.name} · {practice.source.name}
        </p>
        <p className="mt-3 w-full text-sm leading-6 text-muted">{strings.practiceHintCompression}</p>
      </div>
      <CompressionVariantListen variants={practice.variants ?? []} />
    </div>
  );
}

function EqPractice({ practice }: { practice: PracticeResponse }) {
  const engine = useEqEngine();
  const frequencies = practice.frequenciesHz ?? [];
  const gains = practice.gainsDb ?? [];
  const q = practice.q ?? 1;
  const [frequency, setFrequency] = useState(frequencies[0] ?? 1000);
  const [gain, setGain] = useState(gains[0] ?? 0);
  const [playing, setPlaying] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [ready, setReady] = useState(false);
  const [volume, setVolume] = useListenVolume();

  useEffect(() => {
    const url = practice.audio?.url;
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
      .catch((cause) => {
        if (!cancelled) {
          setError(audioFailureMessage(cause));
        }
      });
    return () => {
      cancelled = true;
    };
  }, [engine, practice.audio?.url]);

  function applyBand(nextFrequency: number, nextGain: number) {
    engine.setBand({ frequencyHz: nextFrequency, gainDb: nextGain, q }, { smooth: true });
  }

  async function play() {
    applyBand(frequency, gain);
    engine.setVolume(volume);
    engine.setBypass(false);
    try {
      await engine.play();
      setPlaying(true);
    } catch (cause) {
      setError(audioFailureMessage(cause));
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

  function chooseFrequency(hz: number) {
    setFrequency(hz);
    applyBand(hz, gain);
  }

  function chooseGain(value: number) {
    setGain(value);
    applyBand(frequency, value);
  }

  return (
    <EqExerciseLayout frequenciesHz={frequencies}>
    <div className="mx-auto mt-2 max-w-4xl space-y-8">
      <div>
        <p className="text-sm font-semibold uppercase tracking-[0.16em] text-warn">{strings.practice}</p>
        <p className="mt-2 text-sm font-medium text-accent">
          {practice.module.name} · {practice.source.name}
        </p>
        <p className="mt-3 w-full text-sm leading-6 text-muted">{strings.practiceHint}</p>
      </div>

      <div className="flex flex-wrap gap-2 justify-center">
        {gains.map((value) => (
          <button
            key={value}
            type="button"
            onClick={() => chooseGain(value)}
            aria-pressed={gain === value}
            className={`min-h-11 rounded-full border px-4 py-2 text-sm font-semibold transition ${
              gain === value
                ? "border-accent bg-accent text-on-accent"
                : "border-line bg-panel text-ink hover:border-accent/40"
            }`}
          >
            {formatGain(value)}
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

      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        {frequencies.map((hz) => (
          <button
            key={hz}
            type="button"
            onClick={() => chooseFrequency(hz)}
            aria-pressed={frequency === hz}
            className={`rounded-2xl border px-5 py-6 text-left transition duration-150 ${
              frequency === hz
                ? "border-accent bg-accent/12 shadow-[var(--lift-choice)]"
                : "border-line bg-panel hover:border-accent/40"
            }`}
          >
            <span className="block text-xl font-semibold tracking-tight">{formatHz(hz)}</span>
            <span className="mt-1 block text-sm text-muted">{formatGain(gain)}</span>
          </button>
        ))}
      </div>

      <p className="text-sm font-medium text-muted">
        {strings.listenBand}: {formatHz(frequency)} · {formatGain(gain)}
      </p>
    </div>
    </EqExerciseLayout>
  );
}
