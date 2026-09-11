namespace CriticalListeningLab.Api.Domain.Entities;

/// <summary>
/// Jedan uvjet za otkljucavanje levela. Semantika: svi uvjeti jednog levela
/// moraju biti zadovoljeni (AND), a nula uvjeta znaci da je level otkljucan
/// od pocetka. Uvijek se evaluira u kontekstu jednog audio izvora.
/// <para>
/// Branching nije poseban slucaj: BOOST L3 je prerequisite i za BOOST L4 i
/// za CUT L1, pa jedan prolaz otkljucava oba.
/// </para>
/// </summary>
public class LevelUnlockRequirement
{
    public Guid Id { get; set; }

    /// <summary>Level koji se otkljucava.</summary>
    public Guid ExerciseLevelId { get; set; }
    public ExerciseLevel ExerciseLevel { get; set; } = null!;

    /// <summary>Level koji je prerequisite.</summary>
    public Guid RequiredExerciseLevelId { get; set; }
    public ExerciseLevel RequiredExerciseLevel { get; set; } = null!;

    public UnlockRequirementType RequirementType { get; set; }

    /// <summary>
    /// Minimalni broj tocnih odgovora na prerequisiteu.
    /// Null znaci "koristi <c>PassThreshold</c> prerequisitea".
    /// </summary>
    public int? MinScore { get; set; }
}
