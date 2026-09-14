import { useEffect, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { ApiError } from "../../api/client";
import { invalidateProgress, useAbandon, useAnswer, useSession, useSources, useStartSession, useTree } from "../../api/hooks";
import type { AnswerView, QuestionView, SessionResultView, SessionView, TreeResponse } from "../../api/types";
import { useClipPlayer } from "../../audio/useClipPlayer";
import { useEqEngine } from "../../audio/useEqEngine";
import { useListenHotkeys } from "../../audio/useListenHotkeys";
import { useListenVolume } from "../../audio/useListenVolume";
import { Button } from "../../components/Button";
import { QueryState } from "../../components/QueryState";
import { ScoreDot } from "../../components/ScoreDot";
import { Shell } from "../../components/Shell";
import { EqListenBar } from "../listen/EqListenBar";
import { EqMoveGraph } from "../listen/EqMoveGraph";
import { SignalCompare } from "../listen/SignalCompare";
import { hasFrequencyIntro, musicalEqSourceSlugs } from "../intro/introFlow";
import { strings } from "../../lib/strings";

export function TestPage() {
  const { moduleSlug = "", sourceSlug = "", levelId, sessionId } = useParams();
  const navigate = useNavigate();
  const start = useStartSession();
  const started = useRef(false);

  useEffect(() => {
    if (sessionId || !levelId || started.current) {
      return;
    }
    started.current = true;
    start.mutate(
      { moduleSlug, sourceSlug, levelId },
      { onSuccess: (session) => navigate(`/sessions/${session.sessionId}`, { replace: true }) }
    );
  }, [levelId, moduleSlug, navigate, sessionId, sourceSlug, start]);

  if (!sessionId) {
    return (
      <Shell>
        <QueryState
          isPending={start.isPending || !start.isError}
          error={start.error ? new Error(startErrorMessage(start.error)) : null}
          onRetry={() => {
            started.current = false;
            started.current = true;
            start.mutate(
              { moduleSlug, sourceSlug, levelId: levelId ?? "" },
              { onSuccess: (session) => navigate(`/sessions/${session.sessionId}`, { replace: true }) }
            );
          }}
        >
          <p className="text-muted">{strings.loading}</p>
        </QueryState>
      </Shell>
    );
  }

  return <ActiveSession sessionId={sessionId} />;
}

function ActiveSession({ sessionId }: { sessionId: string }) {
  const session = useSession(sessionId);
  const abandon = useAbandon(sessionId);
  const navigate = useNavigate();

  return (
    <Shell>
      <QueryState isPending={session.isPending} error={session.error} onRetry={() => void session.refetch()}>
        {session.data && (
          <SessionBody
            session={session.data}
            onAbandon={() => {
              if (!window.confirm(strings.abandonConfirm)) {
                return;
              }
              abandon.mutate(undefined, {
                onSuccess: () => {
                  navigate(sourcePath(session.data, session.data.level.segmentKey === "intro"));
                }
              });
            }}
          />
        )}
      </QueryState>
    </Shell>
  );
}

function SessionBody({ session, onAbandon }: { session: SessionView; onAbandon: () => void }) {
  if (session.result) {
    return <ResultBlock session={session} result={session.result} />;
  }

  if (!session.currentQuestion) {
    return <p className="text-muted">{strings.error}</p>;
  }

  return <QuestionBlock session={session} initial={session.currentQuestion} onAbandon={onAbandon} />;
}

function QuestionBlock({
  session,
  initial,
  onAbandon
}: {
  session: SessionView;
  initial: QuestionView;
  onAbandon: () => void;
}) {
  const answer = useAnswer(session.sessionId);
  const queryClient = useQueryClient();
  const [question, setQuestion] = useState(initial);
  const [feedback, setFeedback] = useState<AnswerView | null>(null);
  const [picked, setPicked] = useState<string | null>(null);
  const [correctSoFar, setCorrectSoFar] = useState(session.correctSoFar ?? 0);
  const engine = useEqEngine();
  const clip = useClipPlayer();
  const [playing, setPlaying] = useState(false);
  const [source, setSource] = useState(false);
  const [audioReady, setAudioReady] = useState(false);
  const [audioError, setAudioError] = useState<string | null>(null);
  const [volume, setVolume] = useListenVolume();
  const [showResult, setShowResult] = useState(false);
  const locked = Boolean(feedback);
  const isEq = Boolean(question.eq);

  useEffect(() => {
    let cancelled = false;
    setAudioReady(false);
    setAudioError(null);
    setPlaying(false);
    const load = isEq
      ? engine.load(question.audio.url).then(() => {
          if (!cancelled && question.eq) {
            engine.setBand(question.eq);
          }
        })
      : clip.load(question.audio.url);
    void load
      .then(() => {
        if (!cancelled) {
          setAudioReady(true);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setAudioError(strings.audioDecodeFailed);
        }
      });
    return () => {
      cancelled = true;
    };
  }, [clip, engine, isEq, question.audio.url]);

  useEffect(() => {
    if (question.eq) {
      engine.setBand(question.eq);
    }
  }, [engine, question.eq?.frequencyHz, question.eq?.gainDb, question.eq?.q]);

  function hearSource(next: boolean) {
    setSource(next);
    engine.setBypass(next);
  }

  function play() {
    if (!audioReady) {
      return;
    }
    if (question.eq) {
      engine.setBand(question.eq);
      engine.setVolume(volume);
      engine.setBypass(source);
      void engine
        .play()
        .then(() => setPlaying(true))
        .catch(() => setAudioError(strings.audioDecodeFailed));
      return;
    }
    clip.setVolume(volume);
    void clip
      .play()
      .then(() => setPlaying(true))
      .catch(() => setAudioError(strings.audioDecodeFailed));
  }

  function stop() {
    engine.stop();
    clip.stop();
    setPlaying(false);
  }

  useListenHotkeys({
    enabled: audioReady && !showResult,
    onTogglePlay: () => (playing ? stop() : play()),
    onToggleCompare: isEq ? () => hearSource(!source) : undefined
  });

  function submit(answerKey: string) {
    if (locked || answer.isPending) {
      return;
    }
    setPicked(answerKey);
    answer.mutate(
      { index: question.questionIndex, answerKey },
      {
        onSuccess: (data) => {
          setFeedback(data);
          setCorrectSoFar(data.correctSoFar);
          if (data.result) {
            invalidateProgress(queryClient, session.module.slug, session.source.slug);
          }
        }
      }
    );
  }

  if (showResult && feedback?.result) {
    return <ResultBlock session={session} result={feedback.result} />;
  }

  return (
    <div className="mx-auto max-w-4xl">
      <div className="mb-2 flex items-start justify-between gap-4">
        <div>
          <p className="text-sm font-medium text-accent">
            {session.module.name} · {session.source.name} · {session.level.title}
          </p>
        </div>
        <a href="#" className="text-sm font-medium hover:underline" onClick={onAbandon}>
          {strings.abandon}
        </a>
      </div>

      <p className="tabular text-sm font-medium text-muted">
        {strings.question} {question.questionIndex} {strings.of} {question.questionCount}
        {" · "}
        {correctSoFar} {strings.of} {question.questionCount}
      </p>
      <h2 className="mt-2 text-3xl font-semibold tracking-tight text-center">{question.prompt}</h2>

      <div className="mt-5 space-y-3">
        <EqListenBar
          playing={playing}
          disabled={!audioReady}
          error={audioError}
          volume={volume}
          onPlay={play}
          onStop={stop}
          onVolumeChange={(next) => {
            setVolume(next);
            engine.setVolume(next);
            clip.setVolume(next);
          }}
        />
        {question.eq ? (
          <SignalCompare source={source} disabled={!audioReady} onSelect={hearSource} />
        ) : (
          <p className="text-center text-sm text-muted">{strings.listenShortcutsClip}</p>
        )}
      </div>

      <div className={`mt-6 grid grid-cols-2 gap-4 ${answerGridColumns(question.answerOptions.length)}`}>
        {question.answerOptions.map((option) => {
          const isCorrect = feedback?.correctAnswerKey === option.key;
          const isPicked = picked === option.key;
          const isWrongPick = Boolean(feedback && isPicked && !feedback.isCorrect);
          const verdict = isPicked && feedback
            ? feedback.isCorrect
              ? strings.correct
              : strings.incorrect
            : null;
          return (
            <button
              key={option.key}
              type="button"
              disabled={locked || answer.isPending}
              onClick={() => submit(option.key)}
              className={`rounded-2xl border px-5 py-6 text-left transition duration-150 ${
                isCorrect
                  ? "border-good bg-emerald-50 text-good"
                  : isWrongPick
                    ? "border-bad bg-red-50 text-bad"
                    : "border-line bg-panel hover:border-accent/40 hover:shadow-[0_10px_24px_rgba(21,32,51,0.06)]"
              }`}
            >
              <span className="flex w-full items-baseline justify-between gap-3">
                <span className="text-xl font-semibold tracking-tight">{option.label}</span>
                {verdict && <span className="shrink-0 text-sm font-semibold">{verdict}</span>}
              </span>
            </button>
          );
        })}
      </div>

      {feedback && (
        <div className="mt-6 space-y-4">
          <Button
            className="w-full py-3.5 text-base"
            onClick={() => {
              if (feedback.nextQuestion) {
                setQuestion(feedback.nextQuestion);
                setFeedback(null);
                setPicked(null);
                hearSource(false);
                return;
              }
              stop();
              setShowResult(true);
            }}
          >
            {feedback.nextQuestion ? strings.next : strings.finishTest}
          </Button>
          {question.eq && <EqMoveGraph band={question.eq} />}
        </div>
      )}

      {answer.error && <p className="mt-4 text-sm text-bad">{answer.error.message}</p>}
    </div>
  );
}

function ResultBlock({
  session,
  result
}: {
  session: SessionView;
  result: SessionResultView;
}) {
  const intro = session.level.segmentKey === "intro";
  const introA2 = intro && session.level.levelNumber === 2;
  const introB = intro && session.level.levelNumber === 3;
  const boost1Passed =
    result.passed
    && hasFrequencyIntro(session.module.slug, session.source.slug)
    && session.level.segmentKey === "boost"
    && session.level.levelNumber === 1;
  const tree = useTree(session.module.slug, session.source.slug);
  const sources = useSources(session.module.slug);
  const unlocked = unlockedLevels(
    result,
    session,
    tree.data,
    sources.data?.sources,
    introA2 ? { segment: "boost", levelNumber: 1 } : introB ? { segment: "boost", levelNumber: 2 } : null,
    introB
  );
  const nextPath = introA2 && unlocked[0]
    ? `/modules/${session.module.slug}/sources/${session.source.slug}/levels/${unlocked[0].levelId}`
    : sourcePath(session, (intro && !introB) || boost1Passed);
  const outcome = intro ? strings.introQuizDone : result.passed ? strings.passed : strings.failed;
  const nextLabel = introA2
    ? strings.continueToLevel1
    : introB
      ? strings.continueTests
      : intro
        ? strings.continueIntro
        : boost1Passed
          ? strings.continueFrequencyIntro
          : strings.backToTree;
  const introHint = introB ? strings.introQuizDoneHintB : introA2 ? strings.introQuizDoneHintA2 : strings.introQuizDoneHint;

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <div>
        <p className="text-sm font-medium text-accent">
          {session.module.name} · {session.source.name}
        </p>
        <h1 className="mt-1 text-3xl font-semibold tracking-tight">{session.level.title}</h1>
      </div>
      <div className="rounded-3xl border border-line bg-panel p-8 shadow-[0_16px_40px_rgba(21,32,51,0.06)]">
        <div className="flex items-center gap-6">
          <ScoreDot
            score={result.correctAnswers}
            total={result.questionCount}
            threshold={session.passThreshold}
            className="size-20"
          />
          <div>
            <p className="tabular text-5xl font-semibold tracking-tight">
              {result.correctAnswers} / {result.questionCount}
            </p>
            <p className={`mt-2 text-sm font-semibold ${intro || result.passed ? "text-good" : "text-bad"}`}>
              {outcome}
            </p>
            {intro && <p className="mt-2 text-sm text-muted">{introHint}</p>}
            {!intro && result.isFirstPass && <p className="mt-2 text-sm text-accent">{strings.firstPass}</p>}
          </div>
        </div>
      </div>
      {result.passed && unlocked.length > 0 && (
        <div className="space-y-2">
          <h2 className="text-sm font-semibold text-muted">{strings.unlocked}</h2>
          {unlocked.map((level) => (
            <Link
              key={`${level.sourceSlug}-${level.levelId}`}
              to={`/modules/${session.module.slug}/sources/${level.sourceSlug}/levels/${level.levelId}`}
              className="block w-full rounded-xl border border-line bg-panel px-4 py-3 transition hover:border-accent/50 hover:shadow-[0_8px_20px_rgba(21,32,51,0.06)]"
            >
              <p className="text-sm font-semibold tracking-tight">
                {level.sourceName} - {level.title}
              </p>
            </Link>
          ))}
        </div>
      )}
      <Link to={nextPath} className="block">
        <Button className="w-full py-3.5 text-base">{nextLabel}</Button>
      </Link>
    </div>
  );
}

function unlockedLevels(
  result: SessionResultView,
  session: SessionView,
  tree: TreeResponse | undefined,
  sources: { slug: string; name: string }[] | undefined,
  fallback: { segment: string; levelNumber: number } | null,
  introB: boolean
) {
  const fromResult = result.newlyUnlockedLevels
    .filter((level) => level.segmentKey !== "intro")
    .map((level) => ({
      levelId: level.levelId,
      title: level.title,
      sourceSlug: level.sourceSlug || session.source.slug,
      sourceName: level.sourceName || session.source.name
    }));

  const cards =
    fromResult.length > 0 || !fallback
      ? fromResult
      : fallbackCard(tree, fallback, session);

  if (!introB) {
    return cards;
  }

  const boost1 = tree?.segments.find((segment) => segment.key === "boost")?.levels.find((level) => level.levelNumber === 1);
  if (!boost1 || !sources) {
    return cards;
  }

  const extra = sources
    .filter((source) => (musicalEqSourceSlugs as readonly string[]).includes(source.slug))
    .filter((source) => !cards.some((card) => card.sourceSlug === source.slug && card.levelId === boost1.levelId))
    .map((source) => ({
      levelId: boost1.levelId,
      title: boost1.title,
      sourceSlug: source.slug,
      sourceName: source.name
    }));

  return [...cards, ...extra];
}

function fallbackCard(
  tree: TreeResponse | undefined,
  fallback: { segment: string; levelNumber: number },
  session: SessionView
) {
  const level = tree?.segments
    .find((segment) => segment.key === fallback.segment)
    ?.levels.find((item) => item.levelNumber === fallback.levelNumber);
  if (!level || level.status === "Locked") {
    return [];
  }

  return [
    {
      levelId: level.levelId,
      title: level.title,
      sourceSlug: session.source.slug,
      sourceName: session.source.name
    }
  ];
}

function answerGridColumns(count: number): string {
  if (count <= 2) {
    return "";
  }
  if (count === 3) {
    return "lg:grid-cols-3";
  }
  return "lg:grid-cols-4";
}

function sourcePath(session: SessionView, toIntro: boolean): string {
  const base = `/modules/${session.module.slug}/sources/${session.source.slug}`;
  return toIntro ? `${base}/intro` : base;
}

function startErrorMessage(error: unknown): string {
  if (error instanceof ApiError && error.problem?.type?.includes("source-locked")) {
    return strings.sourceLockedHint;
  }
  if (error instanceof ApiError && error.problem?.type?.includes("level-locked")) {
    return strings.lockedLevel;
  }
  return error instanceof Error ? error.message : strings.error;
}
