using CriticalListeningLab.Api.Features.Modules;

namespace CriticalListeningLab.Api.Features.Admin;

public sealed record AdminModuleListResponse(IReadOnlyList<AdminModuleItem> Modules);

public sealed record AdminModuleItem(
    string Slug,
    string Name,
    bool IsEnabledGlobally,
    IReadOnlyList<AdminCohortOverride> CohortOverrides);

public sealed record AdminCohortOverride(Guid CohortId, string CohortName, bool IsEnabled);

public sealed record UpdateModuleRequest(bool IsEnabledGlobally);

public sealed record UpdateModuleResponse(string Slug, bool IsEnabledGlobally, int AffectedStudentCount);

public sealed record CohortListResponse(IReadOnlyList<AdminCohortItem> Cohorts);

public sealed record AdminCohortItem(Guid Id, string Name, bool IsActive, int StudentCount);

public sealed record CreateCohortRequest(string Name, bool IsActive);

public sealed record UpdateCohortRequest(string Name, bool IsActive);

public sealed record UpdateCohortModuleRequest(bool? IsEnabled);

public sealed record StudentListResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<AdminStudentItem> Students);

public sealed record AdminStudentItem(
    Guid UserId,
    string Email,
    string DisplayName,
    Guid? CohortId,
    string? CohortName,
    DateTimeOffset? LastLoginAt,
    int CompletedLevelCount,
    int TotalLevelCount);

public sealed record StudentProgressResponse(
    AdminStudentRef Student,
    IReadOnlyList<StudentModuleProgress> Modules);

public sealed record AdminStudentRef(Guid UserId, string Email, string DisplayName, Guid? CohortId, string? CohortName);

public sealed record UpdateStudentRequest(Guid? CohortId);

public sealed record StudentModuleProgress(
    string Slug,
    string Name,
    IReadOnlyList<StudentSourceProgress> Sources);

public sealed record StudentSourceProgress(
    string Slug,
    string Name,
    IReadOnlyList<TreeSegment> Segments);
