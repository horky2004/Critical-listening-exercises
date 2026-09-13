using CriticalListeningLab.Api.Domain;

namespace CriticalListeningLab.Api.Features.Modules;

public sealed record ModuleListResponse(IReadOnlyList<ModuleListItem> Modules);

public sealed record ModuleListItem(
    string Slug,
    string Name,
    string Description,
    bool IsAvailable,
    string? UnavailableReason,
    int SourceCount,
    int LevelCount,
    int CompletedLevelCount,
    int TotalLevelCount);

public sealed record SourceListResponse(ModuleRef Module, IReadOnlyList<SourceListItem> Sources);

public sealed record SourceListItem(
    string Slug,
    string Name,
    int CompletedLevelCount,
    int LevelCount,
    bool HasPracticeMode,
    bool IsAvailable,
    string? UnavailableReason);

public sealed record TreeResponse(
    ModuleRef Module,
    SourceRef Source,
    IReadOnlyList<TreeSegment> Segments);

public sealed record TreeSegment(string Key, string Name, IReadOnlyList<TreeLevel> Levels);

public sealed record TreeLevel(
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

public sealed record PracticeResponse(
    ModuleRef Module,
    SourceRef Source,
    string Mode,
    PracticeAudio? Audio,
    IReadOnlyList<int>? FrequenciesHz,
    IReadOnlyList<int>? GainsDb,
    double? Q,
    IReadOnlyList<PracticeVariant>? Variants);

public sealed record PracticeAudio(Guid AssetId, string Url, int DurationMs, string MimeType);

public sealed record PracticeVariant(
    string VariantSlug,
    string Label,
    string Url,
    int DurationMs,
    string MimeType);

public sealed record PreviewResponse(
    ModuleRef Module,
    SourceRef Source,
    LevelPreviewRef Level,
    string Mode,
    PracticeAudio? Audio,
    IReadOnlyList<int>? FrequenciesHz,
    IReadOnlyList<int>? GainsDb,
    double? Q,
    IReadOnlyList<PracticeVariant>? Variants);

public sealed record LevelPreviewRef(Guid LevelId, string SegmentKey, int LevelNumber, string Title);

public sealed record ModuleRef(string Slug, string Name);

public sealed record SourceRef(string Slug, string Name);

public sealed record IntroResponse(
    ModuleRef Module,
    SourceRef Source,
    PracticeAudio Audio,
    double Q,
    IReadOnlyList<IntroQuiz> Quizzes,
    LevelStatus Boost1Status,
    LevelStatus Boost2Status);

public sealed record IntroQuiz(
    string Key,
    Guid LevelId,
    string Title,
    LevelStatus Status,
    IReadOnlyList<int> FrequenciesHz);
