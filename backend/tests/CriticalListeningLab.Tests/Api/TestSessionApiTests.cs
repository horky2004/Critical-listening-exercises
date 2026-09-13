using System.Net;
using System.Net.Http.Json;
using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Data.Seed;
using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Domain.Entities;
using CriticalListeningLab.Api.Features.Progress;
using CriticalListeningLab.Api.Features.TestSessions;
using CriticalListeningLab.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CriticalListeningLab.Tests.Api;

public class TestSessionApiTests
{
    [Fact]
    public async Task Locked_level_returns_403_and_does_not_create_a_session()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        await ProgressFixtures.CompleteFrequencyIntroForCurrentStudentAsync(factory.Services, client);
        var response = await client.PostAsJsonAsync("/api/test-sessions", new
        {
            moduleSlug = "eq",
            sourceSlug = "drums",
            levelId = SeedIds.Level("eq", "combined", 1)
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).ShouldContain("level-locked");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.TestSessions.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Disabled_module_for_cohort_cannot_start_a_session()
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
                ModuleId = SeedIds.Module("compression"),
                IsEnabled = false
            });
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/api/test-sessions", new
        {
            moduleSlug = "compression",
            sourceSlug = "drums",
            levelId = SeedIds.Level("compression", "detection", 1)
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Another_students_session_returns_403()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        (await client.GetAsync("/api/me")).EnsureSuccessStatusCode();

        Guid foreignSession;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var other = new User
            {
                Id = Guid.CreateVersion7(),
                EntraObjectId = Guid.NewGuid().ToString(),
                Email = "druga@student.algebra.hr",
                DisplayName = "Druga",
                Role = UserRole.Student,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Users.Add(other);
            await db.SaveChangesAsync();

            var progression = scope.ServiceProvider.GetRequiredService<IProgressionService>();
            await ProgressFixtures.CompleteIntroAAsync(progression, other.Id);

            var sessions = scope.ServiceProvider.GetRequiredService<ITestSessionService>();
            var created = await sessions.StartAsync(
                other.Id, "eq", "drums", SeedIds.Level("eq", "boost", 1), CancellationToken.None);
            foreignSession = created.SessionId;
        }

        var response = await client.GetAsync($"/api/test-sessions/{foreignSession}");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Answers_must_be_in_order_and_valid()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var session = await StartEqAsync(factory, client);

        var skip = await client.PostAsJsonAsync(
            $"/api/test-sessions/{session.SessionId}/questions/5/answer",
            new { answerKey = "125" });
        skip.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var unknown = await client.PostAsJsonAsync(
            $"/api/test-sessions/{session.SessionId}/questions/1/answer",
            new { answerKey = "999" });
        unknown.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var first = await db.TestSessionQuestions.SingleAsync(
            q => q.TestSessionId == session.SessionId && q.QuestionIndex == 1);

        var firstAnswer = await client.PostAsJsonAsync(
            $"/api/test-sessions/{session.SessionId}/questions/1/answer",
            new { answerKey = first.CorrectAnswerKey });
        firstAnswer.StatusCode.ShouldBe(HttpStatusCode.OK);

        var again = await client.PostAsJsonAsync(
            $"/api/test-sessions/{session.SessionId}/questions/1/answer",
            new { answerKey = first.CorrectAnswerKey });
        again.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        using var verify = factory.Services.CreateScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        (await verifyDb.TestSessionQuestions.AsNoTracking().SingleAsync(q => q.Id == first.Id))
            .StudentAnswerKey.ShouldBe(first.CorrectAnswerKey);
    }

    [Fact]
    public async Task Start_json_hides_correctAnswerKey_and_storageKey()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await ProgressFixtures.CompleteIntroAForCurrentStudentAsync(factory.Services, client);
        var eq = await client.PostAsJsonAsync("/api/test-sessions", new
        {
            moduleSlug = "eq",
            sourceSlug = "drums",
            levelId = SeedIds.Level("eq", "boost", 1)
        });
        eq.StatusCode.ShouldBe(HttpStatusCode.Created);
        var eqJson = await eq.Content.ReadAsStringAsync();
        eqJson.ShouldNotContain("correctAnswerKey");
        eqJson.ShouldNotContain("storageKey");

        var compression = await client.PostAsJsonAsync("/api/test-sessions", new
        {
            moduleSlug = "compression",
            sourceSlug = "drums",
            levelId = SeedIds.Level("compression", "detection", 1)
        });
        compression.StatusCode.ShouldBe(HttpStatusCode.Created);
        var compressionJson = await compression.Content.ReadAsStringAsync();
        compressionJson.ShouldNotContain("correctAnswerKey");
        compressionJson.ShouldNotContain("storageKey");
        compressionJson.ShouldNotContain(".mp3");
        compressionJson.ShouldContain("/api/audio/questions/");
        compressionJson.ShouldNotContain("/api/audio/assets/");
    }

    [Fact]
    public async Task Full_pass_unlocks_the_next_level_and_updates_the_tree()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var session = await StartEqAsync(factory, client);

        AnswerView? last = null;
        for (var i = 1; i <= CatalogSeeder.QuestionCount; i++)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var question = await db.TestSessionQuestions.AsNoTracking()
                .SingleAsync(q => q.TestSessionId == session.SessionId && q.QuestionIndex == i);
            var response = await client.PostAsJsonAsync(
                $"/api/test-sessions/{session.SessionId}/questions/{i}/answer",
                new { answerKey = question.CorrectAnswerKey });
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            last = await response.Content.ReadFromJsonAsync<AnswerView>(ApiJson.Options);
        }

        last.ShouldNotBeNull();
        last.Result.ShouldNotBeNull();
        last.Result.Passed.ShouldBeTrue();
        last.Result.IsFirstPass.ShouldBeTrue();
        last.Result.NewlyUnlockedLevels.Select(l => l.LevelId)
            .ShouldBe([SeedIds.Level("eq", CatalogSeeder.IntroSegmentKey, 3)]);

        var tree = await client.GetFromJsonAsync<TreeDocument>(
            "/api/modules/eq/sources/drums/tree", ApiJson.Options);
        tree.ShouldNotBeNull();
        tree.Segments.Select(s => s.Key).ShouldNotContain(CatalogSeeder.IntroSegmentKey);
        var boost = tree.Segments.Single(s => s.Key == "boost").Levels;
        boost.Single(l => l.LevelNumber == 1).Status.ShouldBe("Completed");
        boost.Single(l => l.LevelNumber == 2).Status.ShouldBe("Locked");
        boost.Single(l => l.LevelNumber == 3).Status.ShouldBe("Locked");
    }

    [Fact]
    public async Task Get_resumes_at_the_first_unanswered_question()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var session = await StartEqAsync(factory, client);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var questions = await db.TestSessionQuestions
                .Where(q => q.TestSessionId == session.SessionId && q.QuestionIndex <= 3)
                .ToListAsync();
            foreach (var question in questions.OrderBy(q => q.QuestionIndex))
            {
                (await client.PostAsJsonAsync(
                    $"/api/test-sessions/{session.SessionId}/questions/{question.QuestionIndex}/answer",
                    new { answerKey = question.CorrectAnswerKey })).EnsureSuccessStatusCode();
            }
        }

        var resumed = await client.GetFromJsonAsync<SessionView>(
            $"/api/test-sessions/{session.SessionId}", ApiJson.Options);
        resumed.ShouldNotBeNull();
        resumed.AnsweredCount.ShouldBe(3);
        resumed.CorrectSoFar.ShouldBe(3);
        resumed.CurrentQuestion.ShouldNotBeNull();
        resumed.CurrentQuestion.QuestionIndex.ShouldBe(4);
        var raw = await (await client.GetAsync($"/api/test-sessions/{session.SessionId}")).Content.ReadAsStringAsync();
        raw.ShouldNotContain("correctAnswerKey");
    }

    [Fact]
    public async Task Starting_again_abandons_the_previous_session()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var first = await StartEqAsync(factory, client);
        var second = await StartEqAsync(factory, client);

        first.SessionId.ShouldNotBe(second.SessionId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.TestSessions.SingleAsync(s => s.Id == first.SessionId)).Status
            .ShouldBe(TestSessionStatus.Abandoned);
        (await db.TestSessions.SingleAsync(s => s.Id == second.SessionId)).Status
            .ShouldBe(TestSessionStatus.InProgress);
    }

    private static async Task<SessionView> StartEqAsync(ApiFactory factory, HttpClient client)
    {
        await ProgressFixtures.CompleteIntroAForCurrentStudentAsync(factory.Services, client);
        var response = await client.PostAsJsonAsync("/api/test-sessions", new
        {
            moduleSlug = "eq",
            sourceSlug = "drums",
            levelId = SeedIds.Level("eq", "boost", 1)
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var session = await response.Content.ReadFromJsonAsync<SessionView>(ApiJson.Options);
        session.ShouldNotBeNull();
        return session;
    }

    private sealed record TreeDocument(IReadOnlyList<TreeSegmentDoc> Segments);
    private sealed record TreeSegmentDoc(string Key, IReadOnlyList<TreeLevelDoc> Levels);
    private sealed record TreeLevelDoc(int LevelNumber, string Status);
}
