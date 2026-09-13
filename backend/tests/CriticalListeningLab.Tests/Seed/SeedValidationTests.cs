using CriticalListeningLab.Api.Data.Seed;
using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Domain.Questions;
using CriticalListeningLab.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace CriticalListeningLab.Tests.Seed;

public class SeedValidationTests
{
    [Fact]
    public async Task Seed_is_idempotent_and_has_expected_counts()
    {
        await using var db = await TestDb.CreateSeededInMemoryAsync();
        await CatalogSeeder.EnsureAsync(db);

        (await db.Modules.CountAsync()).ShouldBe(2);
        (await db.AudioSources.CountAsync()).ShouldBe(6);
        (await db.AudioAssets.CountAsync()).ShouldBe(16);
        (await db.ExerciseSegments.CountAsync()).ShouldBe(5);
        (await db.ExerciseLevels.CountAsync()).ShouldBe(19);
        (await db.LevelUnlockRequirements.CountAsync()).ShouldBe(17);
        (await db.ExerciseLevels.CountAsync(l => l.Segment.Module.Slug == "eq")).ShouldBe(16);
        (await db.ExerciseLevels.CountAsync(l => l.Segment.Module.Slug == "compression")).ShouldBe(3);
    }

    [Fact]
    public async Task Every_config_deserializes_for_its_exercise_type()
    {
        await using var db = await TestDb.CreateSeededInMemoryAsync();
        var levels = await db.ExerciseLevels.Include(l => l.Segment).ToListAsync();

        foreach (var level in levels)
        {
            Should.NotThrow(() => ExerciseConfig.Parse(level.ExerciseType, level.ConfigJson));
            if (level.Segment.Key == CatalogSeeder.IntroSegmentKey)
            {
                level.QuestionCount.ShouldBe(CatalogSeeder.IntroQuestionCount);
                level.PassThreshold.ShouldBe(CatalogSeeder.IntroPassThreshold);
            }
            else
            {
                level.QuestionCount.ShouldBe(CatalogSeeder.QuestionCount);
                level.PassThreshold.ShouldBe(CatalogSeeder.PassThreshold);
            }

            level.PassThreshold.ShouldBeLessThanOrEqualTo(level.QuestionCount);
        }
    }

    [Fact]
    public async Task Eq_configs_only_use_allowed_frequencies_and_gains()
    {
        await using var db = await TestDb.CreateSeededInMemoryAsync();
        var levels = await db.ExerciseLevels
            .Where(l => l.ExerciseType != ExerciseType.CompressionChoice)
            .ToListAsync();

        foreach (var level in levels)
        {
            var config = ExerciseConfig.ParseEq(level.ConfigJson);
            config.FrequenciesHz.ShouldAllBe(f => ExerciseLimits.FrequenciesHz.Contains(f));
            config.GainsDb.ShouldAllBe(g => ExerciseLimits.GainsDb.Contains(g));
        }
    }

    [Fact]
    public async Task Prerequisites_belong_to_the_same_module_and_form_no_cycle()
    {
        await using var db = await TestDb.CreateSeededInMemoryAsync();
        var levels = await db.ExerciseLevels
            .Include(l => l.Segment)
            .Include(l => l.UnlockRequirements)
            .ToListAsync();

        foreach (var level in levels)
        {
            foreach (var requirement in level.UnlockRequirements)
            {
                var required = levels.Single(l => l.Id == requirement.RequiredExerciseLevelId);
                required.Segment.ModuleId.ShouldBe(level.Segment.ModuleId);
            }
        }

        HasCycle(levels.ToDictionary(
            l => l.Id,
            l => l.UnlockRequirements.Select(r => r.RequiredExerciseLevelId).ToList()))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Each_module_has_at_least_one_level_without_requirements()
    {
        await using var db = await TestDb.CreateSeededInMemoryAsync();
        var levels = await db.ExerciseLevels
            .Include(l => l.Segment)
            .Include(l => l.UnlockRequirements)
            .ToListAsync();

        levels.Where(l => l.Segment.ModuleId == SeedIds.Module("eq"))
            .ShouldContain(l => l.UnlockRequirements.Count == 0);
        levels.Where(l => l.Segment.ModuleId == SeedIds.Module("compression"))
            .ShouldContain(l => l.UnlockRequirements.Count == 0);
    }

    [Fact]
    public async Task Compression_variants_in_config_exist_for_every_source()
    {
        await using var db = await TestDb.CreateSeededInMemoryAsync();
        var levels = await db.ExerciseLevels
            .Where(l => l.ExerciseType == ExerciseType.CompressionChoice)
            .ToListAsync();
        var sources = await db.AudioSources
            .Where(s => s.ModuleId == SeedIds.Module("compression"))
            .ToListAsync();
        var assets = await db.AudioAssets.ToListAsync();

        foreach (var level in levels)
        {
            var config = ExerciseConfig.ParseCompression(level.ConfigJson);
            foreach (var variant in config.Options.SelectMany(o => o.Variants))
            {
                foreach (var source in sources)
                {
                    assets.ShouldContain(a =>
                        a.AudioSourceId == source.Id && a.VariantSlug == variant && a.IsEnabled);
                }
            }
        }
    }

    private static bool HasCycle(IReadOnlyDictionary<Guid, List<Guid>> graph)
    {
        var state = new Dictionary<Guid, int>();

        bool Dfs(Guid node)
        {
            state[node] = 1;
            foreach (var next in graph.GetValueOrDefault(node) ?? [])
            {
                if (!graph.ContainsKey(next))
                {
                    continue;
                }

                if (state.GetValueOrDefault(next) == 1 || (state.GetValueOrDefault(next) == 0 && Dfs(next)))
                {
                    return true;
                }
            }

            state[node] = 2;
            return false;
        }

        return graph.Keys.Any(node => state.GetValueOrDefault(node) == 0 && Dfs(node));
    }
}
