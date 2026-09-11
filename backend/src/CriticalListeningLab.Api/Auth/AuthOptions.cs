using CriticalListeningLab.Api.Domain;

namespace CriticalListeningLab.Api.Auth;

public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Dopustene e-mail domene. Single-tenant registracija vec ogranicava
    /// prijavu na Algebrin tenant, ali ovo je druga, eksplicitna granica -
    /// tenant moze sadrzavati goste ili racune drugih domena.
    /// </summary>
    public string[] AllowedEmailDomains { get; set; } = [];

    /// <summary>
    /// Bootstrap admina. Provjerava se samo pri kreiranju korisnika; nakon
    /// toga je rola iz baze autoritet, pa uklanjanje s liste ne skida rolu.
    /// </summary>
    public string[] AdminEmails { get; set; } = [];

    /// <summary>
    /// Preskace Entra validaciju i glumi prijavljenog korisnika. Cita se
    /// isklucivo iz konfiguracije, nikad iz zahtjeva. Aplikacija odbija
    /// startati ako je uklucen u Production okolini.
    /// </summary>
    public bool UseDevBypass { get; set; }

    /// <summary>Identitet koji dev bypass koristi po defaultu.</summary>
    public DevUserOptions DevUser { get; set; } = new();

    /// <summary>
    /// Drugi dev identitet, za testiranje admin ekrana bez restarta
    /// (header <c>X-Dev-Role: Admin</c>). Njegov e-mail mora biti na
    /// <see cref="AdminEmails"/> listi da provisioning dodijeli admin rolu -
    /// time rola i u dev modu dolazi iz iste logike kao u produkciji.
    /// </summary>
    public DevUserOptions DevAdminUser { get; set; } = new()
    {
        EntraObjectId = "dev-00000000-0000-0000-0000-000000000002",
        Email = "dev.admin@algebra.hr",
        DisplayName = "Dev Admin"
    };
}

public class DevUserOptions
{
    public string EntraObjectId { get; set; } = "dev-00000000-0000-0000-0000-000000000001";
    public string Email { get; set; } = "dev.student@student.algebra.hr";
    public string DisplayName { get; set; } = "Dev Student";
}
