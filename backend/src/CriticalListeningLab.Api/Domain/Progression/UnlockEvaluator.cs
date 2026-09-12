namespace CriticalListeningLab.Api.Domain.Progression;

public sealed record UnlockRequirementDefinition(
    Guid RequiredLevelId,
    UnlockRequirementType RequirementType,
    int? MinScore);

public sealed record LevelDefinition(
    Guid Id,
    IReadOnlyList<UnlockRequirementDefinition> Requirements);

public sealed record ProgressSnapshot(
    Guid LevelId,
    int BestScore,
    bool IsPassed,
    int AttemptCount,
    DateTimeOffset? FirstPassedAt);

/// <summary>
/// Cista evaluacija unlock pravila. Nema EF-a, nema <c>if (level == 3)</c>.
/// Graf je aciklican (provjerava seed test), pa je jedan prolaz dovoljan.
/// </summary>
public static class UnlockEvaluator
{
    public static IReadOnlyDictionary<Guid, LevelStatus> Evaluate(
        IReadOnlyList<LevelDefinition> levels,
        IReadOnlyDictionary<Guid, ProgressSnapshot> progress)
    {
        var result = new Dictionary<Guid, LevelStatus>(levels.Count);

        foreach (var level in levels)
        {
            if (!RequirementsSatisfied(level, progress))
            {
                result[level.Id] = LevelStatus.Locked;
                continue;
            }

            if (!progress.TryGetValue(level.Id, out var snapshot))
            {
                result[level.Id] = LevelStatus.Unlocked;
                continue;
            }

            result[level.Id] = snapshot.IsPassed ? LevelStatus.Completed : LevelStatus.InProgress;
        }

        return result;
    }

    public static bool IsUnlocked(
        IReadOnlyList<LevelDefinition> levels,
        IReadOnlyDictionary<Guid, ProgressSnapshot> progress,
        Guid levelId)
    {
        var statuses = Evaluate(levels, progress);
        return statuses.TryGetValue(levelId, out var status) && status != LevelStatus.Locked;
    }

    private static bool RequirementsSatisfied(
        LevelDefinition level,
        IReadOnlyDictionary<Guid, ProgressSnapshot> progress)
    {
        if (level.Requirements.Count == 0)
        {
            return true;
        }

        return level.Requirements.All(requirement => IsSatisfied(requirement, progress));
    }

    private static bool IsSatisfied(
        UnlockRequirementDefinition requirement,
        IReadOnlyDictionary<Guid, ProgressSnapshot> progress)
    {
        if (requirement.RequirementType != UnlockRequirementType.PassedLevel)
        {
            return false;
        }

        if (!progress.TryGetValue(requirement.RequiredLevelId, out var snapshot))
        {
            return false;
        }

        return requirement.MinScore is null
            ? snapshot.IsPassed
            : snapshot.BestScore >= requirement.MinScore;
    }
}
