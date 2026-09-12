using CriticalListeningLab.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CriticalListeningLab.Api.Features.Modules;

[ApiController]
[Route("api/modules")]
[Authorize(AuthPolicies.RequireStudent)]
public class ModulesController(ICatalogService catalog, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public Task<ModuleListResponse> List(CancellationToken ct) =>
        catalog.ListModulesAsync(currentUser.Id, ct);

    [HttpGet("{moduleSlug}")]
    public Task<ModuleListItem> Get(string moduleSlug, CancellationToken ct) =>
        catalog.GetModuleAsync(currentUser.Id, moduleSlug, ct);

    [HttpGet("{moduleSlug}/sources")]
    public Task<SourceListResponse> Sources(string moduleSlug, CancellationToken ct) =>
        catalog.ListSourcesAsync(currentUser.Id, moduleSlug, ct);

    [HttpGet("{moduleSlug}/sources/{sourceSlug}/tree")]
    public Task<TreeResponse> Tree(string moduleSlug, string sourceSlug, CancellationToken ct) =>
        catalog.GetTreeAsync(currentUser.Id, moduleSlug, sourceSlug, ct);

    [HttpGet("{moduleSlug}/sources/{sourceSlug}/practice")]
    public Task<PracticeResponse> Practice(string moduleSlug, string sourceSlug, CancellationToken ct) =>
        catalog.GetPracticeAsync(currentUser.Id, moduleSlug, sourceSlug, ct);
}
