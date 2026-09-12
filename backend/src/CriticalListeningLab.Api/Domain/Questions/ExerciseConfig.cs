using System.Text.Json;
using System.Text.Json.Serialization;

namespace CriticalListeningLab.Api.Domain.Questions;

/// <summary>
/// Dopustene vrijednosti za EQ config. Seed test odbija sve izvan ovih skupova.
/// </summary>
public static class ExerciseLimits
{
    public static readonly IReadOnlySet<int> FrequenciesHz =
        new HashSet<int> { 125, 250, 500, 1000, 2000, 4000, 8000 };

    public static readonly IReadOnlySet<int> GainsDb =
        new HashSet<int> { 12, 9, 6, 3, -3, -6, -9, -12 };

    public const double DefaultQ = 1.0;
}

public sealed record EqLevelConfig(
    IReadOnlyList<int> FrequenciesHz,
    IReadOnlyList<int> GainsDb,
    double Q);

public sealed record CompressionOption(
    string Key,
    string Label,
    IReadOnlyList<string> Variants);

public sealed record CompressionLevelConfig(IReadOnlyList<CompressionOption> Options);

public static class ExerciseConfig
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static EqLevelConfig ParseEq(string json)
    {
        var config = JsonSerializer.Deserialize<EqLevelConfig>(json, JsonOptions)
                     ?? throw new InvalidOperationException("EQ config je prazan.");
        ValidateEq(config);
        return config;
    }

    public static CompressionLevelConfig ParseCompression(string json)
    {
        var config = JsonSerializer.Deserialize<CompressionLevelConfig>(json, JsonOptions)
                     ?? throw new InvalidOperationException("Compression config je prazan.");
        ValidateCompression(config);
        return config;
    }

    public static object Parse(ExerciseType type, string json) => type switch
    {
        ExerciseType.EqFrequency or ExerciseType.EqFrequencyAndDirection => ParseEq(json),
        ExerciseType.CompressionChoice => ParseCompression(json),
        _ => throw new InvalidOperationException($"Nepoznat ExerciseType: {type}")
    };

    public static string SerializeEq(EqLevelConfig config) =>
        JsonSerializer.Serialize(config, JsonOptions);

    public static string SerializeCompression(CompressionLevelConfig config) =>
        JsonSerializer.Serialize(config, JsonOptions);

    public static void ValidateEq(EqLevelConfig config)
    {
        if (config.FrequenciesHz is not { Count: > 0 })
        {
            throw new InvalidOperationException("EQ config mora imati barem jednu frekvenciju.");
        }

        if (config.GainsDb is not { Count: > 0 })
        {
            throw new InvalidOperationException("EQ config mora imati barem jedan gain.");
        }

        if (config.FrequenciesHz.Any(f => !ExerciseLimits.FrequenciesHz.Contains(f)))
        {
            throw new InvalidOperationException(
                $"EQ config sadrzi nedopustenu frekvenciju: {string.Join(",", config.FrequenciesHz)}");
        }

        if (config.GainsDb.Any(g => !ExerciseLimits.GainsDb.Contains(g)))
        {
            throw new InvalidOperationException(
                $"EQ config sadrzi nedopusten gain: {string.Join(",", config.GainsDb)}");
        }

        if (config.Q <= 0)
        {
            throw new InvalidOperationException("EQ Q mora biti veci od nule.");
        }
    }

    public static void ValidateCompression(CompressionLevelConfig config)
    {
        if (config.Options is not { Count: > 0 })
        {
            throw new InvalidOperationException("Compression config mora imati barem jednu opciju.");
        }

        foreach (var option in config.Options)
        {
            if (string.IsNullOrWhiteSpace(option.Key) || string.IsNullOrWhiteSpace(option.Label))
            {
                throw new InvalidOperationException("Compression opcija mora imati key i label.");
            }

            if (option.Variants is not { Count: > 0 })
            {
                throw new InvalidOperationException(
                    $"Compression opcija '{option.Key}' mora imati barem jednu varijantu.");
            }
        }
    }
}
