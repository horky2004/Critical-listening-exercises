using CriticalListeningLab.Api.Domain.Entities;

namespace CriticalListeningLab.Api.Domain.Questions;

public sealed record QuestionGenerationContext(
    ExerciseLevel Level,
    int QuestionCount,
    Random Random);

public interface IQuestionGenerator
{
    ExerciseType ExerciseType { get; }

    IReadOnlyList<GeneratedQuestion> Generate(QuestionGenerationContext context);
}
