using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Data.Seed;
using CriticalListeningLab.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CriticalListeningLab.Api.Features.Modules;

/// <summary>
/// Efektivna dostupnost: <c>IsEnabledGlobally &amp;&amp; (override ?? true)</c>.
/// Gasenje nikad ne brise napredak.
/// </summary>
public class ModuleAccessService(AppDbContext db) : IModuleAccessService
{
    public async Task<bool> IsModuleAvailableAsync(Guid userId, string moduleSlug, CancellationToken ct)
    {
        var module = await db.Modules.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Slug == moduleSlug, ct);

        if (module is null || !module.IsEnabledGlobally)
        {
            return false;
        }

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return false;
        }

        // Admin i korisnik bez cohorta vide module po globalnoj dostupnosti.
        if (user.Role == UserRole.Admin || user.CohortId is null)
        {
            return true;
        }

        var availability = await db.CohortModuleAvailabilities.AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.CohortId == user.CohortId && a.ModuleId == module.Id, ct);

        return availability?.IsEnabled ?? true;
    }

    public async Task<bool> IsSourceAvailableAsync(
        Guid userId, string moduleSlug, string sourceSlug, CancellationToken ct)
    {
        if (!CatalogSeeder.IsMusicalEqSource(moduleSlug, sourceSlug))
        {
            return true;
        }

        var pinkNoiseId = SeedIds.Source("eq", CatalogSeeder.StarterEqSourceSlug);
        var introDone = SeedIds.Level("eq", CatalogSeeder.IntroSegmentKey, 3);
        if (await db.StudentProgress.AsNoTracking().AnyAsync(
                p => p.UserId == userId
                     && p.AudioSourceId == pinkNoiseId
                     && p.ExerciseLevelId == introDone
                     && p.IsPassed,
                ct))
        {
            return true;
        }

        var sourceId = SeedIds.Source(moduleSlug, sourceSlug);
        return await db.StudentProgress.AsNoTracking()
            .AnyAsync(p => p.UserId == userId && p.AudioSourceId == sourceId, ct);
    }

    public Task<Domain.Entities.AudioSource?> FindSourceAsync(
        string moduleSlug, string sourceSlug, CancellationToken ct) =>
        db.AudioSources.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Slug == sourceSlug && s.Module.Slug == moduleSlug && s.IsEnabled, ct);
}
