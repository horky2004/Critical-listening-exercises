namespace CriticalListeningLab.Api.Domain.Questions;

/// <summary>
/// Multiset odgovora kroz N pitanja, zamijesan tako da ista vrijednost
/// ne smije doci vise od dva puta zaredom (odluka 17).
/// </summary>
public static class BalancedAnswerSequence
{
    public static IReadOnlyList<T> Build<T>(IReadOnlyList<T> options, int length, Random random)
        where T : notnull
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);
        if (options.Count == 0)
        {
            throw new ArgumentException("Mora postojati barem jedna opcija.", nameof(options));
        }

        for (var attempt = 0; attempt < 32; attempt++)
        {
            var remaining = Distribute(options, length, random);
            if (TryGreedy(remaining, length, random, out var sequence))
            {
                return sequence;
            }
        }

        throw new InvalidOperationException(
            "Nije moguce rasporediti odgovore bez tri ista zaredom.");
    }

    public static int[] Counts<T>(IReadOnlyList<T> sequence, IReadOnlyList<T> options)
        where T : notnull
    {
        return options.Select(option => sequence.Count(item => Equals(item, option))).ToArray();
    }

    public static bool HasThreeInARow<T>(IReadOnlyList<T> sequence)
    {
        for (var i = 2; i < sequence.Count; i++)
        {
            if (Equals(sequence[i], sequence[i - 1]) && Equals(sequence[i], sequence[i - 2]))
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<T, int> Distribute<T>(IReadOnlyList<T> options, int length, Random random)
        where T : notnull
    {
        var order = options.ToList();
        Shuffle(order, random);

        var baseline = length / options.Count;
        var extra = length % options.Count;
        var remaining = new Dictionary<T, int>();

        for (var i = 0; i < order.Count; i++)
        {
            remaining[order[i]] = baseline + (i < extra ? 1 : 0);
        }

        return remaining;
    }

    private static bool TryGreedy<T>(
        Dictionary<T, int> remaining, int length, Random random, out List<T> sequence)
        where T : notnull
    {
        sequence = new List<T>(length);

        while (sequence.Count < length)
        {
            T? forbidden = default;
            var hasForbidden = sequence.Count >= 2
                               && Equals(sequence[^1], sequence[^2]);
            if (hasForbidden)
            {
                forbidden = sequence[^1];
            }

            var candidates = remaining
                .Where(pair => pair.Value > 0 && !(hasForbidden && Equals(pair.Key, forbidden)))
                .ToList();

            if (candidates.Count == 0)
            {
                return false;
            }

            var pick = WeightedPick(candidates, random);
            sequence.Add(pick);
            remaining[pick]--;
        }

        return !HasThreeInARow(sequence);
    }

    private static T WeightedPick<T>(List<KeyValuePair<T, int>> candidates, Random random)
    {
        var total = candidates.Sum(c => c.Value);
        var ticket = random.Next(total);
        var running = 0;

        foreach (var candidate in candidates)
        {
            running += candidate.Value;
            if (ticket < running)
            {
                return candidate.Key;
            }
        }

        return candidates[^1].Key;
    }

    private static void Shuffle<T>(IList<T> items, Random random)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
