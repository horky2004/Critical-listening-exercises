using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Domain.Entities;
using CriticalListeningLab.Api.Domain.Questions;
using Microsoft.EntityFrameworkCore;

namespace CriticalListeningLab.Api.Data.Seed;

public static class CatalogSeeder
{
    public const string CohortName = "2025/26";
    public const string IntroSegmentKey = "intro";
    public const string StarterEqSourceSlug = "pink-noise";
    public static readonly string[] MusicalEqSourceSlugs = ["drums", "acoustic-guitar", "vocal"];
    public const int QuestionCount = 20;
    public const int PassThreshold = 16;
    public const int IntroQuestionCount = 6;
    public const int IntroPassThreshold = 0;
    public const int CompressionDetection1QuestionCount = 10;
    public const int CompressionDetection1PassThreshold = 8;

    public static readonly string[] CompressionVariantOrder =
        ["uncompressed", "light", "heavy", "ratio-2", "ratio-4", "ratio-12"];

    public const int CompressionDrumsDurationMs = 9822;
    public const int CompressionVocalDurationMs = 15882;

    private static readonly int[] All7 = [125, 250, 500, 1000, 2000, 4000, 8000];
    private static readonly int[] BoostL1Freq = [125, 500, 2000, 8000];

    public static async Task EnsureAsync(AppDbContext db, CancellationToken ct = default)
    {
        Upsert(db.Cohorts, new Cohort
        {
            Id = SeedIds.Cohort(CohortName),
            Name = CohortName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UnixEpoch
        });

        UpsertModule(db, "eq", "Ekvalizacija", "Vježbe prepoznavanja frekvencijskih promjena", 1);
        UpsertModule(db, "compression", "Kompresija", "Vježbe prepoznavanja dinamičke obrade", 2);

        SeedEq(db);
        SeedCompression(db);

        await db.SaveChangesAsync(ct);
        await BackfillIntroForExistingPassesAsync(db, ct);
    }

    /// <summary>
    /// Studenti koji su vec polozili BOOST L1/L2 prije uvoda ne smiju ostati zakljucani.
    /// </summary>
    private static async Task BackfillIntroForExistingPassesAsync(AppDbContext db, CancellationToken ct)
    {
        var intro1 = SeedIds.Level("eq", IntroSegmentKey, 1);
        var intro2 = SeedIds.Level("eq", IntroSegmentKey, 2);
        var intro3 = SeedIds.Level("eq", IntroSegmentKey, 3);
        var boost1 = SeedIds.Level("eq", "boost", 1);
        var boost2 = SeedIds.Level("eq", "boost", 2);

        var passed = await db.StudentProgress.AsNoTracking()
            .Where(p => p.IsPassed && (p.ExerciseLevelId == boost1 || p.ExerciseLevelId == boost2))
            .ToListAsync(ct);
        if (passed.Count == 0)
        {
            return;
        }

        var existing = (await db.StudentProgress.AsNoTracking()
                .Where(p => p.ExerciseLevelId == intro1 || p.ExerciseLevelId == intro2 || p.ExerciseLevelId == intro3)
                .Select(p => new { p.UserId, p.AudioSourceId, p.ExerciseLevelId })
                .ToListAsync(ct))
            .Select(p => (p.UserId, p.AudioSourceId, p.ExerciseLevelId))
            .ToHashSet();

        var now = DateTimeOffset.UtcNow;
        foreach (var row in passed)
        {
            Ensure(intro1, row);
            Ensure(intro2, row);
            if (row.ExerciseLevelId == boost2)
            {
                Ensure(intro3, row);
            }
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(ct);
        }

        void Ensure(Guid levelId, StudentProgress row)
        {
            if (!existing.Add((row.UserId, row.AudioSourceId, levelId)))
            {
                return;
            }

            db.StudentProgress.Add(new StudentProgress
            {
                Id = Guid.CreateVersion7(),
                UserId = row.UserId,
                AudioSourceId = row.AudioSourceId,
                ExerciseLevelId = levelId,
                BestScore = 0,
                IsPassed = true,
                AttemptCount = 1,
                FirstPassedAt = now,
                LastAttemptAt = now
            });
        }
    }

    private static void SeedEq(AppDbContext db)
    {
        SeedSource(db, "eq", "pink-noise", "Ružičasti šum", 1);
        SeedSource(db, "eq", "drums", "Bubnjevi", 2);
        SeedSource(db, "eq", "acoustic-guitar", "Akustična gitara", 3);
        SeedSource(db, "eq", "vocal", "Vokal", 4);

        SeedAsset(db, "eq", "pink-noise", "full", "eq/pink-noise.flac", "audio/flac", 9746);
        SeedAsset(db, "eq", "drums", "full", "eq/drums.flac", "audio/flac", 9771);
        SeedAsset(db, "eq", "acoustic-guitar", "full", "eq/acoustic-guitar.flac", "audio/flac", 19492);
        SeedAsset(db, "eq", "vocal", "full", "eq/vocal.flac", "audio/flac", 15838);

        var intro = SeedSegment(db, "eq", IntroSegmentKey, "Upoznavanje", 0);
        var boost = SeedSegment(db, "eq", "boost", "Boost", 1);
        var cut = SeedSegment(db, "eq", "cut", "Cut", 2);
        var combined = SeedSegment(db, "eq", "combined", "Kombinirano", 3);

        SeedEqLevel(db, intro, 1, "125 Hz ili 500 Hz", ExerciseType.EqFrequency, [125, 500], [12],
            IntroQuestionCount, IntroPassThreshold);
        SeedEqLevel(db, intro, 2, "2 kHz ili 8 kHz", ExerciseType.EqFrequency, [2000, 8000], [12],
            IntroQuestionCount, IntroPassThreshold);
        SeedEqLevel(db, intro, 3, "250 Hz, 1 kHz ili 4 kHz", ExerciseType.EqFrequency, [250, 1000, 4000], [12],
            IntroQuestionCount, IntroPassThreshold);

        SeedEqLevel(db, boost, 1, "Boost +12 dB", ExerciseType.EqFrequency, BoostL1Freq, [12]);
        SeedEqLevel(db, boost, 2, "Boost +12 dB", ExerciseType.EqFrequency, All7, [12]);
        SeedEqLevel(db, boost, 3, "Boost +9 dB", ExerciseType.EqFrequency, All7, [9]);
        SeedEqLevel(db, boost, 4, "Boost +6 dB", ExerciseType.EqFrequency, All7, [6]);
        SeedEqLevel(db, boost, 5, "Boost +3 dB", ExerciseType.EqFrequency, All7, [3]);

        SeedEqLevel(db, cut, 1, "Cut -12 dB", ExerciseType.EqFrequency, All7, [-12]);
        SeedEqLevel(db, cut, 2, "Cut -9 dB", ExerciseType.EqFrequency, All7, [-9]);
        SeedEqLevel(db, cut, 3, "Cut -6 dB", ExerciseType.EqFrequency, All7, [-6]);
        SeedEqLevel(db, cut, 4, "Cut -3 dB", ExerciseType.EqFrequency, All7, [-3]);

        SeedEqLevel(db, combined, 1, "Kombinirano +/-12 dB", ExerciseType.EqFrequencyAndDirection, All7, [12, -12]);
        SeedEqLevel(db, combined, 2, "Kombinirano +/-9 dB", ExerciseType.EqFrequencyAndDirection, All7, [9, -9]);
        SeedEqLevel(db, combined, 3, "Kombinirano +/-6 dB", ExerciseType.EqFrequencyAndDirection, All7, [6, -6]);
        SeedEqLevel(db, combined, 4, "Kombinirano +/-3 dB", ExerciseType.EqFrequencyAndDirection, All7, [3, -3]);

        Require(db, "eq", IntroSegmentKey, 2, "eq", IntroSegmentKey, 1);
        Require(db, "eq", "boost", 1, "eq", IntroSegmentKey, 2);
        Require(db, "eq", IntroSegmentKey, 3, "eq", "boost", 1);
        Require(db, "eq", "boost", 2, "eq", IntroSegmentKey, 3);
        Require(db, "eq", "boost", 3, "eq", "boost", 2);
        Require(db, "eq", "boost", 4, "eq", "boost", 3);
        Require(db, "eq", "boost", 5, "eq", "boost", 4);
        Require(db, "eq", "cut", 1, "eq", "boost", 3);
        Require(db, "eq", "cut", 2, "eq", "cut", 1);
        Require(db, "eq", "cut", 3, "eq", "cut", 2);
        Require(db, "eq", "cut", 4, "eq", "cut", 3);
        Require(db, "eq", "combined", 1, "eq", "cut", 3);
        Require(db, "eq", "combined", 2, "eq", "combined", 1);
        Require(db, "eq", "combined", 3, "eq", "combined", 2);
        Require(db, "eq", "combined", 4, "eq", "combined", 3);
    }

    private static void SeedCompression(AppDbContext db)
    {
        SeedSource(db, "compression", "drums", "Bubnjevi", 1);
        SeedSource(db, "compression", "vocal", "Vokal", 2);

        foreach (var source in new[] { "drums", "vocal" })
        {
            var durationMs = source == "drums" ? CompressionDrumsDurationMs : CompressionVocalDurationMs;
            foreach (var variant in CompressionVariantOrder)
            {
                SeedAsset(db, "compression", source, variant,
                    $"compression/{source}/{variant}.mp3", "audio/mpeg", durationMs);
            }
        }

        var detection = SeedSegment(db, "compression", "detection", "Prepoznavanje", 1);

        SeedCompressionLevel(db, detection, 1, "Je li signal komprimiran?",
            new CompressionLevelConfig(
            [
                new("uncompressed", "Nije komprimiran", ["uncompressed"]),
                new("compressed", "Komprimiran", ["heavy"])
            ]),
            CompressionDetection1QuestionCount,
            CompressionDetection1PassThreshold);

        SeedCompressionLevel(db, detection, 2, "Koliko je signal komprimiran?",
            new CompressionLevelConfig(
            [
                new("uncompressed", "Nekomprimiran", ["uncompressed"]),
                new("light", "Lagano komprimiran", ["light"]),
                new("heavy", "Jako komprimiran", ["heavy"])
            ]));

        SeedCompressionLevel(db, detection, 3, "Koji je omjer kompresije?",
            new CompressionLevelConfig(
            [
                new("ratio-2", "2:1", ["ratio-2"]),
                new("ratio-4", "4:1", ["ratio-4"]),
                new("ratio-12", "12:1", ["ratio-12"])
            ]));

        Require(db, "compression", "detection", 2, "compression", "detection", 1);
        Require(db, "compression", "detection", 3, "compression", "detection", 2);
    }

    private static void UpsertModule(AppDbContext db, string slug, string name, string description, int sort)
    {
        Upsert(db.Modules, new Module
        {
            Id = SeedIds.Module(slug),
            Slug = slug,
            Name = name,
            Description = description,
            SortOrder = sort,
            IsEnabledGlobally = true
        });
    }

    private static void SeedSource(AppDbContext db, string module, string slug, string name, int sort)
    {
        Upsert(db.AudioSources, new AudioSource
        {
            Id = SeedIds.Source(module, slug),
            ModuleId = SeedIds.Module(module),
            Slug = slug,
            Name = name,
            SortOrder = sort,
            IsEnabled = true
        });
    }

    private static void SeedAsset(
        AppDbContext db, string module, string source, string variant,
        string storageKey, string mime, int durationMs)
    {
        Upsert(db.AudioAssets, new AudioAsset
        {
            Id = SeedIds.Asset(module, source, variant),
            AudioSourceId = SeedIds.Source(module, source),
            VariantSlug = variant,
            StorageKey = storageKey,
            MimeType = mime,
            DurationMs = durationMs,
            SampleRate = 44100,
            IsEnabled = true
        });
    }

    private static ExerciseSegment SeedSegment(AppDbContext db, string module, string key, string name, int sort)
    {
        var segment = new ExerciseSegment
        {
            Id = SeedIds.Segment(module, key),
            ModuleId = SeedIds.Module(module),
            Key = key,
            Name = name,
            SortOrder = sort
        };
        Upsert(db.ExerciseSegments, segment);
        return segment;
    }

    public static bool IsClassicSegment(string key) => key != IntroSegmentKey;

    public static bool FrequencyIntroAppliesTo(string moduleSlug, string sourceSlug) =>
        moduleSlug == "eq" && sourceSlug == StarterEqSourceSlug;

    public static bool IsMusicalEqSource(string moduleSlug, string sourceSlug) =>
        moduleSlug == "eq" && MusicalEqSourceSlugs.Contains(sourceSlug);

    private static void SeedEqLevel(
        AppDbContext db, ExerciseSegment segment, int number, string title,
        ExerciseType type, int[] frequencies, int[] gains,
        int? questionCount = null, int? passThreshold = null)
    {
        var config = new EqLevelConfig(frequencies, gains, ExerciseLimits.DefaultQ);
        Upsert(db.ExerciseLevels, new ExerciseLevel
        {
            Id = SeedIds.Level(ModuleSlug(segment), segment.Key, number),
            SegmentId = segment.Id,
            LevelNumber = number,
            Title = title,
            ExerciseType = type,
            ConfigJson = ExerciseConfig.SerializeEq(config),
            QuestionCount = questionCount ?? QuestionCount,
            PassThreshold = passThreshold ?? PassThreshold,
            IsEnabled = true
        });
    }

    private static void SeedCompressionLevel(
        AppDbContext db, ExerciseSegment segment, int number, string title, CompressionLevelConfig config,
        int? questionCount = null, int? passThreshold = null)
    {
        Upsert(db.ExerciseLevels, new ExerciseLevel
        {
            Id = SeedIds.Level("compression", segment.Key, number),
            SegmentId = segment.Id,
            LevelNumber = number,
            Title = title,
            ExerciseType = ExerciseType.CompressionChoice,
            ConfigJson = ExerciseConfig.SerializeCompression(config),
            QuestionCount = questionCount ?? QuestionCount,
            PassThreshold = passThreshold ?? PassThreshold,
            IsEnabled = true
        });
    }

    private static void Require(
        AppDbContext db,
        string module, string segment, int level,
        string requiredModule, string requiredSegment, int requiredLevel)
    {
        Upsert(db.LevelUnlockRequirements, new LevelUnlockRequirement
        {
            Id = SeedIds.Unlock(module, segment, level),
            ExerciseLevelId = SeedIds.Level(module, segment, level),
            RequiredExerciseLevelId = SeedIds.Level(requiredModule, requiredSegment, requiredLevel),
            RequirementType = UnlockRequirementType.PassedLevel
        });
    }

    private static string ModuleSlug(ExerciseSegment segment) =>
        segment.ModuleId == SeedIds.Module("eq") ? "eq" : "compression";

    private static void Upsert<T>(DbSet<T> set, T entity) where T : class
    {
        var existing = set.Local.FirstOrDefault(e =>
            set.EntityType.FindPrimaryKey()!.Properties
                .All(p => Equals(p.PropertyInfo!.GetValue(e), p.PropertyInfo!.GetValue(entity))));

        if (existing is not null)
        {
            return;
        }

        var id = set.EntityType.FindPrimaryKey()!.Properties[0].PropertyInfo!.GetValue(entity);
        var tracked = set.Find(id);
        if (tracked is null)
        {
            set.Add(entity);
            return;
        }

        set.Entry(tracked).CurrentValues.SetValues(entity);
    }
}
