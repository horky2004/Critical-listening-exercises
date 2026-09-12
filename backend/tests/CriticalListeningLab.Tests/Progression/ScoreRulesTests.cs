using CriticalListeningLab.Api.Domain.Progression;

namespace CriticalListeningLab.Tests.Progression;

public class ScoreRulesTests
{
    [Theory]
    [InlineData(11, false)]
    [InlineData(12, true)]
    [InlineData(13, true)]
    [InlineData(14, true)]
    [InlineData(0, false)]
    public void Pass_is_integer_comparison_against_threshold(int correct, bool expected)
    {
        ScoreRules.IsPassed(correct, 12).ShouldBe(expected);
    }

    [Fact]
    public void Percentage_is_display_only_and_12_of_14_is_85_7()
    {
        ScoreRules.Percentage(12, 14).ShouldBe(85.7);
        ScoreRules.Percentage(11, 14).ShouldBe(78.6);
        ScoreRules.IsPassed(11, 12).ShouldBeFalse();
        ScoreRules.IsPassed(12, 12).ShouldBeTrue();
    }

    [Fact]
    public void BestScore_never_decreases()
    {
        ScoreRules.BestScore(14, 12).ShouldBe(14);
        ScoreRules.BestScore(12, 14).ShouldBe(14);
        ScoreRules.BestScore(0, 11).ShouldBe(11);
    }
}
