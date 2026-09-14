using CriticalListeningLab.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CriticalListeningLab.Api.Features.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(AuthPolicies.RequireAdmin)]
public class AdminController(IAdminService admin) : ControllerBase
{
    [HttpGet("modules")]
    public Task<AdminModuleListResponse> Modules(CancellationToken ct) =>
        admin.ListModulesAsync(ct);

    [HttpPut("modules/{moduleSlug}")]
    public Task<UpdateModuleResponse> UpdateModule(
        string moduleSlug, UpdateModuleRequest request, CancellationToken ct) =>
        admin.UpdateModuleAsync(moduleSlug, request.IsEnabledGlobally, ct);

    [HttpGet("cohorts")]
    public Task<CohortListResponse> Cohorts(CancellationToken ct) =>
        admin.ListCohortsAsync(ct);

    [HttpPost("cohorts")]
    public async Task<ActionResult<AdminCohortItem>> CreateCohort(
        CreateCohortRequest request, CancellationToken ct)
    {
        var created = await admin.CreateCohortAsync(request.Name, request.IsActive, ct);
        return Created($"/api/admin/cohorts/{created.Id}", created);
    }

    [HttpPut("cohorts/{cohortId:guid}")]
    public Task<AdminCohortItem> UpdateCohort(
        Guid cohortId, UpdateCohortRequest request, CancellationToken ct) =>
        admin.UpdateCohortAsync(cohortId, request.Name, request.IsActive, ct);

    [HttpPut("cohorts/{cohortId:guid}/modules/{moduleSlug}")]
    public async Task<IActionResult> UpdateCohortModule(
        Guid cohortId, string moduleSlug, UpdateCohortModuleRequest request, CancellationToken ct)
    {
        await admin.UpdateCohortModuleAsync(cohortId, moduleSlug, request.IsEnabled, ct);
        return NoContent();
    }

    [HttpGet("students")]
    public Task<StudentListResponse> Students(
        [FromQuery] Guid? cohortId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default) =>
        admin.ListStudentsAsync(cohortId, search, page, pageSize, ct);

    [HttpGet("students/{userId:guid}/progress")]
    public Task<StudentProgressResponse> StudentProgress(Guid userId, CancellationToken ct) =>
        admin.GetStudentProgressAsync(userId, ct);

    [HttpPut("students/{userId:guid}")]
    public Task<AdminStudentRef> UpdateStudent(
        Guid userId, UpdateStudentRequest request, CancellationToken ct) =>
        admin.UpdateStudentAsync(userId, request.CohortId, ct);
}
