using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CriticalListeningLab.Api.Auth;

/// <summary>
/// Nakon uspjesne validacije tokena: provjeri e-mail domenu, upsertaj
/// korisnika i dodaj domenske claimove.
/// <para>
/// Ako domena nije dopustena, domenski claimovi se NE dodaju i korisnik se
/// NE kreira. Kako svaki endpoint zahtijeva <c>app:role</c> claim, rezultat
/// je 403 - autentificiran, ali neautoriziran.
/// </para>
/// <para>
/// Ista transformacija se izvrsava i za dev bypass i za pravi Entra tok,
/// pa bypass zamjenjuje samo izvor identiteta, ne logiku oko njega.
/// </para>
/// </summary>
public class UserProvisioningClaimsTransformation(
    IUserProvisioningService provisioning,
    IOptions<AuthOptions> authOptions,
    ILogger<UserProvisioningClaimsTransformation> logger) : IClaimsTransformation
{
    private readonly AuthOptions _options = authOptions.Value;

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not { IsAuthenticated: true })
        {
            return principal;
        }

        // Transformacija se moze pozvati vise puta po zahtjevu, pa mora biti
        // idempotentna.
        if (principal.HasClaim(c => c.Type == AppClaims.UserId))
        {
            return principal;
        }

        var entraObjectId = ReadObjectId(principal);
        if (string.IsNullOrWhiteSpace(entraObjectId))
        {
            logger.LogWarning("Token bez 'oid' claima - korisnik se ne provisionira");
            return principal;
        }

        var email = EmailIdentity.ReadEmail(principal);
        if (!EmailIdentity.IsAllowedDomain(email, _options.AllowedEmailDomains))
        {
            logger.LogWarning(
                "Odbijena prijava: e-mail domena nije dopustena ({Email})",
                email ?? "<nepoznat>");
            return principal;
        }

        var displayName = principal.FindFirst("name")?.Value
                          ?? principal.FindFirst(ClaimTypes.Name)?.Value
                          ?? email!;

        var user = await provisioning.GetOrCreateAsync(
            entraObjectId, email!, displayName, CancellationToken.None);

        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(AppClaims.UserId, user.Id.ToString()));
        identity.AddClaim(new Claim(AppClaims.Role, user.Role.ToString()));

        if (user.CohortId is { } cohortId)
        {
            identity.AddClaim(new Claim(AppClaims.CohortId, cohortId.ToString()));
        }

        principal.AddIdentity(identity);
        return principal;
    }

    private static string? ReadObjectId(ClaimsPrincipal principal) =>
        principal.FindFirst("oid")?.Value
        ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
        ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
