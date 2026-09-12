using CriticalListeningLab.Api.Domain.Progression;

namespace CriticalListeningLab.Tests.Progression;

public class ProgressRulesTests
{
    private static readonly Guid Level = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset T1 = DateTimeOffset.Parse("2026-01-01T10:00:00Z");
    private static readonly DateTimeOffset T2 = DateTimeOffset.Parse("2026-01-02T10:00:00Z");

    [Fact]
    public void Worse_later_result_does_not_reduce_BestScore()
    {
        var first = ProgressRules.Apply(null, Level, 14, 12, T1);
        var second = ProgressRules.Apply(first, Level, 12, 12, T2);

        second.BestScore.ShouldBe(14);
        second.IsPassed.ShouldBeTrue();
        second.AttemptCount.ShouldBe(2);
    }

    [Fact]
    public void Worse_later_result_does_not_clear_IsPassed()
    {
        var first = ProgressRules.Apply(null, Level, 12, 12, T1);
        var second = ProgressRules.Apply(first, Level, 3, 12, T2);

        second.IsPassed.ShouldBeTrue();
        second.BestScore.ShouldBe(12);
    }

    [Fact]
    public void First_pass_sets_FirstPassedAt_second_pass_keeps_it()
    {
        var fail = ProgressRules.Apply(null, Level, 11, 12, T1);
        fail.FirstPassedAt.ShouldBeNull();
        fail.IsPassed.ShouldBeFalse();

        var firstPass = ProgressRules.Apply(fail, Level, 12, 12, T1);
        firstPass.FirstPassedAt.ShouldBe(T1);

        var secondPass = ProgressRules.Apply(firstPass, Level, 14, 12, T2);
        secondPass.FirstPassedAt.ShouldBe(T1);
    }

    [Fact]
    public void AttemptCount_increments_for_pass_and_fail()
    {
        var fail = ProgressRules.Apply(null, Level, 11, 12, T1);
        var pass = ProgressRules.Apply(fail, Level, 12, 12, T2);

        fail.AttemptCount.ShouldBe(1);
        pass.AttemptCount.ShouldBe(2);
    }
}
