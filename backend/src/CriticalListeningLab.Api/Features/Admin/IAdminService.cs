namespace CriticalListeningLab.Api.Features.Admin;

public interface IAdminService
{
    Task<AdminModuleListResponse> ListModulesAsync(CancellationToken ct);

    Task<UpdateModuleResponse> UpdateModuleAsync(string moduleSlug, bool isEnabledGlobally, CancellationToken ct);

    Task<CohortListResponse> ListCohortsAsync(CancellationToken ct);

    Task<AdminCohortItem> CreateCohortAsync(string name, bool isActive, CancellationToken ct);

    Task<AdminCohortItem> UpdateCohortAsync(Guid cohortId, string name, bool isActive, CancellationToken ct);

    Task UpdateCohortModuleAsync(Guid cohortId, string moduleSlug, bool? isEnabled, CancellationToken ct);

    Task<StudentListResponse> ListStudentsAsync(
        Guid? cohortId, string? search, int page, int pageSize, CancellationToken ct);

    Task<StudentProgressResponse> GetStudentProgressAsync(Guid userId, CancellationToken ct);
}
