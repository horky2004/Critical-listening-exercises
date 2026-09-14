using System.Net;
using System.Net.Http.Json;
using CriticalListeningLab.Api.Auth;
using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Features.Admin;
using CriticalListeningLab.Api.Features.Me;

namespace CriticalListeningLab.Tests.Api;

public class AdminApiTests
{
    [Fact]
    public async Task Admin_can_manage_modules_cohorts_and_students()
    {
        using var factory = new ApiFactory();
        using var studentClient = factory.CreateClient();
        using var adminClient = factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add(DevAuthenticationHandler.RoleOverrideHeader, nameof(UserRole.Admin));

        (await studentClient.GetAsync("/api/me")).EnsureSuccessStatusCode();
        var student = await (await studentClient.GetAsync("/api/me")).Content
            .ReadFromJsonAsync<MeResponse>(ApiJson.Options);
        student.ShouldNotBeNull();

        var modules = await (await adminClient.GetAsync("/api/admin/modules")).Content
            .ReadFromJsonAsync<AdminModuleListResponse>(ApiJson.Options);
        modules.ShouldNotBeNull();
        modules.Modules.Count.ShouldBe(2);
        modules.Modules.ShouldAllBe(m => m.IsEnabledGlobally);

        var disabled = await adminClient.PutAsJsonAsync("/api/admin/modules/eq", new UpdateModuleRequest(false));
        disabled.StatusCode.ShouldBe(HttpStatusCode.OK);
        var disableBody = await disabled.Content.ReadFromJsonAsync<UpdateModuleResponse>(ApiJson.Options);
        disableBody.ShouldNotBeNull();
        disableBody.IsEnabledGlobally.ShouldBeFalse();
        disableBody.AffectedStudentCount.ShouldBeGreaterThan(0);

        var created = await adminClient.PostAsJsonAsync(
            "/api/admin/cohorts", new CreateCohortRequest("2026/27", false));
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var cohort = await created.Content.ReadFromJsonAsync<AdminCohortItem>(ApiJson.Options);
        cohort.ShouldNotBeNull();
        cohort.IsActive.ShouldBeFalse();

        var overrideResponse = await adminClient.PutAsJsonAsync(
            $"/api/admin/cohorts/{cohort.Id}/modules/eq", new UpdateCohortModuleRequest(true));
        overrideResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var students = await (await adminClient.GetAsync("/api/admin/students?search=Dev")).Content
            .ReadFromJsonAsync<StudentListResponse>(ApiJson.Options);
        students.ShouldNotBeNull();
        students.TotalCount.ShouldBe(1);
        students.Students[0].DisplayName.ShouldBe("Dev Student");

        var assigned = await adminClient.PutAsJsonAsync(
            $"/api/admin/students/{student.Id}", new UpdateStudentRequest(cohort.Id));
        assigned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var assignedBody = await assigned.Content.ReadFromJsonAsync<AdminStudentRef>(ApiJson.Options);
        assignedBody.ShouldNotBeNull();
        assignedBody.CohortId.ShouldBe(cohort.Id);

        var progress = await adminClient.GetAsync($"/api/admin/students/{student.Id}/progress");
        progress.StatusCode.ShouldBe(HttpStatusCode.OK);
        var progressBody = await progress.Content.ReadFromJsonAsync<StudentProgressResponse>(ApiJson.Options);
        progressBody.ShouldNotBeNull();
        progressBody.Student.CohortName.ShouldBe("2026/27");
        progressBody.Modules.Count.ShouldBe(2);
        progressBody.Modules.ShouldAllBe(m => m.Sources.Count > 0);
    }

    [Fact]
    public async Task Duplicate_cohort_name_returns_409()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(DevAuthenticationHandler.RoleOverrideHeader, nameof(UserRole.Admin));

        var first = await client.PostAsJsonAsync("/api/admin/cohorts", new CreateCohortRequest("2026/27", false));
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        var duplicate = await client.PostAsJsonAsync("/api/admin/cohorts", new CreateCohortRequest("2026/27", false));
        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
