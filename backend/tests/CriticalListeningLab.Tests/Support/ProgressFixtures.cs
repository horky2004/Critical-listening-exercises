using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Data.Seed;
using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Features.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CriticalListeningLab.Tests.Support;

public static class ProgressFixtures
{
    public static async Task CompleteFrequencyIntroAsync(IProgressionService progression, Guid userId)
    {
        var pink = SeedIds.Source("eq", CatalogSeeder.StarterEqSourceSlug);
        await progression.ApplyTestResultAsync(
            userId, pink, SeedIds.Level("eq", CatalogSeeder.IntroSegmentKey, 1), 0, CancellationToken.None);
        await progression.ApplyTestResultAsync(
            userId, pink, SeedIds.Level("eq", CatalogSeeder.IntroSegmentKey, 2), 0, CancellationToken.None);
        await progression.ApplyTestResultAsync(
            userId, pink, SeedIds.Level("eq", "boost", 1), CatalogSeeder.PassThreshold, CancellationToken.None);
        await progression.ApplyTestResultAsync(
            userId, pink, SeedIds.Level("eq", CatalogSeeder.IntroSegmentKey, 3), 0, CancellationToken.None);
    }

    public static async Task CompleteIntroAAsync(
        IProgressionService progression, Guid userId, Guid? sourceId = null)
    {
        await CompleteFrequencyIntroAsync(progression, userId);
        var source = sourceId ?? SeedIds.Source("eq", "drums");
        if (source == SeedIds.Source("eq", CatalogSeeder.StarterEqSourceSlug))
        {
            return;
        }

        await progression.ApplyTestResultAsync(
            userId, source, SeedIds.Level("eq", CatalogSeeder.IntroSegmentKey, 1), 0, CancellationToken.None);
        await progression.ApplyTestResultAsync(
            userId, source, SeedIds.Level("eq", CatalogSeeder.IntroSegmentKey, 2), 0, CancellationToken.None);
    }

    public static async Task CompleteFrequencyIntroForCurrentStudentAsync(
        IServiceProvider services, HttpClient client)
    {
        (await client.GetAsync("/api/me")).EnsureSuccessStatusCode();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Role == UserRole.Student);
        var progression = scope.ServiceProvider.GetRequiredService<IProgressionService>();
        await CompleteFrequencyIntroAsync(progression, user.Id);
    }

    public static async Task CompleteIntroAForCurrentStudentAsync(
        IServiceProvider services, HttpClient client, string sourceSlug = "drums")
    {
        (await client.GetAsync("/api/me")).EnsureSuccessStatusCode();
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Role == UserRole.Student);
        var progression = scope.ServiceProvider.GetRequiredService<IProgressionService>();
        await CompleteIntroAAsync(progression, user.Id, SeedIds.Source("eq", sourceSlug));
    }
}
