using System.Text.Json;
using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Data.Seed;
using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Domain.Entities;
using CriticalListeningLab.Api.Domain.Progression;
using CriticalListeningLab.Api.Domain.Questions;
using CriticalListeningLab.Api.Features.Modules;
using CriticalListeningLab.Api.Features.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CriticalListeningLab.Api.Features.TestSessions;

public class TestSessionService(
    AppDbContext db,
    IModuleAccessService access,
    IProgressionService progression,
    QuestionGeneratorResolver generators,
    TimeProvider time) : ITestSessionService
{
    public async Task<SessionView> StartAsync(
        Guid userId, string moduleSlug, string sourceSlug, Guid levelId, CancellationToken ct)
    {
        if (!await access.IsModuleAvailableAsync(userId, moduleSlug, ct))
        {
            throw new ForbiddenException("Modul nije dostupan.", "module-unavailable");
        }

        var source = await access.FindSourceAsync(moduleSlug, sourceSlug, ct)
                     ?? throw new NotFoundException("Audio izvor nije pronaden u tom modulu.");
        if (!await access.IsSourceAvailableAsync(userId, moduleSlug, sourceSlug, ct))
        {
            throw new ForbiddenException(
                "Izvor je dostupan nakon upoznavanja s frekvencijama.", "source-locked");
        }

        var level = await db.ExerciseLevels
            .Include(l => l.Segment)
            .ThenInclude(s => s.Module)
            .FirstOrDefaultAsync(l => l.Id == levelId, ct)
                    ?? throw new NotFoundException("Level nije pronaden.");

        if (level.Segment.ModuleId != source.ModuleId || !level.IsEnabled)
        {
            throw new NotFoundException("Level ne pripada navedenom modulu.");
        }

        if (level.Segment.Key == CatalogSeeder.IntroSegmentKey
            && !CatalogSeeder.FrequencyIntroAppliesTo(moduleSlug, sourceSlug))
        {
            throw new NotFoundException("Upoznavanje s frekvencijama postoji samo na ružičastom šumu.");
        }

        if (!await progression.IsUnlockedAsync(userId, source.Id, levelId, ct))
        {
            throw new ForbiddenException("Level jos nije otkljucan.", "level-locked");
        }

        var inProgress = await db.TestSessions
            .Where(s => s.UserId == userId
                        && s.AudioSourceId == source.Id
                        && s.ExerciseLevelId == levelId
                        && s.Status == TestSessionStatus.InProgress)
            .ToListAsync(ct);

        foreach (var existing in inProgress)
        {
            existing.Status = TestSessionStatus.Abandoned;
            existing.CompletedAt = time.GetUtcNow();
        }

        var assets = await db.AudioAssets
            .Where(a => a.AudioSourceId == source.Id && a.IsEnabled)
            .ToListAsync(ct);

        var seed = Random.Shared.Next();
        var generated = generators.For(level.ExerciseType).Generate(
            new QuestionGenerationContext(level, level.QuestionCount, new Random(seed)));

        var session = new TestSession
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            AudioSourceId = source.Id,
            ExerciseLevelId = level.Id,
            Status = TestSessionStatus.InProgress,
            StartedAt = time.GetUtcNow(),
            QuestionCount = level.QuestionCount,
            PassThreshold = level.PassThreshold,
            RandomSeed = seed
        };

        var index = 1;
        foreach (var question in generated)
        {
            session.Questions.Add(ToEntity(session.Id, index, question, assets));
            index++;
        }

        db.TestSessions.Add(session);
        await db.SaveChangesAsync(ct);

        return await GetAsync(userId, session.Id, ct);
    }

    public async Task<SessionView> GetAsync(Guid userId, Guid sessionId, CancellationToken ct)
    {
        var session = await LoadOwnedAsync(userId, sessionId, ct);
        return ToView(session, includeNewlyUnlocked: false, null);
    }

    public async Task<AnswerView> AnswerAsync(
        Guid userId, Guid sessionId, int questionIndex, string answerKey, CancellationToken ct)
    {
        await using var transaction = await BeginTransactionIfPossibleAsync(ct);

        var session = await LoadOwnedAsync(userId, sessionId, ct);
        if (session.Status != TestSessionStatus.InProgress)
        {
            throw new ConflictException("Sesija vise nije u tijeku.");
        }

        var answeredCount = session.Questions.Count(q => q.IsCorrect is not null);
        if (questionIndex != answeredCount + 1)
        {
            throw new ConflictException("Odgovori moraju ici redom.");
        }

        var question = session.Questions.SingleOrDefault(q => q.QuestionIndex == questionIndex)
                       ?? throw new NotFoundException("Pitanje nije pronadeno.");

        if (question.IsCorrect is not null)
        {
            throw new ConflictException("Pitanje je vec odgovoreno.");
        }

        var prompt = DeserializePrompt(question.PromptJson);
        if (prompt.AnswerOptions.All(o => o.Key != answerKey))
        {
            throw new BadRequestException("Odgovor nije medu ponudenim opcijama.");
        }

        question.StudentAnswerKey = answerKey;
        question.IsCorrect = string.Equals(answerKey, question.CorrectAnswerKey, StringComparison.Ordinal);
        question.AnsweredAt = time.GetUtcNow();

        SessionResultView? result = null;
        var isLast = questionIndex == session.QuestionCount;

        if (isLast)
        {
            result = await FinalizeAsync(session, ct);
        }

        await db.SaveChangesAsync(ct);
        if (transaction is not null)
        {
            await transaction.CommitAsync(ct);
        }

        var next = isLast
            ? null
            : ToQuestionView(session, session.Questions.Single(q => q.QuestionIndex == questionIndex + 1));

        return new AnswerView(
            question.IsCorrect.Value,
            question.CorrectAnswerKey,
            session.Questions.Count(q => q.IsCorrect is not null),
            session.Questions.Count(q => q.IsCorrect == true),
            next,
            result);
    }

    public async Task AbandonAsync(Guid userId, Guid sessionId, CancellationToken ct)
    {
        var session = await LoadOwnedAsync(userId, sessionId, ct);
        if (session.Status != TestSessionStatus.InProgress)
        {
            throw new ConflictException("Sesija vise nije u tijeku.");
        }

        session.Status = TestSessionStatus.Abandoned;
        session.CompletedAt = time.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }

    private async Task<SessionResultView> FinalizeAsync(TestSession session, CancellationToken ct)
    {
        if (session.Status == TestSessionStatus.Completed)
        {
            return ToResult(session, isFirstPass: false, []);
        }

        var correct = session.Questions.Count(q => q.IsCorrect == true);
        var passed = ScoreRules.IsPassed(correct, session.PassThreshold);

        session.Status = TestSessionStatus.Completed;
        session.CompletedAt = time.GetUtcNow();
        session.CorrectAnswers = correct;
        session.Passed = passed;

        var progress = await progression.ApplyTestResultAsync(
            session.UserId, session.AudioSourceId, session.ExerciseLevelId, correct, ct,
            session.PassThreshold);

        var unlockedIds = progress.NewlyUnlockedLevels.Select(l => l.LevelId).ToList();
        var segmentKeys = await db.ExerciseLevels.AsNoTracking()
            .Where(l => unlockedIds.Contains(l.Id))
            .Select(l => new { l.Id, l.Segment.Key })
            .ToDictionaryAsync(l => l.Id, l => l.Key, ct);

        var unlocked = progress.NewlyUnlockedLevels
            .Select(l => new UnlockedLevelView(
                l.LevelId,
                segmentKeys.GetValueOrDefault(l.LevelId) ?? "",
                l.LevelNumber,
                l.Title,
                session.AudioSource.Slug,
                session.AudioSource.Name))
            .ToList();

        if (progress.IsFirstPass
            && passed
            && CatalogSeeder.FrequencyIntroAppliesTo(session.AudioSource.Module.Slug, session.AudioSource.Slug)
            && session.ExerciseLevel.Segment.Key == CatalogSeeder.IntroSegmentKey
            && session.ExerciseLevel.LevelNumber == 3)
        {
            unlocked.AddRange(await MusicalSourceStarterUnlocksAsync(ct));
        }

        return new SessionResultView(
            correct,
            session.QuestionCount,
            ScoreRules.Percentage(correct, session.QuestionCount),
            passed,
            progress.IsFirstPass,
            unlocked);
    }

    private async Task<IReadOnlyList<UnlockedLevelView>> MusicalSourceStarterUnlocksAsync(CancellationToken ct)
    {
        var boost1Id = SeedIds.Level("eq", "boost", 1);
        var title = await db.ExerciseLevels.AsNoTracking()
            .Where(level => level.Id == boost1Id)
            .Select(level => level.Title)
            .SingleAsync(ct);

        var sources = await db.AudioSources.AsNoTracking()
            .Where(source =>
                source.ModuleId == SeedIds.Module("eq")
                && CatalogSeeder.MusicalEqSourceSlugs.Contains(source.Slug))
            .OrderBy(source => source.SortOrder)
            .Select(source => new { source.Slug, source.Name })
            .ToListAsync(ct);

        return sources
            .Select(source => new UnlockedLevelView(boost1Id, "boost", 1, title, source.Slug, source.Name))
            .ToList();
    }

    private async Task<TestSession> LoadOwnedAsync(Guid userId, Guid sessionId, CancellationToken ct)
    {
        var session = await db.TestSessions
            .Include(s => s.Questions)
            .Include(s => s.AudioSource)
            .ThenInclude(a => a.Module)
            .Include(s => s.AudioSource)
            .ThenInclude(a => a.Assets)
            .Include(s => s.ExerciseLevel)
            .ThenInclude(l => l.Segment)
            .ThenInclude(seg => seg.Module)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
        {
            throw new NotFoundException("Sesija nije pronadena.");
        }

        if (session.UserId != userId)
        {
            throw new ForbiddenException("Sesija nije tvoja.", "session-not-owned");
        }

        return session;
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfPossibleAsync(CancellationToken ct) =>
        db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;

    private static TestSessionQuestion ToEntity(
        Guid sessionId, int index, GeneratedQuestion question, IReadOnlyList<AudioAsset> assets)
    {
        Guid? assetId = null;
        if (question.VariantSlug is not null)
        {
            assetId = assets.FirstOrDefault(a => a.VariantSlug == question.VariantSlug)?.Id
                      ?? throw new InvalidOperationException(
                          $"Nedostaje audio varijanta '{question.VariantSlug}'.");
        }

        return new TestSessionQuestion
        {
            Id = Guid.CreateVersion7(),
            TestSessionId = sessionId,
            QuestionIndex = index,
            PromptJson = JsonSerializer.Serialize(
                new QuestionPromptJson(question.Prompt, question.Options, question.Eq),
                ExerciseConfig.JsonOptions),
            CorrectAnswerKey = question.CorrectAnswerKey,
            AudioAssetId = assetId,
            AudioToken = Guid.NewGuid()
        };
    }

    private static SessionView ToView(
        TestSession session, bool includeNewlyUnlocked, IReadOnlyList<UnlockedLevelView>? newlyUnlocked)
    {
        var answered = session.Questions.Count(q => q.IsCorrect is not null);
        var correctSoFar = session.Questions.Count(q => q.IsCorrect == true);
        var current = session.Status == TestSessionStatus.InProgress
            ? session.Questions
                .Where(q => q.IsCorrect is null)
                .OrderBy(q => q.QuestionIndex)
                .Select(q => ToQuestionView(session, q))
                .FirstOrDefault()
            : null;

        SessionResultView? result = session.Status == TestSessionStatus.Completed
            ? ToResult(session, isFirstPass: false, includeNewlyUnlocked ? newlyUnlocked ?? [] : [])
            : null;

        return new SessionView(
            session.Id,
            new ModuleRef(session.AudioSource.Module.Slug, session.AudioSource.Module.Name),
            new SourceRef(session.AudioSource.Slug, session.AudioSource.Name),
            new LevelRef(
                session.ExerciseLevel.Id,
                session.ExerciseLevel.Segment.Key,
                session.ExerciseLevel.LevelNumber,
                session.ExerciseLevel.Title),
            session.QuestionCount,
            session.PassThreshold,
            answered,
            correctSoFar,
            current,
            result);
    }

    private static SessionResultView ToResult(
        TestSession session, bool isFirstPass, IReadOnlyList<UnlockedLevelView> newlyUnlocked) =>
        new(session.CorrectAnswers,
            session.QuestionCount,
            ScoreRules.Percentage(session.CorrectAnswers, session.QuestionCount),
            session.Passed,
            isFirstPass,
            newlyUnlocked);

    private static QuestionView ToQuestionView(TestSession session, TestSessionQuestion question)
    {
        var prompt = DeserializePrompt(question.PromptJson);
        var audio = prompt.Eq is not null
            ? new AudioView($"/api/audio/assets/{EqAssetId(session)}", true)
            : new AudioView($"/api/audio/questions/{question.AudioToken}", true);

        return new QuestionView(
            question.QuestionIndex,
            session.QuestionCount,
            prompt.Prompt,
            prompt.AnswerOptions,
            audio,
            prompt.Eq);
    }

    private static Guid EqAssetId(TestSession session)
    {
        var full = session.AudioSource.Assets.FirstOrDefault(a => a.VariantSlug == "full");
        return full?.Id ?? session.AudioSource.Assets.First().Id;
    }

    private static QuestionPromptJson DeserializePrompt(string json) =>
        JsonSerializer.Deserialize<QuestionPromptJson>(json, ExerciseConfig.JsonOptions)
        ?? throw new InvalidOperationException("PromptJson nije valjan.");
}
