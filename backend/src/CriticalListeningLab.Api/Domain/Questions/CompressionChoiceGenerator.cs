namespace CriticalListeningLab.Api.Domain.Questions;

public class CompressionChoiceGenerator : IQuestionGenerator
{
    public ExerciseType ExerciseType => ExerciseType.CompressionChoice;

    public IReadOnlyList<GeneratedQuestion> Generate(QuestionGenerationContext context)
    {
        var config = ExerciseConfig.ParseCompression(context.Level.ConfigJson);
        var keys = config.Options.Select(o => o.Key).ToList();
        var sequence = BalancedAnswerSequence.Build(keys, context.QuestionCount, context.Random);
        var options = config.Options
            .Select(o => new AnswerOption(o.Key, o.Label))
            .ToList();
        var byKey = config.Options.ToDictionary(o => o.Key);

        return sequence.Select(key =>
        {
            var option = byKey[key];
            var variant = option.Variants[context.Random.Next(option.Variants.Count)];
            return new GeneratedQuestion(
                Prompt: context.Level.Title,
                CorrectAnswerKey: key,
                Options: options,
                Eq: null,
                VariantSlug: variant);
        }).ToList();
    }
}
