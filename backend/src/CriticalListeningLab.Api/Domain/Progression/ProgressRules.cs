namespace CriticalListeningLab.Api.Domain.Progression;

/// <summary>
/// Pravila azuriranja <c>StudentProgress</c> nakon zavrsenog testa.
/// BestScore i IsPassed se nikad ne vracaju unazad.
/// </summary>
public static class ProgressRules
{
    public static ProgressSnapshot Apply(
        ProgressSnapshot? current,
        Guid levelId,
        int correctAnswers,
        int passThreshold,
        DateTimeOffset now)
    {
        var passed = ScoreRules.IsPassed(correctAnswers, passThreshold);
        var isFirstPass = passed && current?.IsPassed != true;

        return new ProgressSnapshot(
            LevelId: levelId,
            BestScore: ScoreRules.BestScore(current?.BestScore ?? 0, correctAnswers),
            IsPassed: current?.IsPassed == true || passed,
            AttemptCount: (current?.AttemptCount ?? 0) + 1,
            FirstPassedAt: current?.FirstPassedAt ?? (passed ? now : null));
    }
}
