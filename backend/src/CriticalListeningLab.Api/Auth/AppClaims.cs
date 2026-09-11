namespace CriticalListeningLab.Api.Auth;

/// <summary>
/// Domenski claimovi koje dodaje <see cref="UserProvisioningClaimsTransformation"/>.
/// Sve u aplikaciji cita identitet odavde, a ne iz Entra claimova, pa dev
/// bypass i pravi tok imaju identican oblik principala.
/// </summary>
public static class AppClaims
{
    public const string UserId = "app:userId";
    public const string Role = "app:role";
    public const string CohortId = "app:cohortId";
}
