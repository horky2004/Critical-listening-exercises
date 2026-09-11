using System.Security.Claims;
using System.Text.Encodings.Web;
using CriticalListeningLab.Api.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CriticalListeningLab.Api.Auth;

/// <summary>
/// Glumi prijavljenog korisnika da razvoj ne ovisi o Entra app registraciji.
/// Registrira se samo kad je <c>Auth:UseDevBypass</c> uklucen, a aplikacija
/// odbija startati ako je to slucaj u Production okolini.
/// <para>
/// Proizvodi claimove istog oblika kao pravi Entra token, pa
/// <see cref="UserProvisioningClaimsTransformation"/> radi identican posao
/// u oba slucaja - domenska provjera i JIT provisioning se stvarno testiraju.
/// </para>
/// </summary>
public class DevAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    IOptions<AuthOptions> authOptions,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "DevBypass";

    /// <summary>
    /// Prebacivanje role bez restarta, za testiranje admin ekrana.
    /// Cita se samo ovdje, a ovaj handler postoji samo kad je bypass uklucen.
    /// </summary>
    public const string RoleOverrideHeader = "X-Dev-Role";

    private readonly AuthOptions _authOptions = authOptions.Value;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var devUser = SelectDevUser();

        // Isti oblik claimova kao pravi Entra token. Rola se NE postavlja
        // ovdje - dolazi iz baze nakon provisioninga, kao i u produkciji.
        var claims = new List<Claim>
        {
            new("oid", devUser.EntraObjectId),
            new("preferred_username", devUser.Email),
            new("name", devUser.DisplayName)
        };

        var identity = new ClaimsIdentity(claims, SchemeName, "preferred_username", null);
        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(
            AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }

    private DevUserOptions SelectDevUser()
    {
        var requested = Request.Headers[RoleOverrideHeader].ToString();

        return string.Equals(requested, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase)
            ? _authOptions.DevAdminUser
            : _authOptions.DevUser;
    }
}
