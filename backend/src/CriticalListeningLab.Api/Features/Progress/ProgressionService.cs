using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Domain.Entities;
using CriticalListeningLab.Api.Domain.Progression;
using Microsoft.EntityFrameworkCore;

namespace CriticalListeningLab.Api.Features.Progress;

public class ProgressionService(AppDbContext db, TimeProvider time) : IProgressionService
{
    public async Task<SourceTreeState> GetTreeStateAsync(
        Guid userId, Guid audioSourceId, CancellationToken ct)
    {
        var snapshot = await LoadAsync(userId, audioSourceId, ct);
        return BuildTree(snapshot, UnlockEvaluator.Evaluate(snapshot.Levels, snapshot.Progress));
    }

    public async Task<bool> IsUnlockedAsync(
        Guid userId, Guid audioSourceId, Guid levelId, CancellationToken ct)
    {
        var snapshot = await LoadAsync(userId, audioSourceId, ct);
        return UnlockEvaluator.IsUnlocked(snapshot.Levels, snapshot.Progress, levelId);
    }

    public async Task<ProgressUpdateResult> ApplyTestResultAsync(
        Guid userId, Guid audioSourceId, Guid levelId, int correctAnswers, CancellationToken ct,
        int? passThreshold = null)
    {
        var snapshot = await LoadAsync(userId, audioSourceId, ct);
        var level = snapshot.Rows.FirstOrDefault(l => l.Id == levelId)
                    ?? throw new InvalidOperationException($"Level {levelId} ne postoji na ovom izvoru.");

        var threshold = passThreshold ?? level.PassThreshold;
        var before = UnlockEvaluator.Evaluate(snapshot.Levels, snapshot.Progress);
        var now = time.GetUtcNow();
        var applied = ProgressRules.Apply(
            snapshot.Progress.GetValueOrDefault(levelId),
            levelId,
            correctAnswers,
            threshold,
            now);

        var progress = await db.StudentProgress
            .FirstOrDefaultAsync(
                p => p.UserId == userId && p.AudioSourceId == audioSourceId && p.ExerciseLevelId == levelId,
                ct);

        if (progress is null)
        {
            progress = new StudentProgress
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                AudioSourceId = audioSourceId,
                ExerciseLevelId = levelId,
                BestScore = applied.BestScore,
                IsPassed = applied.IsPassed,
                AttemptCount = applied.AttemptCount,
                FirstPassedAt = applied.FirstPassedAt,
                LastAttemptAt = now
            };
            db.StudentProgress.Add(progress);
        }
        else
        {
            progress.BestScore = applied.BestScore;
            progress.IsPassed = applied.IsPassed;
            progress.AttemptCount = applied.AttemptCount;
            progress.FirstPassedAt = applied.FirstPassedAt;
            progress.LastAttemptAt = now;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Paralelna finalizacija: unique indeks, retry kao update.
            db.ChangeTracker.Clear();
            return await ApplyTestResultAsync(userId, audioSourceId, levelId, correctAnswers, ct);
        }

        var afterProgress = new Dictionary<Guid, ProgressSnapshot>(snapshot.Progress)
        {
            [levelId] = applied
        };
        var after = UnlockEvaluator.Evaluate(snapshot.Levels, afterProgress);
        var newlyUnlocked = snapshot.Rows
            .Where(row => before[row.Id] == LevelStatus.Locked && after[row.Id] != LevelStatus.Locked)
            .Select(row => ToLevelState(row, after[row.Id], afterProgress.GetValueOrDefault(row.Id)))
            .ToList();

        var passed = ScoreRules.IsPassed(correctAnswers, threshold);
        return new ProgressUpdateResult(
            applied.BestScore,
            passed,
            IsFirstPass: passed && snapshot.Progress.GetValueOrDefault(levelId)?.IsPassed != true,
            newlyUnlocked);
    }

    private async Task<CatalogSnapshot> LoadAsync(Guid userId, Guid audioSourceId, CancellationToken ct)
    {
        var source = await db.AudioSources.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == audioSourceId, ct)
                     ?? throw new InvalidOperationException($"Audio izvor {audioSourceId} ne postoji.");

        var rows = await db.ExerciseLevels.AsNoTracking()
            .Where(l => l.Segment.ModuleId == source.ModuleId && l.IsEnabled)
            .Include(l => l.Segment)
            .Include(l => l.UnlockRequirements)
            .OrderBy(l => l.Segment.SortOrder)
            .ThenBy(l => l.LevelNumber)
            .ToListAsync(ct);

        var progressRows = await db.StudentProgress.AsNoTracking()
            .Where(p => p.UserId == userId && p.AudioSourceId == audioSourceId)
            .ToListAsync(ct);

        var progress = progressRows.ToDictionary(
            p => p.ExerciseLevelId,
            p => new ProgressSnapshot(p.ExerciseLevelId, p.BestScore, p.IsPassed, p.AttemptCount, p.FirstPassedAt));

        var levels = rows
            .Select(l => new LevelDefinition(
                l.Id,
                l.UnlockRequirements
                    .Select(r => new UnlockRequirementDefinition(
                        r.RequiredExerciseLevelId, r.RequirementType, r.MinScore))
                    .ToList()))
            .ToList();

        return new CatalogSnapshot(source, rows, levels, progress);
    }

    private static SourceTreeState BuildTree(
        CatalogSnapshot snapshot,
        IReadOnlyDictionary<Guid, LevelStatus> statuses)
    {
        var segments = snapshot.Rows
            .GroupBy(l => l.SegmentId)
            .Select(group =>
            {
                var segment = group.First().Segment;
                var levels = group
                    .OrderBy(l => l.LevelNumber)
                    .Select(l => ToLevelState(
                        l,
                        statuses[l.Id],
                        snapshot.Progress.GetValueOrDefault(l.Id)))
                    .ToList();

                return new SegmentState(segment.Key, segment.Name, segment.SortOrder, levels);
            })
            .OrderBy(s => s.SortOrder)
            .ToList();

        return new SourceTreeState(snapshot.Source.Id, snapshot.Source.Slug, segments);
    }

    private static LevelState ToLevelState(
        ExerciseLevel level, LevelStatus status, ProgressSnapshot? progress)
    {
        var best = progress?.BestScore ?? 0;
        return new LevelState(
            level.Id,
            level.LevelNumber,
            level.Title,
            status,
            best,
            ScoreRules.Percentage(best, level.QuestionCount),
            level.QuestionCount,
            level.PassThreshold,
            progress?.AttemptCount ?? 0,
            progress?.FirstPassedAt,
            level.UnlockRequirements.Select(r => r.RequiredExerciseLevelId).ToList());
    }

    private sealed record CatalogSnapshot(
        AudioSource Source,
        IReadOnlyList<ExerciseLevel> Rows,
        IReadOnlyList<LevelDefinition> Levels,
        IReadOnlyDictionary<Guid, ProgressSnapshot> Progress);
}
