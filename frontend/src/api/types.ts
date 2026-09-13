export type LevelStatus = "Locked" | "Unlocked" | "InProgress" | "Completed";

export type MeResponse = {
  id: string;
  email: string;
  displayName: string;
  role: "Student" | "Admin";
  cohort: { id: string; name: string } | null;
};

export type ModuleListItem = {
  slug: string;
  name: string;
  description: string;
  isAvailable: boolean;
  unavailableReason: string | null;
  sourceCount: number;
  levelCount: number;
  completedLevelCount: number;
  totalLevelCount: number;
};

export type SourceListItem = {
  slug: string;
  name: string;
  completedLevelCount: number;
  levelCount: number;
  hasPracticeMode: boolean;
  isAvailable: boolean;
  unavailableReason: string | null;
};

export type TreeLevel = {
  levelId: string;
  levelNumber: number;
  title: string;
  status: LevelStatus;
  bestScore: number;
  bestScorePercentage: number;
  questionCount: number;
  passThreshold: number;
  attemptCount: number;
  firstPassedAt: string | null;
  requiredLevelIds: string[];
};

export type TreeSegment = {
  key: string;
  name: string;
  levels: TreeLevel[];
};

export type TreeResponse = {
  module: { slug: string; name: string };
  source: { slug: string; name: string };
  segments: TreeSegment[];
};

export type AnswerOption = {
  key: string;
  label: string;
};

export type QuestionView = {
  questionIndex: number;
  questionCount: number;
  prompt: string;
  answerOptions: AnswerOption[];
  audio: { url: string; loop: boolean };
  eq: { frequencyHz: number; gainDb: number; q: number } | null;
};

export type SessionResultView = {
  correctAnswers: number;
  questionCount: number;
  scorePercentage: number;
  passed: boolean;
  isFirstPass: boolean;
  newlyUnlockedLevels: {
    levelId: string;
    segmentKey: string;
    levelNumber: number;
    title: string;
    sourceSlug: string;
    sourceName: string;
  }[];
};

export type SessionView = {
  sessionId: string;
  module: { slug: string; name: string };
  source: { slug: string; name: string };
  level: { levelId: string; segmentKey: string; levelNumber: number; title: string };
  questionCount: number;
  passThreshold: number;
  answeredCount: number;
  correctSoFar: number;
  currentQuestion: QuestionView | null;
  result: SessionResultView | null;
};

export type AnswerView = {
  isCorrect: boolean;
  correctAnswerKey: string;
  answeredCount: number;
  correctSoFar: number;
  nextQuestion: QuestionView | null;
  result: SessionResultView | null;
};

export type IntroQuiz = {
  key: "a1" | "a2" | "b" | string;
  levelId: string;
  title: string;
  status: LevelStatus;
  frequenciesHz: number[];
};

export type IntroResponse = {
  module: { slug: string; name: string };
  source: { slug: string; name: string };
  audio: { assetId: string; url: string; durationMs: number; mimeType: string };
  q: number;
  quizzes: IntroQuiz[];
  boost1Status: LevelStatus;
  boost2Status: LevelStatus;
};

export type PracticeResponse = {
  module: { slug: string; name: string };
  source: { slug: string; name: string };
  mode: "eqBand" | "compressionVariants";
  audio: { assetId: string; url: string; durationMs: number; mimeType: string } | null;
  frequenciesHz: number[] | null;
  gainsDb: number[] | null;
  q: number | null;
  variants: { variantSlug: string; label: string; url: string; durationMs: number; mimeType: string }[] | null;
};

export type PreviewResponse = {
  module: { slug: string; name: string };
  source: { slug: string; name: string };
  level: { levelId: string; segmentKey: string; levelNumber: number; title: string };
  mode: "eqBand" | "compressionVariants";
  audio: { assetId: string; url: string; durationMs: number; mimeType: string } | null;
  frequenciesHz: number[] | null;
  gainsDb: number[] | null;
  q: number | null;
  variants: { variantSlug: string; label: string; url: string; durationMs: number; mimeType: string }[] | null;
};

export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
};
