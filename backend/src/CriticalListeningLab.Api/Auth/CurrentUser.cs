using System.Security.Claims;
using CriticalListeningLab.Api.Domain;

namespace CriticalListeningLab.Api.Auth;

/// <summary>
/// Identitet pozivatelja, citan iz domenskih claimova. Sve u aplikaciji
/// koristi ovo, a ne Entra claimove direktno.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid Id { get; }
    UserRole Role { get; }
    Guid? CohortId { get; }
    bool IsAdmin { get; }
}

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.HasClaim(c => c.Type == AppClaims.UserId) == true;

    public Guid Id =>
        Guid.TryParse(Principal?.FindFirst(AppClaims.UserId)?.Value, out var id)
            ? id
            : throw new InvalidOperationException(
                "Zahtjev nema identificiranog korisnika. Endpoint nije zasticen ili claims transformacija nije odradena.");

    public UserRole Role =>
        Enum.TryParse<UserRole>(Principal?.FindFirst(AppClaims.Role)?.Value, out var role)
            ? role
            : throw new InvalidOperationException("Zahtjev nema rolu korisnika.");

    public Guid? CohortId =>
        Guid.TryParse(Principal?.FindFirst(AppClaims.CohortId)?.Value, out var id)
            ? id
            : null;

    public bool IsAdmin => Role == UserRole.Admin;
}
