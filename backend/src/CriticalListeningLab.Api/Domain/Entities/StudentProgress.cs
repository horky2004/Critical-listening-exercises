namespace CriticalListeningLab.Api.Domain.Entities;

/// <summary>
/// Napredak jednog studenta na jednom levelu jednog audio izvora.
/// Kljuc <c>(UserId, AudioSourceId, ExerciseLevelId)</c> je ono sto cini
/// napredak neovisnim po izvoru - EQ/Drums/BOOST L4 i EQ/Vocal/BOOST L2
/// su dva razlicita reda.
/// <para>
/// Red postoji samo za levele koje je student POKUSAO. Otkljucanost se
/// izracunava iz unlock pravila, ne cuva u koloni (odluka 5).
/// </para>
/// </summary>
public class StudentProgress
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid AudioSourceId { get; set; }
    public AudioSource AudioSource { get; set; } = null!;

    public Guid ExerciseLevelId { get; set; }
    public ExerciseLevel ExerciseLevel { get; set; } = null!;

    /// <summary>
    /// Najbolji broj tocnih odgovora, nikad postotak. NIKAD se ne smanjuje:
    /// nakon 14/14 pa 12/14 ostaje 14.
    /// </summary>
    public int BestScore { get; set; }

    /// <summary>Jednom prosao, zauvijek prosao - kasniji pad ne gasi ovo.</summary>
    public bool IsPassed { get; set; }

    /// <summary>Broj zavrsenih testova, i prolaza i padova.</summary>
    public int AttemptCount { get; set; }

    public DateTimeOffset? FirstPassedAt { get; set; }

    public DateTimeOffset LastAttemptAt { get; set; }
}
