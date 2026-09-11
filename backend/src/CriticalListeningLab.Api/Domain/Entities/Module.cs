namespace CriticalListeningLab.Api.Domain.Entities;

/// <summary>Modul vjezbi - trenutno "eq" i "compression".</summary>
public class Module
{
    public Guid Id { get; set; }

    /// <summary>Koristi se u URL-ovima: "eq", "compression".</summary>
    public required string Slug { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public int SortOrder { get; set; }

    /// <summary>
    /// Glavni prekidac. Efektivna dostupnost za studenta je
    /// <c>IsEnabledGlobally &amp;&amp; (cohort override ?? true)</c>.
    /// Gasenje nikad ne brise napredak.
    /// </summary>
    public bool IsEnabledGlobally { get; set; }

    public ICollection<AudioSource> AudioSources { get; set; } = [];
    public ICollection<ExerciseSegment> Segments { get; set; } = [];
    public ICollection<CohortModuleAvailability> CohortAvailabilities { get; set; } = [];
}
