namespace CriticalListeningLab.Api.Domain.Progression;

/// <summary>
/// Cjelobrojna pravila bodovanja. Postotak je samo za prikaz.
/// </summary>
public static class ScoreRules
{
    public static bool IsPassed(int correctAnswers, int passThreshold) =>
        correctAnswers >= passThreshold;

    public static double Percentage(int correctAnswers, int questionCount) =>
        questionCount == 0 ? 0 : Math.Round(correctAnswers * 100.0 / questionCount, 1);

    public static int BestScore(int currentBest, int newScore) =>
        Math.Max(currentBest, newScore);
}
