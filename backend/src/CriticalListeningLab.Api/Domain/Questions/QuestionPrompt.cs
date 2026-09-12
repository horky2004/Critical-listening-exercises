namespace CriticalListeningLab.Api.Domain.Questions;

public sealed record AnswerOption(string Key, string Label);

public sealed record EqBandPrompt(int FrequencyHz, int GainDb, double Q);

public sealed record GeneratedQuestion(
    string Prompt,
    string CorrectAnswerKey,
    IReadOnlyList<AnswerOption> Options,
    EqBandPrompt? Eq,
    string? VariantSlug);
