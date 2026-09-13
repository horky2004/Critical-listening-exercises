using CriticalListeningLab.Api.Data.Seed;
using CriticalListeningLab.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace CriticalListeningLab.Tests.Audio;

public class CompressionAudioFilesTests
{
    [Fact]
    public async Task Compression_files_exist_and_durations_match_within_source()
    {
        var root = LocalAudioRoot.Find();
        root.ShouldNotBeNull(
            "Phase 8 zahtijeva compression fajlove u backend/src/CriticalListeningLab.Api/wwwroot/audio.");

        await using var db = await TestDb.CreateSeededInMemoryAsync();
        var assets = await db.AudioAssets
            .Include(asset => asset.AudioSource)
            .Where(asset => asset.AudioSource.ModuleId == SeedIds.Module("compression"))
            .ToListAsync();

        assets.Count.ShouldBe(12);

        foreach (var asset in assets)
        {
            var path = Path.Combine(root, asset.StorageKey.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(path).ShouldBeTrue($"Nedostaje {asset.StorageKey}");
        }

        foreach (var group in assets.GroupBy(asset => asset.AudioSource.Slug))
        {
            group.Select(asset => asset.DurationMs).Distinct().Count()
                .ShouldBe(1, $"DurationMs se razlikuje unutar izvora {group.Key}.");
            group.Select(asset => new FileInfo(
                    Path.Combine(root, asset.StorageKey.Replace('/', Path.DirectorySeparatorChar)))
                .Length)
                .Distinct()
                .Count()
                .ShouldBe(1, $"Velicina fajla se razlikuje unutar izvora {group.Key}.");

            var expected = group.Key == "drums"
                ? CatalogSeeder.CompressionDrumsDurationMs
                : CatalogSeeder.CompressionVocalDurationMs;
            group.First().DurationMs.ShouldBe(expected);
        }
    }
}
