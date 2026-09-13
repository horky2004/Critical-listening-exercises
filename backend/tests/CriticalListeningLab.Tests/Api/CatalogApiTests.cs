using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Data.Seed;
using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Domain.Entities;
using CriticalListeningLab.Api.Features.Modules;
using CriticalListeningLab.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CriticalListeningLab.Tests.Api;

public class CatalogApiTests
{
    [Fact]
    public async Task Source_from_another_module_returns_404()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/modules/compression/sources/pink-noise/tree");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadAsStringAsync();
        problem.ShouldContain("https://cll.algebra.hr/errors/");
    }

    [Fact]
    public async Task Practice_works_without_unlocked_levels_and_writes_nothing()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var eq = await client.GetAsync("/api/modules/eq/sources/drums/practice");
        eq.StatusCode.ShouldBe(HttpStatusCode.OK);
        var eqBody = await eq.Content.ReadFromJsonAsync<PracticeResponse>(ApiJson.Options);
        eqBody.ShouldNotBeNull();
        eqBody.Mode.ShouldBe("eqBand");
        eqBody.FrequenciesHz.ShouldBe([125, 250, 500, 1000, 2000, 4000, 8000]);
        eqBody.GainsDb.ShouldBe([12, 9, 6, 3, -3, -6, -9, -12]);
        eqBody.Audio.ShouldNotBeNull();

        var compression = await client.GetAsync("/api/modules/compression/sources/drums/practice");
        compression.StatusCode.ShouldBe(HttpStatusCode.OK);
        var raw = await compression.Content.ReadAsStringAsync();
        raw.ShouldNotContain("storageKey");
        var compressionBody = JsonSerializer.Deserialize<PracticeResponse>(raw, ApiJson.Options);
        compressionBody.ShouldNotBeNull();
        compressionBody.Mode.ShouldBe("compressionVariants");
        compressionBody.Variants.ShouldNotBeNull();
        compressionBody.Variants.Select(v => v.VariantSlug).ToArray()
            .ShouldBe(CatalogSeeder.CompressionVariantOrder);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.TestSessions.CountAsync()).ShouldBe(0);
        (await db.StudentProgress.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Practice_is_forbidden_when_module_is_disabled_for_cohort()
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

        var response = await client.GetAsync("/api/modules/eq/sources/drums/practice");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Tree_hides_intro_and_keeps_classic_level_count()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var tree = await client.GetFromJsonAsync<TreeResponse>(
            "/api/modules/eq/sources/drums/tree", ApiJson.Options);
        tree.ShouldNotBeNull();
        tree.Segments.Select(s => s.Key).ShouldBe(["boost", "cut", "combined"]);
        tree.Segments.SelectMany(s => s.Levels).Count().ShouldBe(13);
        tree.Segments.Single(s => s.Key == "boost").Levels.Single(l => l.LevelNumber == 1)
            .Status.ShouldBe(LevelStatus.Locked);

        var sources = await client.GetFromJsonAsync<SourceListResponse>(
            "/api/modules/eq/sources", ApiJson.Options);
        sources.ShouldNotBeNull();
        sources.Sources.ShouldAllBe(s => s.LevelCount == 13);
    }

    [Fact]
    public async Task Intro_returns_three_quizzes_and_compression_is_not_found()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var intro = await client.GetFromJsonAsync<IntroResponse>(
            "/api/modules/eq/sources/drums/intro", ApiJson.Options);
        intro.ShouldNotBeNull();
        intro.Q.ShouldBe(1);
        intro.Quizzes.Select(q => q.Key).ShouldBe(["a1", "a2", "b"]);
        intro.Quizzes[0].FrequenciesHz.ShouldBe([125, 500]);
        intro.Quizzes[1].FrequenciesHz.ShouldBe([2000, 8000]);
        intro.Quizzes[2].FrequenciesHz.ShouldBe([250, 1000, 4000]);
        intro.Quizzes.ShouldAllBe(q => q.Status == LevelStatus.Unlocked || q.Status == LevelStatus.Locked);
        intro.Quizzes[0].Status.ShouldBe(LevelStatus.Unlocked);
        intro.Quizzes[1].Status.ShouldBe(LevelStatus.Locked);
        intro.Quizzes[2].Status.ShouldBe(LevelStatus.Locked);
        intro.Boost1Status.ShouldBe(LevelStatus.Locked);

        var compression = await client.GetAsync("/api/modules/compression/sources/drums/intro");
        compression.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Level_preview_returns_only_that_levels_bands()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await ProgressFixtures.CompleteIntroAForCurrentStudentAsync(factory.Services, client);

        var response = await client.GetAsync(
            $"/api/modules/eq/sources/drums/levels/{SeedIds.Level("eq", "boost", 1)}/preview");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PreviewResponse>(ApiJson.Options);
        body.ShouldNotBeNull();
        body.Mode.ShouldBe("eqBand");
        body.FrequenciesHz.ShouldBe([125, 500, 2000, 8000]);
        body.GainsDb.ShouldBe([12]);
        body.Level.Title.ShouldBe("Boost +12 dB");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.TestSessions.CountAsync()).ShouldBe(0);
        (await db.StudentProgress.CountAsync(p => p.ExerciseLevel.Segment.Key != CatalogSeeder.IntroSegmentKey))
            .ShouldBe(0);
    }

    [Fact]
    public async Task Locked_level_preview_returns_403()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/modules/eq/sources/drums/levels/{SeedIds.Level("eq", "combined", 1)}/preview");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
