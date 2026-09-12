namespace CriticalListeningLab.Api.Domain.Progression;

public sealed record SourceTreeState(
    Guid AudioSourceId,
    string AudioSourceSlug,
    IReadOnlyList<SegmentState> Segments);

public sealed record SegmentState(
    string Key,
    string Name,
    int SortOrder,
    IReadOnlyList<LevelState> Levels);

public sealed record LevelState(
    Guid LevelId,
    int LevelNumber,
    string Title,
    LevelStatus Status,
    int BestScore,
    double BestScorePercentage,
    int QuestionCount,
    int PassThreshold,
    int AttemptCount,
    DateTimeOffset? FirstPassedAt,
    IReadOnlyList<Guid> RequiredLevelIds);

public sealed record ProgressUpdateResult(
    int BestScore,
    bool Passed,
    bool IsFirstPass,
    IReadOnlyList<LevelState> NewlyUnlockedLevels);
