using System.Net;
using System.Net.Http.Json;
using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Data.Seed;
using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CriticalListeningLab.Tests.Api;

public class AudioApiTests
{
    [Fact]
    public async Task Eq_asset_is_cached_and_supports_range()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var assetId = SeedIds.Asset("eq", "drums", "full");
        var response = await client.GetAsync($"/api/audio/assets/{assetId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("audio/flac");
        response.Headers.CacheControl.ShouldNotBeNull();
        response.Headers.CacheControl!.Private.ShouldBeTrue();
        response.Headers.CacheControl.MaxAge.ShouldBe(TimeSpan.FromSeconds(604800));
        response.Headers.ETag.ShouldNotBeNull();
        (await response.Content.ReadAsByteArrayAsync()).ShouldBe("CLL-TEST-AUDIO"u8.ToArray());

        var range = new HttpRequestMessage(HttpMethod.Get, $"/api/audio/assets/{assetId}");
        range.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 3);
        var partial = await client.SendAsync(range);
        partial.StatusCode.ShouldBe(HttpStatusCode.PartialContent);
        (await partial.Content.ReadAsByteArrayAsync()).ShouldBe("CLL-"u8.ToArray());
    }

    [Fact]
    public async Task Question_audio_is_not_cached_and_hides_the_variant()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var start = await client.PostAsJsonAsync("/api/test-sessions", new
        {
            moduleSlug = "compression",
            sourceSlug = "drums",
            levelId = SeedIds.Level("compression", "detection", 1)
        });
        start.EnsureSuccessStatusCode();
        var json = await start.Content.ReadAsStringAsync();
        json.ShouldContain("/api/audio/questions/");

        var tokenStart = json.IndexOf("/api/audio/questions/", StringComparison.Ordinal)
                         + "/api/audio/questions/".Length;
        var token = json[tokenStart..(tokenStart + 36)];

        var response = await client.GetAsync($"/api/audio/questions/{token}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("audio/mpeg");
        response.Headers.CacheControl.ShouldNotBeNull();
        response.Headers.CacheControl!.NoStore.ShouldBeTrue();
        (await response.Content.ReadAsByteArrayAsync()).ShouldBe("CLL-TEST-AUDIO"u8.ToArray());
    }

    [Fact]
    public async Task Asset_from_disabled_module_is_forbidden()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        (await client.GetAsync("/api/me")).EnsureSuccessStatusCode();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.Role == UserRole.Student);
            db.CohortModuleAvailabilities.Add(new CohortModuleAvailability
            {
                Id = Guid.CreateVersion7(),
                CohortId = user.CohortId!.Value,
                ModuleId = SeedIds.Module("eq"),
                IsEnabled = false
            });
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/audio/assets/{SeedIds.Asset("eq", "drums", "full")}");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
