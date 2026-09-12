namespace CriticalListeningLab.Api.Features.Modules;

public interface ICatalogService
{
    Task<ModuleListResponse> ListModulesAsync(Guid userId, CancellationToken ct);

    Task<ModuleListItem> GetModuleAsync(Guid userId, string moduleSlug, CancellationToken ct);

    Task<SourceListResponse> ListSourcesAsync(Guid userId, string moduleSlug, CancellationToken ct);

    Task<TreeResponse> GetTreeAsync(
        Guid userId, string moduleSlug, string sourceSlug, CancellationToken ct);

    Task<PracticeResponse> GetPracticeAsync(
        Guid userId, string moduleSlug, string sourceSlug, CancellationToken ct);
}
