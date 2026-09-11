namespace CriticalListeningLab.Api.Domain.Entities;

/// <summary>
/// Cohort-specifican override dostupnosti modula.
/// Odsutnost reda znaci "naslijedi globalno stanje".
/// </summary>
public class CohortModuleAvailability
{
    public Guid Id { get; set; }

    public Guid CohortId { get; set; }
    public Cohort Cohort { get; set; } = null!;

    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = null!;

    public bool IsEnabled { get; set; }
}
