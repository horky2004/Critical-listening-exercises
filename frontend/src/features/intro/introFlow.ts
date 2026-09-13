import type { IntroQuiz, IntroResponse, LevelStatus } from "../../api/types";

export type IntroPhase = "a1" | "a2" | "b";

export type IntroStep =
  | { kind: "lessons"; phase: IntroPhase; frequenciesHz: number[]; quiz: IntroQuiz }
  | { kind: "ready-for-level-1" }
  | { kind: "done" };

function isCompleted(status: LevelStatus): boolean {
  return status === "Completed";
}

export function resolveIntroStep(intro: IntroResponse): IntroStep {
  const a1 = intro.quizzes.find((quiz) => quiz.key === "a1");
  const a2 = intro.quizzes.find((quiz) => quiz.key === "a2");
  const b = intro.quizzes.find((quiz) => quiz.key === "b");
  if (!a1 || !a2 || !b) {
    return { kind: "done" };
  }

  if (!isCompleted(a1.status)) {
    return { kind: "lessons", phase: "a1", frequenciesHz: a1.frequenciesHz, quiz: a1 };
  }

  if (!isCompleted(a2.status)) {
    return { kind: "lessons", phase: "a2", frequenciesHz: a2.frequenciesHz, quiz: a2 };
  }

  if (!isCompleted(intro.boost1Status)) {
    return { kind: "ready-for-level-1" };
  }

  if (!isCompleted(b.status)) {
    return { kind: "lessons", phase: "b", frequenciesHz: b.frequenciesHz, quiz: b };
  }

  return { kind: "done" };
}

export function introCta(intro: IntroResponse): "start" | "continue-b" | null {
  const step = resolveIntroStep(intro);
  if (step.kind === "lessons" && (step.phase === "a1" || step.phase === "a2")) {
    return "start";
  }
  if (step.kind === "lessons" && step.phase === "b") {
    return "continue-b";
  }
  return null;
}
