using CriticalListeningLab.Api.Domain.Questions;

namespace CriticalListeningLab.Api.Features.TestSessions;

public sealed record SessionView(
    Guid SessionId,
    ModuleRef Module,
    SourceRef Source,
    LevelRef Level,
    int QuestionCount,
    int PassThreshold,
    int AnsweredCount,
    int CorrectSoFar,
    QuestionView? CurrentQuestion,
    SessionResultView? Result,
    IReadOnlyList<int>? FrequenciesHz);

public sealed record ModuleRef(string Slug, string Name);
public sealed record SourceRef(string Slug, string Name);
public sealed record LevelRef(Guid LevelId, string SegmentKey, int LevelNumber, string Title);

public sealed record QuestionView(
    int QuestionIndex,
    int QuestionCount,
    string Prompt,
    IReadOnlyList<AnswerOption> AnswerOptions,
    AudioView Audio,
    EqBandPrompt? Eq);

public sealed record AudioView(string Url, bool Loop);

public sealed record AnswerView(
    bool IsCorrect,
    string CorrectAnswerKey,
    int AnsweredCount,
    int CorrectSoFar,
    QuestionView? NextQuestion,
    SessionResultView? Result);

public sealed record SessionResultView(
    int CorrectAnswers,
    int QuestionCount,
    double ScorePercentage,
    bool Passed,
    bool IsFirstPass,
    IReadOnlyList<UnlockedLevelView> NewlyUnlockedLevels);

public sealed record UnlockedLevelView(
    Guid LevelId,
    string SegmentKey,
    int LevelNumber,
    string Title,
    string SourceSlug,
    string SourceName);

public sealed record QuestionPromptJson(
    string Prompt,
    IReadOnlyList<AnswerOption> AnswerOptions,
    EqBandPrompt? Eq);
