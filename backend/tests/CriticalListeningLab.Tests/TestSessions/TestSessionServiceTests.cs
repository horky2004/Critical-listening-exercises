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
    public async Task Start_creates_fourteen_questions_without_correctAnswerKey_in_prompt()
    {
        var (service, db, user) = await CreateAsync();
        var session = await service.StartAsync(
            user, "eq", "drums", SeedIds.Level("eq", "boost", 1), CancellationToken.None);

        session.QuestionCount.ShouldBe(14);
        session.AnsweredCount.ShouldBe(0);
        session.CurrentQuestion.ShouldNotBeNull();
        session.CurrentQuestion.QuestionIndex.ShouldBe(1);
        session.CurrentQuestion.Eq.ShouldNotBeNull();
        session.CurrentQuestion.Audio.Url.ShouldStartWith("/api/audio/assets/");

        var stored = await db.TestSessionQuestions.ToListAsync();
        stored.Count.ShouldBe(14);
        stored.ShouldAllBe(q => q.AudioToken != Guid.Empty);
        foreach (var question in stored)
        {
            question.PromptJson.ShouldNotContain("correctAnswerKey");
            question.PromptJson.ShouldNotContain("CorrectAnswerKey");
        }
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
        for (var i = 1; i <= 14; i++)
        {
            var question = await db.TestSessionQuestions.AsNoTracking()
                .SingleAsync(q => q.TestSessionId == session.SessionId && q.QuestionIndex == i);
            last = await service.AnswerAsync(
                user, session.SessionId, i, question.CorrectAnswerKey, CancellationToken.None);
        }

        last.ShouldNotBeNull();
        last.Result.ShouldNotBeNull();
        last.Result.Passed.ShouldBeTrue();
        last.Result.CorrectAnswers.ShouldBe(14);
        last.NextQuestion.ShouldBeNull();

        var progress = await db.StudentProgress.SingleAsync();
        progress.AttemptCount.ShouldBe(1);
        progress.BestScore.ShouldBe(14);
        progress.IsPassed.ShouldBeTrue();

        var again = await Should.ThrowAsync<ConflictException>(() =>
            service.AnswerAsync(user, session.SessionId, 14, "125", CancellationToken.None));
        again.StatusCode.ShouldBe(409);
        (await db.StudentProgress.SingleAsync()).AttemptCount.ShouldBe(1);
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
        (await db.StudentProgress.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Abandon_does_not_write_progress()
    {
        var (service, db, user) = await CreateAsync();
        var session = await service.StartAsync(
            user, "eq", "drums", SeedIds.Level("eq", "boost", 1), CancellationToken.None);
        await service.AnswerAsync(user, session.SessionId, 1, "125", CancellationToken.None);
        await service.AbandonAsync(user, session.SessionId, CancellationToken.None);

        (await db.StudentProgress.CountAsync()).ShouldBe(0);
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

    private static async Task<(ITestSessionService Service, AppDbContext Db, Guid UserId)> CreateAsync()
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

        var generators = new QuestionGeneratorResolver(
        [
            new EqFrequencyGenerator(),
            new EqFrequencyAndDirectionGenerator(),
            new CompressionChoiceGenerator()
        ]);

        var service = new TestSessionService(
            db,
            new ModuleAccessService(db),
            new ProgressionService(db, TimeProvider.System),
            generators,
            TimeProvider.System);

        return (service, db, user.Id);
    }
}
