namespace CriticalListeningLab.Api.Domain.Entities;

/// <summary>
/// Template levela, definiran po modulu - NE po audio izvoru (odluka 1).
/// EQ ima 13 redova, Compression 3. Napredak je neovisan po izvoru jer
/// <c>StudentProgress</c> nosi i <c>AudioSourceId</c>.
/// </summary>
public class ExerciseLevel
{
    public Guid Id { get; set; }

    public Guid SegmentId { get; set; }
    public ExerciseSegment Segment { get; set; } = null!;

    /// <summary>1-based, unutar segmenta.</summary>
    public int LevelNumber { get; set; }

    public required string Title { get; set; }

    public ExerciseType ExerciseType { get; set; }

    /// <summary>
    /// Parametri vjezbe kao jsonb. Shema ovisi o <c>ExerciseType</c> - to je
    /// mjesto gdje se dodaju novi tipovi vjezbi bez migracije (odluka 15).
    /// </summary>
    public required string ConfigJson { get; set; }

    public int QuestionCount { get; set; }

    /// <summary>
    /// Broj tocnih odgovora potreban za prolaz - NIKAD postotak.
    /// Prolaz je uvijek <c>correctAnswers &gt;= PassThreshold</c>.
    /// </summary>
    public int PassThreshold { get; set; }

    public bool IsEnabled { get; set; }

    /// <summary>
    /// Uvjeti koje treba zadovoljiti da se ovaj level otkljuca.
    /// Svi moraju biti zadovoljeni (AND); nula uvjeta = otkljucan od pocetka.
    /// </summary>
    public ICollection<LevelUnlockRequirement> UnlockRequirements { get; set; } = [];

    /// <summary>Leveli kojima je ovaj level prerequisite.</summary>
    public ICollection<LevelUnlockRequirement> UnlocksRequirements { get; set; } = [];

    public ICollection<StudentProgress> Progress { get; set; } = [];
    public ICollection<TestSession> TestSessions { get; set; } = [];
}
