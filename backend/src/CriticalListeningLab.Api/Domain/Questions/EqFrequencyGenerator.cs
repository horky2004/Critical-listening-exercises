namespace CriticalListeningLab.Api.Domain.Questions;

public class EqFrequencyGenerator : IQuestionGenerator
{
    public ExerciseType ExerciseType => ExerciseType.EqFrequency;

    public IReadOnlyList<GeneratedQuestion> Generate(QuestionGenerationContext context)
    {
        var config = ExerciseConfig.ParseEq(context.Level.ConfigJson);
        var keys = config.FrequenciesHz.Select(f => f.ToString()).ToList();
        var sequence = BalancedAnswerSequence.Build(keys, context.QuestionCount, context.Random);
        var options = config.FrequenciesHz
            .Select(f => new AnswerOption(f.ToString(), FrequencyLabels.Hz(f)))
            .ToList();
        var gain = config.GainsDb[0];

        return sequence.Select(key => new GeneratedQuestion(
            Prompt: "Koja je frekvencija promijenjena?",
            CorrectAnswerKey: key,
            Options: options,
            Eq: new EqBandPrompt(int.Parse(key), gain, config.Q),
            VariantSlug: null)).ToList();
    }
}
