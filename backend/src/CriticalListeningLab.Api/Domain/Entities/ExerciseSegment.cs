namespace CriticalListeningLab.Api.Domain.Entities;

/// <summary>
/// Segment vjezbi unutar modula: "boost", "cut", "combined" za EQ,
/// "detection" za Compression. Tablica (a ne enum) jer progression tree
/// iz nje vuce labele i poredak, pa frontend ne mora znati strukturu modula.
/// </summary>
public class ExerciseSegment
{
    public Guid Id { get; set; }

    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = null!;

    public required string Key { get; set; }

    public required string Name { get; set; }

    public int SortOrder { get; set; }

    public ICollection<ExerciseLevel> Levels { get; set; } = [];
}
