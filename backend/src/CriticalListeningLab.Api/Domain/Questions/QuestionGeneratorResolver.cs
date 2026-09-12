namespace CriticalListeningLab.Api.Domain.Questions;

public class QuestionGeneratorResolver(IEnumerable<IQuestionGenerator> generators)
{
    private readonly Dictionary<ExerciseType, IQuestionGenerator> _generators =
        generators.ToDictionary(g => g.ExerciseType);

    public IQuestionGenerator For(ExerciseType type) =>
        _generators.TryGetValue(type, out var generator)
            ? generator
            : throw new InvalidOperationException($"Nema generatora za {type}.");
}
