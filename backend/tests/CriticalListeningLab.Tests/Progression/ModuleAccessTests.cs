using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Data.Seed;
using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Domain.Entities;
using CriticalListeningLab.Api.Features.Modules;
using CriticalListeningLab.Api.Features.Progress;
using CriticalListeningLab.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace CriticalListeningLab.Tests.Progression;

public class ModuleAccessTests
{
    [Fact]
    public async Task Globally_disabled_module_is_unavailable_and_does_not_delete_progress()
    {
        await using var db = await TestDb.CreateSeededInMemoryAsync();
        var user = await AddStudentAsync(db);
        var service = new ProgressionService(db, TimeProvider.System);
        await service.ApplyTestResultAsync(
            user.Id, SeedIds.Source("eq", "drums"), SeedIds.Level("eq", "boost", 1), 14, CancellationToken.None);

        db.Modules.Single(m => m.Slug == "eq").IsEnabledGlobally = false;
        await db.SaveChangesAsync();

        var access = new ModuleAccessService(db);
        (await access.IsModuleAvailableAsync(user.Id, "eq", CancellationToken.None)).ShouldBeFalse();

        (await db.StudentProgress.CountAsync()).ShouldBe(1);

        db.Modules.Single(m => m.Slug == "eq").IsEnabledGlobally = true;
        await db.SaveChangesAsync();

        (await access.IsModuleAvailableAsync(user.Id, "eq", CancellationToken.None)).ShouldBeTrue();
        var tree = await service.GetTreeStateAsync(user.Id, SeedIds.Source("eq", "drums"), CancellationToken.None);
        tree.Segments.Single(s => s.Key == "boost").Levels.Single(l => l.LevelNumber == 1)
            .BestScore.ShouldBe(14);
    }

    [Fact]
    public async Task Cohort_override_false_hides_an_otherwise_enabled_module()
    {
        await using var db = await TestDb.CreateSeededInMemoryAsync();
        var user = await AddStudentAsync(db, assignCohort: true);
        db.CohortModuleAvailabilities.Add(new CohortModuleAvailability
        {
            Id = Guid.CreateVersion7(),
            CohortId = user.CohortId!.Value,
            ModuleId = SeedIds.Module("compression"),
            IsEnabled = false
        });
        await db.SaveChangesAsync();

        var access = new ModuleAccessService(db);
        (await access.IsModuleAvailableAsync(user.Id, "eq", CancellationToken.None)).ShouldBeTrue();
        (await access.IsModuleAvailableAsync(user.Id, "compression", CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task Musical_eq_sources_wait_for_pink_noise_intro()
    {
        await using var db = await TestDb.CreateSeededInMemoryAsync();
        var user = await AddStudentAsync(db);
        var access = new ModuleAccessService(db);

        (await access.IsSourceAvailableAsync(user.Id, "eq", "pink-noise", CancellationToken.None))
            .ShouldBeTrue();
        (await access.IsSourceAvailableAsync(user.Id, "eq", "drums", CancellationToken.None))
            .ShouldBeFalse();
        (await access.IsSourceAvailableAsync(user.Id, "compression", "drums", CancellationToken.None))
            .ShouldBeTrue();

        await ProgressFixtures.CompleteFrequencyIntroAsync(new ProgressionService(db, TimeProvider.System), user.Id);

        (await access.IsSourceAvailableAsync(user.Id, "eq", "drums", CancellationToken.None))
            .ShouldBeTrue();
        (await access.IsSourceAvailableAsync(user.Id, "eq", "vocal", CancellationToken.None))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Source_must_belong_to_the_named_module()
    {
        await using var db = await TestDb.CreateSeededInMemoryAsync();
        var access = new ModuleAccessService(db);

        var eqDrums = await access.FindSourceAsync("eq", "drums", CancellationToken.None);
        var compressionDrums = await access.FindSourceAsync("compression", "drums", CancellationToken.None);
        var missing = await access.FindSourceAsync("eq", "does-not-exist", CancellationToken.None);

        eqDrums.ShouldNotBeNull();
        compressionDrums.ShouldNotBeNull();
        eqDrums.Id.ShouldNotBe(compressionDrums.Id);
        missing.ShouldBeNull();
    }

    private static async Task<User> AddStudentAsync(AppDbContext db, bool assignCohort = false)
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            EntraObjectId = Guid.NewGuid().ToString(),
            Email = $"{Guid.NewGuid():N}@student.algebra.hr",
            DisplayName = "Student",
            Role = UserRole.Student,
            CohortId = assignCohort ? SeedIds.Cohort(CatalogSeeder.CohortName) : null,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
