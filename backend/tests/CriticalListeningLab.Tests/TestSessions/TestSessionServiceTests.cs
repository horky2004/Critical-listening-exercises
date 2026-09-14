using System.Text.Json;
using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Data.Seed;
using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Domain.Entities;
using CriticalListeningLab.Api.Domain.Questions;
using CriticalListeningLab.Api.Features.Modules;
using CriticalListeningLab.Api.Features.Progress;
using CriticalListeningLab.Api.Features.TestSessions;
using CriticalListeningLab.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace CriticalListeningLab.Tests.TestSessions;

public class TestSessionServiceTests
{
    [Fact]
    public async Task Start_creates_twenty_questions_without_correctAnswerKey_in_prompt()
    {
        var (service, db, user) = await CreateAsync();
        var session = await service.StartAsync(
            user, "eq", "drums", SeedIds.Level("eq", "boost", 1), CancellationToken.None);

        session.QuestionCount.ShouldBe(CatalogSeeder.QuestionCount);
        session.PassThreshold.ShouldBe(CatalogSeeder.PassThreshold);
        session.AnsweredCount.ShouldBe(0);
        session.CurrentQuestion.ShouldNotBeNull();
        session.CurrentQuestion.QuestionIndex.ShouldBe(1);
        session.CurrentQuestion.Eq.ShouldNotBeNull();
        session.CurrentQuestion.Audio.Url.ShouldStartWith("/api/audio/assets/");
        session.FrequenciesHz.ShouldBe([125, 500, 2000, 8000]);

        var stored = await db.TestSessionQuestions.ToListAsync();
        stored.Count.ShouldBe(CatalogSeeder.QuestionCount);
        stored.ShouldAllBe(q => q.AudioToken != Guid.Empty);
        foreach (var question in stored)
        {
            question.PromptJson.ShouldNotContain("correctAnswerKey");
            question.PromptJson.ShouldNotContain("CorrectAnswerKey");
        }
    }

    [Fact]
    public async Task Musical_source_skips_intro_and_starts_at_boost_L1()
    {
        var (service, _, user) = await CreateAsync();

        var missing = await Should.ThrowAsync<NotFoundException>(() =>
            service.StartAsync(
                user, "eq", "drums", SeedIds.Level("eq", CatalogSeeder.IntroSegmentKey, 1), CancellationToken.None));
        missing.StatusCode.ShouldBe(404);

        var boost = await service.StartAsync(
            user, "eq", "drums", SeedIds.Level("eq", "boost", 1), CancellationToken.None);
        boost.QuestionCount.ShouldBe(CatalogSeeder.QuestionCount);
    }

    [Fact]
    public async Task Pink_noise_boost_L1_is_locked_until_intro_A_is_finished()
    {
        var (service, _, user) = await CreateAsync(pinkIntroDone: false);

        var error = await Should.ThrowAsync<ForbiddenException>(() =>
            service.StartAsync(user, "eq", "pink-noise", SeedIds.Level("eq", "boost", 1), CancellationToken.None));
        error.StatusCode.ShouldBe(403);

        var intro = await service.StartAsync(
            user, "eq", "pink-noise", SeedIds.Level("eq", CatalogSeeder.IntroSegmentKey, 1), CancellationToken.None);
        intro.QuestionCount.ShouldBe(CatalogSeeder.IntroQuestionCount);
        intro.PassThreshold.ShouldBe(CatalogSeeder.IntroPassThreshold);
    }

    [Fact]
    public async Task Intro_quiz_passes_even_with_zero_correct_answers()
    {
        var (service, db, user) = await CreateAsync(pinkIntroDone: false);
        var session = await service.StartAsync(
            user, "eq", "pink-noise", SeedIds.Level("eq", CatalogSeeder.IntroSegmentKey, 1), CancellationToken.None);

        AnswerView? last = null;
        for (var i = 1; i <= CatalogSeeder.IntroQuestionCount; i++)
        {
            var question = await db.TestSessionQuestions.AsNoTracking()
                .SingleAsync(q => q.TestSessionId == session.SessionId && q.QuestionIndex == i);
            var wrong = question.CorrectAnswerKey == "125" ? "500" : "125";
            last = await service.AnswerAsync(user, session.SessionId, i, wrong, CancellationToken.None);
        }

        last.ShouldNotBeNull();
        last.Result.ShouldNotBeNull();
        last.Result.Passed.ShouldBeTrue();
        last.Result.CorrectAnswers.ShouldBe(0);
        last.Result.NewlyUnlockedLevels.Select(l => l.LevelId)
            .ShouldBe([SeedIds.Level("eq", CatalogSeeder.IntroSegmentKey, 2)]);
        last.Result.NewlyUnlockedLevels.ShouldAllBe(level =>
            level.SourceSlug == "pink-noise" && level.SourceName == "Ružičasti šum");
    }

    [Fact]
    public async Task Intro_B_unlocks_boost_on_pink_noise_and_musical_sources()
    {
        var (service, db, user) = await CreateAsync(pinkIntroDone: false);
        await ProgressFixtures.CompleteThroughPinkBoost1Async(new ProgressionService(db, TimeProvider.System), user);

        var session = await service.StartAsync(
            user, "eq", "pink-noise", SeedIds.Level("eq", CatalogSeeder.IntroSegmentKey, 3), CancellationToken.None);

        AnswerView? last = null;
        for (var i = 1; i <= CatalogSeeder.IntroQuestionCount; i++)
        {
            var question = await db.TestSessionQuestions.AsNoTracking()
                .SingleAsync(q => q.TestSessionId == session.SessionId && q.QuestionIndex == i);
            last = await service.AnswerAsync(
                user, session.SessionId, i, question.CorrectAnswerKey, CancellationToken.None);
        }

        last.ShouldNotBeNull();
        last.Result.ShouldNotBeNull();
        var unlocked = last.Result.NewlyUnlockedLevels
            .Where(level => level.SegmentKey != CatalogSeeder.IntroSegmentKey)
            .ToList();
        unlocked.Select(level => $"{level.SourceSlug}:{level.LevelNumber}")
            .ShouldBe(["pink-noise:2", "drums:1", "acoustic-guitar:1", "vocal:1"]);
        unlocked.ShouldAllBe(level => level.Title == "Boost +12 dB");
        unlocked.Single(level => level.SourceSlug == "acoustic-guitar").SourceName.ShouldBe("Akustična gitara");
    }

    [Fact]
    public async Task Locked_level_cannot_start_a_session()
    {
        var (service, db, user) = await CreateAsync();

        var error = await Should.ThrowAsync<ForbiddenException>(() =>
            service.StartAsync(user, "eq", "drums", SeedIds.Level("eq", "combined", 1), CancellationToken.None));

        error.StatusCode.ShouldBe(403);
        (await db.TestSessions.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Another_users_session_is_forbidden()
    {
        var (service, db, user) = await CreateAsync();
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

        await ProgressFixtures.CompleteIntroAAsync(new ProgressionService(db, TimeProvider.System), other.Id);

        var session = await service.StartAsync(
            other.Id, "eq", "drums", SeedIds.Level("eq", "boost", 1), CancellationToken.None);

        var error = await Should.ThrowAsync<ForbiddenException>(() =>
            service.GetAsync(user, session.SessionId, CancellationToken.None));
        error.StatusCode.ShouldBe(403);
    }

    [Fact]
    public async Task Source_from_another_module_is_not_found()
    {
        var (service, _, user) = await CreateAsync();

        var error = await Should.ThrowAsync<NotFoundException>(() =>
            service.StartAsync(user, "compression", "drums", SeedIds.Level("eq", "boost", 1), CancellationToken.None));

        error.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task Unknown_answer_key_is_rejected()
    {
        var (service, _, user) = await CreateAsync();
        var session = await service.StartAsync(
            user, "eq", "drums", SeedIds.Level("eq", "boost", 1), CancellationToken.None);

        var error = await Should.ThrowAsync<BadRequestException>(() =>
            service.AnswerAsync(user, session.SessionId, 1, "999", CancellationToken.None));

        error.StatusCode.ShouldBe(400);
    }

    [Fact]
    public async Task Out_of_order_answer_is_rejected()
    {
        var (service, _, user) = await CreateAsync();
        var session = await service.StartAsync(
            user, "eq", "drums", SeedIds.Level("eq", "boost", 1), CancellationToken.None);

        var error = await Should.ThrowAsync<ConflictException>(() =>
            service.AnswerAsync(user, session.SessionId, 2, "125", CancellationToken.None));

        error.StatusCode.ShouldBe(409);
    }

    [Fact]
    public async Task Completing_the_test_applies_progress_once()
    {
        var (service, db, user) = await CreateAsync();
        var session = await service.StartAsync(
            user, "eq", "drums", SeedIds.Level("eq", "boost", 1), CancellationToken.None);

        AnswerView? last = null;
        for (var i = 1; i <= CatalogSeeder.QuestionCount; i++)
        {
            var question = await db.TestSessionQuestions.AsNoTracking()
                .SingleAsync(q => q.TestSessionId == session.SessionId && q.QuestionIndex == i);
            last = await service.AnswerAsync(
                user, session.SessionId, i, question.CorrectAnswerKey, CancellationToken.None);
        }

        last.ShouldNotBeNull();
        last.Result.ShouldNotBeNull();
        last.Result.Passed.ShouldBeTrue();
        last.Result.CorrectAnswers.ShouldBe(CatalogSeeder.QuestionCount);
        last.NextQuestion.ShouldBeNull();

        var progress = await ClassicProgressAsync(db);
        progress.AttemptCount.ShouldBe(1);
        progress.BestScore.ShouldBe(CatalogSeeder.QuestionCount);
        progress.IsPassed.ShouldBeTrue();

        var again = await Should.ThrowAsync<ConflictException>(() =>
            service.AnswerAsync(user, session.SessionId, CatalogSeeder.QuestionCount, "125", CancellationToken.None));
        again.StatusCode.ShouldBe(409);
        (await ClassicProgressAsync(db)).AttemptCount.ShouldBe(1);
    }

    [Fact]
    public async Task New_session_abandons_the_previous_in_progress_one()
    {
        var (service, db, user) = await CreateAsync();
        var first = await service.StartAsync(
            user, "eq", "drums", SeedIds.Level("eq", "boost", 1), CancellationToken.None);
        var second = await service.StartAsync(
            user, "eq", "drums", SeedIds.Level("eq", "boost", 1), CancellationToken.None);

        first.SessionId.ShouldNotBe(second.SessionId);
        (await db.TestSessions.SingleAsync(s => s.Id == first.SessionId)).Status
            .ShouldBe(TestSessionStatus.Abandoned);
        (await ClassicProgressCountAsync(db)).ShouldBe(0);
    }

    [Fact]
    public async Task Abandon_does_not_write_progress()
    {
        var (service, db, user) = await CreateAsync();
        var session = await service.StartAsync(
            user, "eq", "drums", SeedIds.Level("eq", "boost", 1), CancellationToken.None);
        await service.AnswerAsync(user, session.SessionId, 1, "125", CancellationToken.None);
        await service.AbandonAsync(user, session.SessionId, CancellationToken.None);

        (await ClassicProgressCountAsync(db)).ShouldBe(0);
        (await db.TestSessions.SingleAsync()).Status.ShouldBe(TestSessionStatus.Abandoned);
    }

    [Fact]
    public async Task Get_resumes_at_the_first_unanswered_question()
    {
        var (service, db, user) = await CreateAsync();
        var started = await service.StartAsync(
            user, "eq", "drums", SeedIds.Level("eq", "boost", 1), CancellationToken.None);
        var first = await db.TestSessionQuestions.SingleAsync(
            q => q.TestSessionId == started.SessionId && q.QuestionIndex == 1);
        await service.AnswerAsync(user, started.SessionId, 1, first.CorrectAnswerKey, CancellationToken.None);

        var resumed = await service.GetAsync(user, started.SessionId, CancellationToken.None);
        resumed.AnsweredCount.ShouldBe(1);
        resumed.CorrectSoFar.ShouldBe(1);
        resumed.CurrentQuestion!.QuestionIndex.ShouldBe(2);
        resumed.Result.ShouldBeNull();
    }

    [Fact]
    public async Task Compression_question_audio_uses_token_not_variant_name()
    {
        var (service, _, user) = await CreateAsync();
        var session = await service.StartAsync(
            user, "compression", "drums", SeedIds.Level("compression", "detection", 1), CancellationToken.None);

        session.CurrentQuestion.ShouldNotBeNull();
        session.CurrentQuestion.Audio.Url.ShouldStartWith("/api/audio/questions/");
        session.CurrentQuestion.Eq.ShouldBeNull();
        session.CurrentQuestion.Audio.Url.ShouldNotContain("ratio");
        session.CurrentQuestion.Audio.Url.ShouldNotContain("heavy");
        session.CurrentQuestion.Audio.Url.ShouldNotContain("uncompressed");

        var raw = JsonSerializer.Serialize(session.CurrentQuestion);
        raw.ShouldNotContain("correctAnswerKey");
        raw.ShouldNotContain("storageKey");
    }

    private static Task<StudentProgress> ClassicProgressAsync(AppDbContext db) =>
        db.StudentProgress.SingleAsync(p =>
            p.AudioSourceId == SeedIds.Source("eq", "drums")
            && p.ExerciseLevelId == SeedIds.Level("eq", "boost", 1));

    private static Task<int> ClassicProgressCountAsync(AppDbContext db) =>
        db.StudentProgress.CountAsync(p =>
            p.AudioSourceId == SeedIds.Source("eq", "drums")
            && p.ExerciseLevelId == SeedIds.Level("eq", "boost", 1));

    private static async Task<(ITestSessionService Service, AppDbContext Db, Guid UserId)> CreateAsync(
        bool pinkIntroDone = true)
    {
        var db = await TestDb.CreateSeededInMemoryAsync();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            EntraObjectId = Guid.NewGuid().ToString(),
            Email = "ana@student.algebra.hr",
            DisplayName = "Ana",
            Role = UserRole.Student,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var progression = new ProgressionService(db, TimeProvider.System);
        if (pinkIntroDone)
        {
            await ProgressFixtures.CompleteFrequencyIntroAsync(progression, user.Id);
        }

        var generators = new QuestionGeneratorResolver(
        [
            new EqFrequencyGenerator(),
            new EqFrequencyAndDirectionGenerator(),
            new CompressionChoiceGenerator()
        ]);

        var service = new TestSessionService(
            db,
            new ModuleAccessService(db),
            progression,
            generators,
            TimeProvider.System);

        return (service, db, user.Id);
    }
}
