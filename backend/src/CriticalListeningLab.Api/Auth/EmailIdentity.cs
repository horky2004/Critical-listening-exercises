using System.Security.Claims;

namespace CriticalListeningLab.Api.Auth;

/// <summary>
/// Citanje i provjera e-mail identiteta iz tokena. Cista logika, bez ovisnosti
/// na ASP.NET pipeline, pa je pokrivena unit testovima.
/// </summary>
public static class EmailIdentity
{
    private static readonly string[] EmailClaimTypes =
    [
        "preferred_username",
        ClaimTypes.Email,
        "email",
        ClaimTypes.Upn,
        "upn"
    ];

    /// <summary>
    /// Vraca prvi claim koji izgleda kao e-mail adresa, normaliziran na
    /// lowercase. Null ako ga nema.
    /// </summary>
    public static string? ReadEmail(ClaimsPrincipal principal)
    {
        foreach (var claimType in EmailClaimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value) && value.Contains('@'))
            {
                return Normalize(value);
            }
        }

        return null;
    }

    public static string Normalize(string email) => email.Trim().ToLowerInvariant();

    /// <summary>
    /// Usporedba je egzaktna nad cijelom domenom, pa "algebra.hr.napadac.com"
    /// ne prolazi, a ne prolaze ni poddomene koje nisu na listi.
    /// </summary>
    public static bool IsAllowedDomain(string? email, IReadOnlyCollection<string> allowedDomains)
    {
        if (string.IsNullOrWhiteSpace(email) || allowedDomains.Count == 0)
        {
            return false;
        }

        var atIndex = email.LastIndexOf('@');
        if (atIndex < 1 || atIndex == email.Length - 1)
        {
            return false;
        }

        var domain = email[(atIndex + 1)..];

        return allowedDomains.Any(allowed =>
            string.Equals(domain, allowed.Trim().TrimStart('@'), StringComparison.OrdinalIgnoreCase));
    }
}
