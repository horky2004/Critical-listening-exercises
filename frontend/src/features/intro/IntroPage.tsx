import { useEffect, useState } from "react";
import { Link, Navigate, useNavigate, useParams } from "react-router-dom";
import { useIntro } from "../../api/hooks";
import type { IntroResponse } from "../../api/types";
import { useEqEngine } from "../../audio/useEqEngine";
import { useListenHotkeys } from "../../audio/useListenHotkeys";
import { useListenVolume } from "../../audio/useListenVolume";
import { Button } from "../../components/Button";
import { QueryState } from "../../components/QueryState";
import { Shell } from "../../components/Shell";
import { formatHz } from "../../lib/format";
import { strings } from "../../lib/strings";
import { EqListenBar } from "../listen/EqListenBar";
import { SignalCompare } from "../listen/SignalCompare";
import { mnemonicFor } from "./frequencies";
import { hasFrequencyIntro, resolveIntroStep, type IntroStep } from "./introFlow";

const lessonGainDb = 12;

export function IntroPage() {
  const { moduleSlug = "", sourceSlug = "" } = useParams();
  const intro = useIntro(moduleSlug, sourceSlug);

  if (!hasFrequencyIntro(moduleSlug, sourceSlug)) {
    return <Navigate to={`/modules/${moduleSlug}/sources/${sourceSlug}`} replace />;
  }

  return (
    <Shell>
      <Link
        to={`/modules/${moduleSlug}/sources/${sourceSlug}`}
        className="text-sm font-medium text-muted transition hover:text-accent"
      >
        ← {strings.backToTree}
      </Link>
      <QueryState isPending={intro.isPending} error={intro.error} onRetry={() => void intro.refetch()}>
        {intro.data && <IntroBody intro={intro.data} moduleSlug={moduleSlug} sourceSlug={sourceSlug} />}
      </QueryState>
    </Shell>
  );
}

function IntroBody({
  intro,
  moduleSlug,
  sourceSlug
}: {
  intro: IntroResponse;
  moduleSlug: string;
  sourceSlug: string;
}) {
  const step = resolveIntroStep(intro);
  const treePath = `/modules/${moduleSlug}/sources/${sourceSlug}`;

  if (step.kind === "ready-for-level-1") {
    return (
      <GateCard
        kicker={intro.module.name}
        title={strings.introPhaseADone}
        body={strings.introPhaseADoneHint}
        action={strings.goToLevel1}
        to={treePath}
      />
    );
  }

  if (step.kind === "done") {
    return (
      <GateCard
        kicker={intro.module.name}
        title={strings.introDone}
        body={strings.introDoneHint}
        action={strings.backToTree}
        to={treePath}
      />
    );
  }

  return <LessonFlow intro={intro} step={step} moduleSlug={moduleSlug} sourceSlug={sourceSlug} />;
}

function GateCard({
  kicker,
  title,
  body,
  action,
  to
}: {
  kicker: string;
  title: string;
  body: string;
  action: string;
  to: string;
}) {
  return (
    <div className="mx-auto mt-8 max-w-xl text-center">
      <p className="text-sm font-medium text-accent">{kicker}</p>
      <h1 className="mt-2 text-3xl font-semibold tracking-tight text-center">{title}</h1>
      <p className="mt-3 text-sm text-muted">{body}</p>
      <Link to={to} className="mt-8 block">
        <Button className="w-full py-3.5 text-base">{action}</Button>
      </Link>
    </div>
  );
}

function LessonFlow({
  intro,
  step,
  moduleSlug,
  sourceSlug
}: {
  intro: IntroResponse;
  step: Extract<IntroStep, { kind: "lessons" }>;
  moduleSlug: string;
  sourceSlug: string;
}) {
  const navigate = useNavigate();
  const frequencies = step.frequenciesHz;
  const [index, setIndex] = useState(0);
  const hz = frequencies[index] ?? frequencies[0];
  const card = mnemonicFor(hz);
  const engine = useEqEngine();
  const [playing, setPlaying] = useState(false);
  const [source, setSource] = useState(false);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [volume, setVolume] = useListenVolume();
  const last = index === frequencies.length - 1;

  useEffect(() => {
    setIndex(0);
    setSource(false);
    setPlaying(false);
  }, [step.phase]);

  useEffect(() => {
    let cancelled = false;
    setReady(false);
    setError(null);
    void engine
      .load(intro.audio.url)
      .then(() => {
        if (!cancelled) {
          engine.setBand({ frequencyHz: hz, gainDb: lessonGainDb, q: intro.q });
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
  }, [engine, intro.audio.url, intro.q, hz]);

  useEffect(() => {
    engine.setBand({ frequencyHz: hz, gainDb: lessonGainDb, q: intro.q });
  }, [engine, hz, intro.q]);

  function hearSource(next: boolean) {
    setSource(next);
    engine.setBypass(next);
  }

  function play() {
    engine.setBand({ frequencyHz: hz, gainDb: lessonGainDb, q: intro.q });
    engine.setVolume(volume);
    engine.setBypass(source);
    void engine
      .play()
      .then(() => setPlaying(true))
      .catch(() => setError(strings.audioDecodeFailed));
  }

  function stop() {
    engine.stop();
    setPlaying(false);
  }

  useListenHotkeys({
    enabled: ready,
    onTogglePlay: () => (playing ? stop() : play()),
    onToggleCompare: () => hearSource(!source)
  });

  function goNext() {
    engine.stop();
    setPlaying(false);
    if (!last) {
      setIndex((current) => current + 1);
      setSource(false);
      return;
    }
    navigate(`/modules/${moduleSlug}/sources/${sourceSlug}/test/${step.quiz.levelId}`);
  }

  return (
    <div className="mx-auto mt-6 max-w-2xl">
      <p className="text-center text-sm font-medium text-muted">
        {phaseTitle(step.phase)} ({index + 1}/{frequencies.length})
      </p>

      <div className="mt-4">
        <EqListenBar
          playing={playing}
          disabled={!ready}
          error={error}
          volume={volume}
          onPlay={play}
          onStop={stop}
          onVolumeChange={(next) => {
            setVolume(next);
            engine.setVolume(next);
          }}
        />
      </div>

      <div className="mt-6 rounded-3xl border border-line bg-panel px-6 py-12 text-center shadow-[0_16px_40px_rgba(21,32,51,0.06)]">
        <p className="text-7xl font-semibold tracking-tight text-accent sm:text-8xl">{card.sound}</p>
        <p className="mt-6 text-4xl font-semibold tracking-tight">{formatHz(card.hz)}</p>
        <p className="mt-2 text-base text-muted">{card.asIn}</p>
        <p className="mt-3 text-sm text-muted">{card.hint}</p>
        <div className="mx-auto mt-8 max-w-md">
          <SignalCompare
            source={source}
            disabled={!ready}
            onSelect={hearSource}
            variant="pill"
            hint={strings.introCompareHint}
          />
        </div>
      </div>

      <div className="mt-5 space-y-3">
        <Button className="w-full py-3.5 text-base" onClick={goNext}>
          {last ? strings.introStartQuiz : strings.introGotIt}
        </Button>
        <Button
          variant="ghost"
          className="w-full"
          disabled={index === 0}
          onClick={() => {
            engine.stop();
            setPlaying(false);
            setIndex((current) => Math.max(0, current - 1));
            setSource(false);
          }}
        >
          {strings.back}
        </Button>
      </div>
    </div>
  );
}

function phaseTitle(phase: "a1" | "a2" | "b"): string {
  if (phase === "a1") {
    return strings.introMeetA1;
  }
  if (phase === "a2") {
    return strings.introMeetA2;
  }
  return strings.introMeetB;
}
