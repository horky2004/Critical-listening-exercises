using System.Net;
using System.Net.Http.Json;
using CriticalListeningLab.Api.Auth;
using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Features.Me;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CriticalListeningLab.Tests.Api;

public class MeEndpointTests
{
    [Fact]
    public async Task Health_is_public()
    {
        using var factory = new ApiFactory(useDevBypass: false);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_does_not_provision_a_user_when_bypass_is_on()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Users.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Me_without_token_returns_401_when_bypass_is_off()
    {
        using var factory = new ApiFactory(useDevBypass: false);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Bypass_with_allowed_domain_creates_student_and_returns_profile()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var me = await response.Content.ReadFromJsonAsync<MeResponse>();
        me.ShouldNotBeNull();
        me.Email.ShouldBe("dev.student@student.algebra.hr");
        me.Role.ShouldBe(nameof(UserRole.Student));
        me.DisplayName.ShouldBe("Dev Student");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = await db.Users.ToListAsync();
        users.Count.ShouldBe(1);
        users[0].Role.ShouldBe(UserRole.Student);
        users[0].Id.ShouldBe(me.Id);
    }

    [Fact]
    public async Task Bypass_with_disallowed_domain_returns_403_and_does_not_create_user()
    {
        using var factory = new ApiFactory(configureAuth: options =>
        {
            options.AllowedEmailDomains = ["algebra.hr", "student.algebra.hr"];
            options.DevUser.Email = "hacker@example.com";
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Users.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Bypass_admin_header_uses_admin_identity_from_AdminEmails()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(DevAuthenticationHandler.RoleOverrideHeader, nameof(UserRole.Admin));

        var response = await client.GetAsync("/api/me");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var me = await response.Content.ReadFromJsonAsync<MeResponse>();
        me.ShouldNotBeNull();
        me.Email.ShouldBe("dev.admin@algebra.hr");
        me.Role.ShouldBe(nameof(UserRole.Admin));
        me.Cohort.ShouldBeNull();
    }
}
