using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CriticalListeningLab.Tests.Data;

public class AppDbContextModelTests
{
    private static IModel CreateModel()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused")
            .UseSnakeCaseNamingConvention()
            .Options;

        using var db = new AppDbContext(options);
        return db.Model;
    }

    [Fact]
    public void Model_builds_and_create_script_contains_all_twelve_tables()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused")
            .UseSnakeCaseNamingConvention()
            .Options;

        using var db = new AppDbContext(options);
        var script = db.Database.GenerateCreateScript();

        string[] tables =
        [
            "cohorts",
            "users",
            "modules",
            "cohort_module_availabilities",
            "audio_sources",
            "audio_assets",
            "exercise_segments",
            "exercise_levels",
            "level_unlock_requirements",
            "student_progress",
            "test_sessions",
            "test_session_questions"
        ];

        foreach (var table in tables)
        {
            var quoted = script.Contains($"CREATE TABLE \"{table}\"", StringComparison.OrdinalIgnoreCase);
            var bare = script.Contains($"CREATE TABLE {table}", StringComparison.OrdinalIgnoreCase);
            (quoted || bare).ShouldBeTrue($"Skripta mora kreirati tablicu {table}");
        }

        script.ShouldContain("jsonb");
        script.ShouldContain("timestamp with time zone");
    }

    [Fact]
    public void Unique_indexes_from_the_spec_are_present()
    {
        var model = CreateModel();

        AssertUnique(model, typeof(User), nameof(User.EntraObjectId));
        AssertUnique(model, typeof(User), nameof(User.Email));
        AssertUnique(model, typeof(Cohort), nameof(Cohort.Name));
        AssertUnique(model, typeof(Module), nameof(Module.Slug));
        AssertUnique(model, typeof(AudioSource), nameof(AudioSource.ModuleId), nameof(AudioSource.Slug));
        AssertUnique(model, typeof(AudioAsset), nameof(AudioAsset.AudioSourceId), nameof(AudioAsset.VariantSlug));
        AssertUnique(model, typeof(ExerciseSegment), nameof(ExerciseSegment.ModuleId), nameof(ExerciseSegment.Key));
        AssertUnique(model, typeof(ExerciseLevel), nameof(ExerciseLevel.SegmentId), nameof(ExerciseLevel.LevelNumber));
        AssertUnique(model, typeof(CohortModuleAvailability),
            nameof(CohortModuleAvailability.CohortId), nameof(CohortModuleAvailability.ModuleId));
        AssertUnique(model, typeof(LevelUnlockRequirement),
            nameof(LevelUnlockRequirement.ExerciseLevelId),
            nameof(LevelUnlockRequirement.RequiredExerciseLevelId));
        AssertUnique(model, typeof(StudentProgress),
            nameof(StudentProgress.UserId),
            nameof(StudentProgress.AudioSourceId),
            nameof(StudentProgress.ExerciseLevelId));
        AssertUnique(model, typeof(TestSessionQuestion),
            nameof(TestSessionQuestion.TestSessionId), nameof(TestSessionQuestion.QuestionIndex));
        AssertUnique(model, typeof(TestSessionQuestion), nameof(TestSessionQuestion.AudioToken));
    }

    [Fact]
    public void Active_cohort_has_filtered_unique_index()
    {
        var index = CreateModel()
            .FindEntityType(typeof(Cohort))!
            .GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(Cohort.IsActive)]));

        index.IsUnique.ShouldBeTrue();
        index.GetFilter().ShouldBe("is_active");
    }

    [Fact]
    public void Config_and_prompt_columns_are_jsonb()
    {
        var model = CreateModel();

        model.FindEntityType(typeof(ExerciseLevel))!
            .FindProperty(nameof(ExerciseLevel.ConfigJson))!
            .GetColumnType()
            .ShouldBe("jsonb");

        model.FindEntityType(typeof(TestSessionQuestion))!
            .FindProperty(nameof(TestSessionQuestion.PromptJson))!
            .GetColumnType()
            .ShouldBe("jsonb");
    }

    [Fact]
    public void ScorePercentage_is_not_mapped()
    {
        CreateModel()
            .FindEntityType(typeof(TestSession))!
            .FindProperty(nameof(TestSession.ScorePercentage))
            .ShouldBeNull();
    }

    private static void AssertUnique(IModel model, Type entityType, params string[] propertyNames)
    {
        var indexes = model.FindEntityType(entityType)!
            .GetIndexes()
            .Where(i => i.IsUnique && i.Properties.Select(p => p.Name).SequenceEqual(propertyNames))
            .ToList();

        indexes.Count.ShouldBe(1,
            $"Ocekivan unique indeks na {entityType.Name}({string.Join(", ", propertyNames)})");
    }
}
