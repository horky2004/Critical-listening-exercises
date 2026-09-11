namespace CriticalListeningLab.Api.Domain.Entities;

/// <summary>Generacija studenata, npr. "2025/26".</summary>
public class Cohort
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// Novi korisnici se pri prvoj prijavi dodjeljuju aktivnom cohortu.
    /// Filtrirani unique indeks osigurava da je aktivan najvise jedan.
    /// </summary>
    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<User> Users { get; set; } = [];
    public ICollection<CohortModuleAvailability> ModuleAvailabilities { get; set; } = [];
}
