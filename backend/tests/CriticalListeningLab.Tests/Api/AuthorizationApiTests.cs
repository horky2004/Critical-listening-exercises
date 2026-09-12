using System.Net;
using System.Net.Http.Json;
using CriticalListeningLab.Api.Auth;
using CriticalListeningLab.Api.Domain;

namespace CriticalListeningLab.Tests.Api;

public class AuthorizationApiTests
{
    public static TheoryData<HttpMethod, string> ProtectedRoutes => new()
    {
        { HttpMethod.Get, "/api/me" },
        { HttpMethod.Get, "/api/modules" },
        { HttpMethod.Get, "/api/modules/eq" },
        { HttpMethod.Get, "/api/modules/eq/sources" },
        { HttpMethod.Get, "/api/modules/eq/sources/drums/tree" },
        { HttpMethod.Get, "/api/modules/eq/sources/drums/practice" },
        { HttpMethod.Get, $"/api/modules/eq/sources/drums/levels/{Guid.CreateVersion7()}/preview" },
        { HttpMethod.Post, "/api/test-sessions" },
        { HttpMethod.Get, $"/api/test-sessions/{Guid.CreateVersion7()}" },
        { HttpMethod.Post, $"/api/test-sessions/{Guid.CreateVersion7()}/questions/1/answer" },
        { HttpMethod.Post, $"/api/test-sessions/{Guid.CreateVersion7()}/abandon" },
        { HttpMethod.Get, $"/api/audio/assets/{Guid.CreateVersion7()}" },
        { HttpMethod.Get, $"/api/audio/questions/{Guid.CreateVersion7()}" },
        { HttpMethod.Get, "/api/admin/modules" },
        { HttpMethod.Put, "/api/admin/modules/eq" },
        { HttpMethod.Get, "/api/admin/cohorts" },
        { HttpMethod.Post, "/api/admin/cohorts" },
        { HttpMethod.Put, $"/api/admin/cohorts/{Guid.CreateVersion7()}" },
        { HttpMethod.Put, $"/api/admin/cohorts/{Guid.CreateVersion7()}/modules/eq" },
        { HttpMethod.Get, "/api/admin/students" },
        { HttpMethod.Get, $"/api/admin/students/{Guid.CreateVersion7()}/progress" }
    };

    [Theory]
    [MemberData(nameof(ProtectedRoutes))]
    public async Task Unauthenticated_request_returns_401(HttpMethod method, string url)
    {
        using var factory = new ApiFactory(useDevBypass: false);
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(method, url);
        if (method == HttpMethod.Post || method == HttpMethod.Put)
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Disallowed_email_on_modules_returns_403()
    {
        using var factory = new ApiFactory(configureAuth: options =>
        {
            options.AllowedEmailDomains = ["algebra.hr", "student.algebra.hr"];
            options.DevUser.Email = "hacker@example.com";
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/modules");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Student_cannot_call_admin_routes()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/modules");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_can_list_modules()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(DevAuthenticationHandler.RoleOverrideHeader, nameof(UserRole.Admin));

        var response = await client.GetAsync("/api/admin/modules");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
