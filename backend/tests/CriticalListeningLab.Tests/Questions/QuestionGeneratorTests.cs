using CriticalListeningLab.Api.Data.Seed;
using CriticalListeningLab.Api.Domain.Entities;
using CriticalListeningLab.Api.Domain.Questions;
using Microsoft.EntityFrameworkCore;

namespace CriticalListeningLab.Tests.Questions;

public class QuestionGeneratorTests
{
    [Fact]
    public async Task Eq_boost_L1_uses_four_frequencies_without_three_in_a_row()
    {
        var level = await LoadLevel("eq", "boost", 1);
        var questions = new EqFrequencyGenerator().Generate(new QuestionGenerationContext(level, 14, new Random(1)));

        questions.Count.ShouldBe(14);
        BalancedAnswerSequence.HasThreeInARow(questions.Select(q => q.CorrectAnswerKey).ToList()).ShouldBeFalse();
        var keys = questions.Select(q => q.CorrectAnswerKey).Distinct().OrderBy(k => k).ToList();
        keys.ShouldBe(["125", "2000", "500", "8000"]);
        questions.ShouldAllBe(q => q.Eq != null && q.Eq.GainDb == 12);
        questions.ShouldAllBe(q => q.Prompt == "Koja je frekvencija promijenjena?");
    }

    [Fact]
    public async Task Eq_boost_L2_uses_all_seven_frequencies_twice()
    {
        var level = await LoadLevel("eq", "boost", 2);
        var questions = new EqFrequencyGenerator().Generate(new QuestionGenerationContext(level, 14, new Random(2)));

        questions.Count.ShouldBe(14);
        BalancedAnswerSequence.HasThreeInARow(questions.Select(q => q.CorrectAnswerKey).ToList()).ShouldBeFalse();
        BalancedAnswerSequence.Counts(
                questions.Select(q => q.CorrectAnswerKey).ToList(),
                ["125", "250", "500", "1000", "2000", "4000", "8000"])
            .ShouldAllBe(count => count == 2);
        questions.ShouldAllBe(q => q.Options.Count == 7);
    }

    [Fact]
    public async Task Combined_balances_boost_and_cut()
    {
        var level = await LoadLevel("eq", "combined", 1);
        var questions = new EqFrequencyAndDirectionGenerator()
            .Generate(new QuestionGenerationContext(level, 14, new Random(3)));

        questions.Count(q => q.CorrectAnswerKey.EndsWith(":boost")).ShouldBe(7);
        questions.Count(q => q.CorrectAnswerKey.EndsWith(":cut")).ShouldBe(7);
        questions.ShouldAllBe(q => q.CorrectAnswerKey.Contains(':'));
        BalancedAnswerSequence.HasThreeInARow(questions.Select(q => q.CorrectAnswerKey).ToList()).ShouldBeFalse();
    }

    [Fact]
    public async Task Compression_L1_is_seven_and_seven()
    {
        var level = await LoadLevel("compression", "detection", 1);
        var questions = new CompressionChoiceGenerator()
            .Generate(new QuestionGenerationContext(level, 14, new Random(8)));

        questions.Count(q => q.CorrectAnswerKey == "uncompressed").ShouldBe(7);
        questions.Count(q => q.CorrectAnswerKey == "compressed").ShouldBe(7);
        questions.Where(q => q.CorrectAnswerKey == "compressed").ShouldAllBe(q => q.VariantSlug == "heavy");
        questions.ShouldAllBe(q => q.Eq == null);
    }

    [Fact]
    public async Task Compression_L3_is_five_five_four()
    {
        var level = await LoadLevel("compression", "detection", 3);
        var questions = new CompressionChoiceGenerator()
            .Generate(new QuestionGenerationContext(level, 14, new Random(11)));

        var counts = questions.GroupBy(q => q.CorrectAnswerKey)
            .Select(g => g.Count())
            .OrderByDescending(c => c)
            .ToArray();
        counts.ShouldBe([5, 5, 4]);
    }

    private static async Task<ExerciseLevel> LoadLevel(string module, string segment, int number)
    {
        await using var db = await Support.TestDb.CreateSeededInMemoryAsync();
        return await db.ExerciseLevels
            .Include(l => l.Segment)
            .SingleAsync(l => l.Id == SeedIds.Level(module, segment, number));
    }
}
