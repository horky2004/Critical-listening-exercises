using System.Text.Json;
using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Data.Seed;
using CriticalListeningLab.Api.Domain.Questions;
using Microsoft.EntityFrameworkCore;

namespace CriticalListeningLab.Tests.Seed;

/// <summary>
/// Seed na pravom PostgreSQL-u zbog <c>jsonb</c>. Koristi bazu <c>cll_test</c>
/// i connection string iz user-secrets (ista lozinka kao <c>cll_dev</c>).
/// </summary>
public class PostgresSeedTests
{
    [Fact]
    public async Task Seed_roundtrips_jsonb_configs_on_postgres()
    {
        var connection = TryTestConnectionString()
                         ?? throw new InvalidOperationException(
                             "Nema user-secrets connection stringa. Phase 2 mora biti dovrsena (cll_dev u user-secrets).");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await CatalogSeeder.EnsureAsync(db);

        var levels = await db.ExerciseLevels.AsNoTracking().ToListAsync();
        levels.Count.ShouldBe(16);

        foreach (var level in levels)
        {
            Should.NotThrow(() => ExerciseConfig.Parse(level.ExerciseType, level.ConfigJson));
        }

        (await db.LevelUnlockRequirements.CountAsync()).ShouldBe(14);
    }

    private static string? TryTestConnectionString()
    {
        var secretsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft", "UserSecrets", "92c63c17-a51b-4fee-bae8-01057e9ab9a3", "secrets.json");

        if (!File.Exists(secretsPath))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(secretsPath));
        if (!doc.RootElement.TryGetProperty("ConnectionStrings:Database", out var value))
        {
            return null;
        }

        var connection = value.GetString();
        return string.IsNullOrWhiteSpace(connection)
            ? null
            : connection.Replace("Database=cll_dev", "Database=cll_test", StringComparison.OrdinalIgnoreCase);
    }
}
