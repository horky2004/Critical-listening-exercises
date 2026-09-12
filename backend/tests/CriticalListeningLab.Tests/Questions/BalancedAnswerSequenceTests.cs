using CriticalListeningLab.Api.Domain.Questions;

namespace CriticalListeningLab.Tests.Questions;

public class BalancedAnswerSequenceTests
{
    [Theory]
    [InlineData(7, 14, "2,2,2,2,2,2,2")]
    [InlineData(4, 14, "4,4,3,3")]
    [InlineData(3, 14, "5,5,4")]
    [InlineData(2, 14, "7,7")]
    public void Distributes_counts_evenly(int optionCount, int length, string expected)
    {
        var options = Enumerable.Range(1, optionCount).Select(i => i.ToString()).ToList();
        var expectedCounts = expected.Split(',').Select(int.Parse).OrderByDescending(x => x).ToArray();

        for (var seed = 0; seed < 40; seed++)
        {
            var sequence = BalancedAnswerSequence.Build(options, length, new Random(seed));
            sequence.Count.ShouldBe(length);
            BalancedAnswerSequence.HasThreeInARow(sequence).ShouldBeFalse();

            var counts = BalancedAnswerSequence.Counts(sequence, options)
                .OrderByDescending(x => x)
                .ToArray();
            counts.ShouldBe(expectedCounts);
        }
    }
}
