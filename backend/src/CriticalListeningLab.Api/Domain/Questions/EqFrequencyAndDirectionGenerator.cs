namespace CriticalListeningLab.Api.Domain.Questions;

public class EqFrequencyAndDirectionGenerator : IQuestionGenerator
{
    public ExerciseType ExerciseType => ExerciseType.EqFrequencyAndDirection;

    public IReadOnlyList<GeneratedQuestion> Generate(QuestionGenerationContext context)
    {
        var config = ExerciseConfig.ParseEq(context.Level.ConfigJson);
        var boostGain = config.GainsDb.First(g => g > 0);
        var cutGain = config.GainsDb.First(g => g < 0);

        var keys = config.FrequenciesHz
            .SelectMany(f => new[] { $"{f}:boost", $"{f}:cut" })
            .ToList();
        var sequence = BalancedAnswerSequence.Build(keys, context.QuestionCount, context.Random);

        var options = keys
            .Select(key =>
            {
                var parts = key.Split(':');
                return new AnswerOption(key, FrequencyLabels.Combined(int.Parse(parts[0]), parts[1]));
            })
            .ToList();

        return sequence.Select(key =>
        {
            var parts = key.Split(':');
            var frequency = int.Parse(parts[0]);
            var gain = parts[1] == "boost" ? boostGain : cutGain;
            return new GeneratedQuestion(
                Prompt: "Koja je frekvencija promijenjena i u kojem smjeru?",
                CorrectAnswerKey: key,
                Options: options,
                Eq: new EqBandPrompt(frequency, gain, config.Q),
                VariantSlug: null);
        }).ToList();
    }
}
