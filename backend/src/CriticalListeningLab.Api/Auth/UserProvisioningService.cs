using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CriticalListeningLab.Api.Auth;

public interface IUserProvisioningService
{
    Task<User> GetOrCreateAsync(
        string entraObjectId,
        string email,
        string displayName,
        CancellationToken ct);
}

/// <summary>
/// Just-in-time provisioning: nema registracije ni pozivnica, korisnik se
/// kreira pri prvoj autentificiranoj requesti.
/// <para>
/// Identitet je <c>EntraObjectId</c>, a ne e-mail, pa promjena e-maila
/// (npr. promjena prezimena) ne stvara novog korisnika i ne gubi napredak.
/// </para>
/// </summary>
public class UserProvisioningService(
    AppDbContext db,
    IOptions<AuthOptions> authOptions,
    TimeProvider timeProvider,
    ILogger<UserProvisioningService> logger) : IUserProvisioningService
{
    /// <summary>
    /// <c>LastLoginAt</c> se ne osvjezava pri svakom zahtjevu, nego najvise
    /// jednom u ovom intervalu - inace bi svaki GET bio i UPDATE.
    /// </summary>
    private static readonly TimeSpan LastLoginRefreshInterval = TimeSpan.FromHours(1);

    private readonly AuthOptions _options = authOptions.Value;

    public async Task<User> GetOrCreateAsync(
        string entraObjectId,
        string email,
        string displayName,
        CancellationToken ct)
    {
        var normalizedEmail = EmailIdentity.Normalize(email);
        var now = timeProvider.GetUtcNow();

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.EntraObjectId == entraObjectId, ct);

        if (user is null)
        {
            user = await CreateAsync(entraObjectId, normalizedEmail, displayName, now, ct);
            logger.LogInformation(
                "Kreiran korisnik {Email} s rolom {Role}", user.Email, user.Role);
            return user;
        }

        var changed = false;

        if (user.Email != normalizedEmail)
        {
            user.Email = normalizedEmail;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(displayName) && user.DisplayName != displayName)
        {
            user.DisplayName = displayName;
            changed = true;
        }

        if (user.LastLoginAt is null || now - user.LastLoginAt.Value > LastLoginRefreshInterval)
        {
            user.LastLoginAt = now;
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync(ct);
        }

        return user;
    }

    private async Task<User> CreateAsync(
        string entraObjectId,
        string normalizedEmail,
        string displayName,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var isAdmin = _options.AdminEmails.Any(configured =>
            string.Equals(
                EmailIdentity.Normalize(configured), normalizedEmail, StringComparison.Ordinal));

        var role = isAdmin ? UserRole.Admin : UserRole.Student;

        // Admini nemaju cohort - dostupnost modula im se ne ogranicava.
        Guid? cohortId = null;
        if (role == UserRole.Student)
        {
            cohortId = await db.Cohorts
                .Where(c => c.IsActive)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(ct);
        }

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            EntraObjectId = entraObjectId,
            Email = normalizedEmail,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalizedEmail : displayName,
            Role = role,
            CohortId = cohortId,
            CreatedAt = now,
            LastLoginAt = now
        };

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Dva paralelna prva zahtjeva istog korisnika - unique indeks na
            // EntraObjectId odbije drugi, pa se citamo postojeci red.
            db.Entry(user).State = EntityState.Detached;

            var existing = await db.Users
                .FirstOrDefaultAsync(u => u.EntraObjectId == entraObjectId, ct);

            if (existing is null)
            {
                throw;
            }

            return existing;
        }

        return user;
    }
}
