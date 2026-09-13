import { useEffect, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { ApiError } from "../../api/client";
import { invalidateProgress, useAbandon, useAnswer, useSession, useStartSession } from "../../api/hooks";
import type { AnswerView, QuestionView, SessionResultView, SessionView } from "../../api/types";
import { useEqEngine } from "../../audio/useEqEngine";
import { useListenHotkeys } from "../../audio/useListenHotkeys";
import { useListenVolume } from "../../audio/useListenVolume";
import { Button } from "../../components/Button";
import { QueryState } from "../../components/QueryState";
import { Shell } from "../../components/Shell";
import { EqListenBar } from "../listen/EqListenBar";
import { EqMoveGraph } from "../listen/EqMoveGraph";
import { SignalCompare } from "../listen/SignalCompare";
import { scoreLine } from "../../lib/format";
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
  const [playing, setPlaying] = useState(false);
  const [source, setSource] = useState(false);
  const [audioReady, setAudioReady] = useState(false);
  const [audioError, setAudioError] = useState<string | null>(null);
  const [volume, setVolume] = useListenVolume();
  const locked = Boolean(feedback);
  const canListen = Boolean(question.eq) && audioReady;

  useEffect(() => {
    if (!question.eq) {
      return;
    }
    let cancelled = false;
    setAudioReady(false);
    setAudioError(null);
    void engine
      .load(question.audio.url)
      .then(() => {
        if (!cancelled) {
          engine.setBand(question.eq!);
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
  }, [engine, question.audio.url]);

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
    if (!question.eq || !audioReady) {
      return;
    }
    engine.setBand(question.eq);
    engine.setVolume(volume);
    engine.setBypass(source);
    void engine
      .play()
      .then(() => setPlaying(true))
      .catch(() => setAudioError(strings.audioDecodeFailed));
  }

  function stop() {
    engine.stop();
    setPlaying(false);
  }

  useListenHotkeys({
    enabled: canListen && !feedback?.result,
    onTogglePlay: () => (playing ? stop() : play()),
    onToggleCompare: () => hearSource(!source)
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
            engine.stop();
            setPlaying(false);
            invalidateProgress(queryClient, session.module.slug, session.source.slug);
          }
        }
      }
    );
  }

  if (feedback?.result) {
    return (
      <ResultBlock session={session} result={feedback.result} lastAnswer={feedback} eq={question.eq} />
    );
  }

  return (
    <div className="mx-auto max-w-4xl">
      <div className="mb-2 flex items-start justify-between gap-4">
        <div>
          <p className="text-sm font-medium text-accent">
            {session.module.name} · {session.source.name}
          </p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">{session.level.title}</h1>
        </div>
        <Button variant="ghost" onClick={onAbandon}>
          {strings.abandon}
        </Button>
      </div>

      <p className="tabular text-sm font-medium text-muted">
        {strings.question} {question.questionIndex} {strings.of} {question.questionCount}
        {" · "}
        {correctSoFar} {strings.of} {question.questionCount}
      </p>
      <h2 className="mt-2 text-3xl font-semibold tracking-tight">{question.prompt}</h2>

      <div className="mt-8 space-y-3">
        {question.eq ? (
          <>
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
              }}
            />
            <SignalCompare source={source} disabled={!audioReady} onSelect={hearSource} />
          </>
        ) : (
          <div className="rounded-2xl border border-dashed border-line bg-panel px-6 py-12 text-center">
            <p className="text-sm text-muted">{strings.audioSoon}</p>
          </div>
        )}
      </div>

      <div className="mt-6 grid gap-3 sm:grid-cols-2">
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
              className={`rounded-2xl border px-5 py-4 text-left transition ${
                isCorrect
                  ? "border-good bg-emerald-50 text-good"
                  : isWrongPick
                    ? "border-bad bg-red-50 text-bad"
                    : "border-line bg-panel hover:border-accent/50 hover:shadow-[0_8px_20px_rgba(21,32,51,0.06)]"
              }`}
            >
              <span className="flex items-baseline justify-between gap-3">
                <span className="text-base font-semibold">{option.label}</span>
                {verdict && <span className="text-sm font-semibold">{verdict}</span>}
              </span>
            </button>
          );
        })}
      </div>

      {feedback && (
        <div className="mt-6 space-y-4">
          {feedback.nextQuestion && (
            <Button
              className="w-full py-3.5 text-base"
              onClick={() => {
                setQuestion(feedback.nextQuestion!);
                setFeedback(null);
                setPicked(null);
                hearSource(false);
              }}
            >
              {strings.next}
            </Button>
          )}
          {question.eq && <EqMoveGraph band={question.eq} />}
        </div>
      )}

      {answer.error && <p className="mt-4 text-sm text-bad">{answer.error.message}</p>}
    </div>
  );
}

function ResultBlock({
  session,
  result,
  lastAnswer,
  eq
}: {
  session: SessionView;
  result: SessionResultView;
  lastAnswer?: AnswerView;
  eq?: QuestionView["eq"];
}) {
  const intro = session.level.segmentKey === "intro";
  const nextPath = sourcePath(session, intro);
  const unlocked = result.newlyUnlockedLevels.filter((level) => level.segmentKey !== "intro");

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <div>
        <p className="text-sm font-medium text-accent">
          {session.module.name} · {session.source.name}
        </p>
        <h1 className="mt-1 text-3xl font-semibold tracking-tight">{session.level.title}</h1>
      </div>
      <div className="rounded-3xl border border-line bg-panel p-8 shadow-[0_16px_40px_rgba(21,32,51,0.06)]">
        <p className={`text-sm font-semibold ${intro || result.passed ? "text-good" : "text-bad"}`}>
          {intro ? strings.introQuizDone : result.passed ? strings.passed : strings.failed}
        </p>
        <p className="tabular mt-3 text-5xl font-semibold tracking-tight">
          {scoreLine(result.correctAnswers, result.questionCount, result.scorePercentage)}
        </p>
        {lastAnswer && (
          <p className="mt-2 text-sm text-muted">
            {lastAnswer.isCorrect ? strings.correct : strings.incorrect}
          </p>
        )}
        {intro && <p className="mt-3 text-sm text-muted">{strings.introQuizDoneHint}</p>}
        {!intro && result.isFirstPass && <p className="mt-3 text-sm text-accent">{strings.firstPass}</p>}
      </div>
      <Link to={nextPath} className="block">
        <Button className="w-full py-3.5 text-base">
          {intro ? strings.continueIntro : strings.backToTree}
        </Button>
      </Link>
      {eq && <EqMoveGraph band={eq} />}
      {unlocked.length > 0 && (
        <div>
          <h2 className="mb-2 text-sm text-muted">{strings.unlocked}</h2>
          <ul className="space-y-1 text-sm">
            {unlocked.map((level) => (
              <li key={level.levelId}>{level.title}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
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
