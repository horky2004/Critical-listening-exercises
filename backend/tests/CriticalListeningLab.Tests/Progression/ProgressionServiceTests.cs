using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Data.Seed;
using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Domain.Entities;
using CriticalListeningLab.Api.Domain.Progression;
using CriticalListeningLab.Api.Features.Progress;
using CriticalListeningLab.Tests.Support;

namespace CriticalListeningLab.Tests.Progression;

public class ProgressionServiceTests
{
    [Fact]
    public async Task Apply_returns_only_newly_unlocked_levels()
    {
        await using var db = await TestDb.CreateSeededInMemoryAsync();
        var user = await AddUserAsync(db);
        var drums = SeedIds.Source("eq", "drums");
        var service = new ProgressionService(db, TimeProvider.System);

        await PassAsync(service, user, drums, "boost", 1);
        await PassAsync(service, user, drums, "boost", 2);
        var boost3 = await service.ApplyTestResultAsync(
            user, drums, SeedIds.Level("eq", "boost", 3), 12, CancellationToken.None);

        boost3.Passed.ShouldBeTrue();
        boost3.IsFirstPass.ShouldBeTrue();
        boost3.NewlyUnlockedLevels.Select(l => l.LevelId).ShouldBe(
            [SeedIds.Level("eq", "boost", 4), SeedIds.Level("eq", "cut", 1)],
            ignoreOrder: true);

        var again = await service.ApplyTestResultAsync(
            user, drums, SeedIds.Level("eq", "boost", 3), 13, CancellationToken.None);

        again.IsFirstPass.ShouldBeFalse();
        again.NewlyUnlockedLevels.ShouldBeEmpty();
    }

    [Fact]
    public async Task Student_A_progress_does_not_unlock_levels_for_student_B()
    {
        await using var db = await TestDb.CreateSeededInMemoryAsync();
        var a = await AddUserAsync(db, "a@student.algebra.hr");
        var b = await AddUserAsync(db, "b@student.algebra.hr");
        var drums = SeedIds.Source("eq", "drums");
        var service = new ProgressionService(db, TimeProvider.System);

        await PassThroughBoost3(service, a, drums);

        (await service.IsUnlockedAsync(b, drums, SeedIds.Level("eq", "cut", 1), CancellationToken.None))
            .ShouldBeFalse();
        (await service.IsUnlockedAsync(a, drums, SeedIds.Level("eq", "cut", 1), CancellationToken.None))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Drums_progress_does_not_unlock_the_same_level_on_Vocal()
    {
        await using var db = await TestDb.CreateSeededInMemoryAsync();
        var user = await AddUserAsync(db);
        var service = new ProgressionService(db, TimeProvider.System);

        await PassThroughBoost3(service, user, SeedIds.Source("eq", "drums"));

        (await service.IsUnlockedAsync(user, SeedIds.Source("eq", "vocal"), SeedIds.Level("eq", "cut", 1), CancellationToken.None))
            .ShouldBeFalse();
        (await service.IsUnlockedAsync(user, SeedIds.Source("eq", "drums"), SeedIds.Level("eq", "cut", 1), CancellationToken.None))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Same_level_keeps_independent_BestScore_per_source()
    {
        await using var db = await TestDb.CreateSeededInMemoryAsync();
        var user = await AddUserAsync(db);
        var service = new ProgressionService(db, TimeProvider.System);
        var level = SeedIds.Level("eq", "boost", 1);

        await service.ApplyTestResultAsync(user, SeedIds.Source("eq", "drums"), level, 14, CancellationToken.None);
        await service.ApplyTestResultAsync(user, SeedIds.Source("eq", "vocal"), level, 12, CancellationToken.None);

        var drums = await service.GetTreeStateAsync(user, SeedIds.Source("eq", "drums"), CancellationToken.None);
        var vocal = await service.GetTreeStateAsync(user, SeedIds.Source("eq", "vocal"), CancellationToken.None);

        Find(drums, "boost", 1).BestScore.ShouldBe(14);
        Find(vocal, "boost", 1).BestScore.ShouldBe(12);
    }

    [Fact]
    public async Task Eq_progress_does_not_affect_Compression_even_when_source_slug_is_drums()
    {
        await using var db = await TestDb.CreateSeededInMemoryAsync();
        var user = await AddUserAsync(db);
        var service = new ProgressionService(db, TimeProvider.System);

        await PassThroughBoost3(service, user, SeedIds.Source("eq", "drums"));

        var compression = await service.GetTreeStateAsync(
            user, SeedIds.Source("compression", "drums"), CancellationToken.None);

        Find(compression, "detection", 1).Status.ShouldBe(LevelStatus.Unlocked);
        Find(compression, "detection", 2).Status.ShouldBe(LevelStatus.Locked);
    }

    [Fact]
    public async Task Locked_level_is_not_unlocked()
    {
        await using var db = await TestDb.CreateSeededInMemoryAsync();
        var user = await AddUserAsync(db);
        var service = new ProgressionService(db, TimeProvider.System);

        (await service.IsUnlockedAsync(
                user, SeedIds.Source("eq", "drums"), SeedIds.Level("eq", "combined", 1), CancellationToken.None))
            .ShouldBeFalse();
    }

    private static async Task PassThroughBoost3(ProgressionService service, Guid user, Guid source)
    {
        await PassAsync(service, user, source, "boost", 1);
        await PassAsync(service, user, source, "boost", 2);
        await PassAsync(service, user, source, "boost", 3);
    }

    private static Task<ProgressUpdateResult> PassAsync(
        ProgressionService service, Guid user, Guid source, string segment, int level) =>
        service.ApplyTestResultAsync(user, source, SeedIds.Level("eq", segment, level), 12, CancellationToken.None);

    private static LevelState Find(SourceTreeState tree, string segment, int number) =>
        tree.Segments.Single(s => s.Key == segment).Levels.Single(l => l.LevelNumber == number);

    private static async Task<Guid> AddUserAsync(AppDbContext db, string email = "ana@student.algebra.hr")
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            EntraObjectId = Guid.NewGuid().ToString(),
            Email = email,
            DisplayName = email,
            Role = UserRole.Student,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }
}
