namespace CriticalListeningLab.Api.Domain.Entities;

/// <summary>
/// Korisnik se kreira just-in-time pri prvoj autentificiranoj requesti.
/// Nema lokalnih lozinki ni kredencijala.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>
    /// <c>oid</c> claim iz Entra ID-a - stabilni identitet koji se ne mijenja
    /// kad se korisniku promijeni ime ili e-mail.
    /// </summary>
    public required string EntraObjectId { get; set; }

    /// <summary>Normaliziran na lowercase pri upisu.</summary>
    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    public UserRole Role { get; set; }

    /// <summary>Null za admine i za studente prije dodjele cohorta.</summary>
    public Guid? CohortId { get; set; }
    public Cohort? Cohort { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<StudentProgress> Progress { get; set; } = [];
    public ICollection<TestSession> TestSessions { get; set; } = [];
}
