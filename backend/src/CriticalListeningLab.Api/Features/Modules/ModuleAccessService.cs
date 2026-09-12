using CriticalListeningLab.Api.Data;
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

    public Task<Domain.Entities.AudioSource?> FindSourceAsync(
        string moduleSlug, string sourceSlug, CancellationToken ct) =>
        db.AudioSources.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Slug == sourceSlug && s.Module.Slug == moduleSlug && s.IsEnabled, ct);
}
