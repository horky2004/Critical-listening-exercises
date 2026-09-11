namespace CriticalListeningLab.Api.Auth;

public static class AuthPolicies
{
    /// <summary>Studenti i admini - admin moze koristiti studentske ekrane.</summary>
    public const string RequireStudent = "RequireStudent";

    public const string RequireAdmin = "RequireAdmin";
}
