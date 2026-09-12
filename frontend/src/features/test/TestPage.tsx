import { useEffect, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { ApiError } from "../../api/client";
import { invalidateProgress, useAbandon, useAnswer, useSession, useStartSession } from "../../api/hooks";
import type { AnswerView, QuestionView, SessionResultView, SessionView } from "../../api/types";
import { Button } from "../../components/Button";
import { QueryState } from "../../components/QueryState";
import { Shell } from "../../components/Shell";
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
                  navigate(`/modules/${session.data.module.slug}/sources/${session.data.source.slug}`);
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
  const locked = Boolean(feedback);

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

  if (feedback?.result) {
    return <ResultBlock session={session} result={feedback.result} lastAnswer={feedback} />;
  }

  return (
    <div className="mx-auto max-w-4xl">
      <div className="mb-8 flex items-start justify-between gap-4">
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

      <div className="mt-8 rounded-2xl border border-dashed border-line bg-panel px-6 py-12 text-center shadow-[0_10px_30px_rgba(21,32,51,0.04)]">
        <p className="text-sm text-muted">{strings.audioSoon}</p>
      </div>

      <div className="mt-6 grid gap-3 sm:grid-cols-2">
        {question.answerOptions.map((option) => {
          const isCorrect = feedback?.correctAnswerKey === option.key;
          const isWrongPick = Boolean(feedback && picked === option.key && !feedback.isCorrect);
          return (
            <button
              key={option.key}
              type="button"
              disabled={locked || answer.isPending}
              onClick={() => submit(option.key)}
              className={`rounded-2xl border px-5 py-4 text-left text-base font-semibold transition ${
                isCorrect
                  ? "border-good bg-emerald-50 text-good"
                  : isWrongPick
                    ? "border-bad bg-red-50 text-bad"
                    : "border-line bg-panel hover:border-accent/50 hover:shadow-[0_8px_20px_rgba(21,32,51,0.06)]"
              }`}
            >
              {option.label}
            </button>
          );
        })}
      </div>

      {feedback && (
        <div className="mt-6 flex items-center justify-between">
          <p className={`text-sm font-semibold ${feedback.isCorrect ? "text-good" : "text-bad"}`}>
            {feedback.isCorrect ? strings.correct : `${strings.incorrect}`}
          </p>
          {feedback.nextQuestion && (
            <Button
              onClick={() => {
                setQuestion(feedback.nextQuestion!);
                setFeedback(null);
                setPicked(null);
              }}
            >
              {strings.next}
            </Button>
          )}
        </div>
      )}

      {answer.error && <p className="mt-4 text-sm text-bad">{answer.error.message}</p>}
    </div>
  );
}

function ResultBlock({
  session,
  result,
  lastAnswer
}: {
  session: SessionView;
  result: SessionResultView;
  lastAnswer?: AnswerView;
}) {
  const treePath = `/modules/${session.module.slug}/sources/${session.source.slug}`;

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div>
        <p className="text-sm font-medium text-accent">
          {session.module.name} · {session.source.name}
        </p>
        <h1 className="mt-1 text-3xl font-semibold tracking-tight">{session.level.title}</h1>
      </div>
      <div className="rounded-3xl border border-line bg-panel p-8 shadow-[0_16px_40px_rgba(21,32,51,0.06)]">
        <p className={`text-sm font-semibold ${result.passed ? "text-good" : "text-bad"}`}>
          {result.passed ? strings.passed : strings.failed}
        </p>
        <p className="tabular mt-3 text-5xl font-semibold tracking-tight">
          {scoreLine(result.correctAnswers, result.questionCount, result.scorePercentage)}
        </p>
        {lastAnswer && (
          <p className="mt-2 text-sm text-muted">
            {lastAnswer.isCorrect ? strings.correct : strings.incorrect}
          </p>
        )}
        {result.isFirstPass && <p className="mt-3 text-sm text-accent">{strings.firstPass}</p>}
      </div>
      {result.newlyUnlockedLevels.length > 0 && (
        <div>
          <h2 className="mb-2 text-sm text-muted">{strings.unlocked}</h2>
          <ul className="space-y-1 text-sm">
            {result.newlyUnlockedLevels.map((level) => (
              <li key={level.levelId}>{level.title}</li>
            ))}
          </ul>
        </div>
      )}
      <Link to={treePath}>
        <Button>{strings.backToTree}</Button>
      </Link>
    </div>
  );
}

function startErrorMessage(error: unknown): string {
  if (error instanceof ApiError && error.problem?.type?.includes("level-locked")) {
    return strings.lockedLevel;
  }
  return error instanceof Error ? error.message : strings.error;
}
