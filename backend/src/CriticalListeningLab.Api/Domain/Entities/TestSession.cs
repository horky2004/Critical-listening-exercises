namespace CriticalListeningLab.Api.Domain.Entities;

/// <summary>
/// Jedan pokusaj testa. Sva pitanja se generiraju odmah pri kreiranju, pa je
/// sesija resumable nakon refresha. Finalizira se automatski pri zaprimanju
/// zadnjeg odgovora, u istoj transakciji (odluka 7).
/// </summary>
public class TestSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid AudioSourceId { get; set; }
    public AudioSource AudioSource { get; set; } = null!;

    public Guid ExerciseLevelId { get; set; }
    public ExerciseLevel ExerciseLevel { get; set; } = null!;

    public TestSessionStatus Status { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Kopija iz levela u trenutku kreiranja. Ako admin promijeni parametre
    /// levela dok test traje, test se dovrsava po pravilima s pocetka.
    /// </summary>
    public int QuestionCount { get; set; }

    /// <summary>Kopija iz levela u trenutku kreiranja - vidi <see cref="QuestionCount"/>.</summary>
    public int PassThreshold { get; set; }

    /// <summary>Izracunato iz baze pri finalizaciji, nikad iz klijentskog podatka.</summary>
    public int CorrectAnswers { get; set; }

    public bool Passed { get; set; }

    /// <summary>Za reproducibilnost pri debugiranju.</summary>
    public int RandomSeed { get; set; }

    public ICollection<TestSessionQuestion> Questions { get; set; } = [];

    /// <summary>
    /// Postotak je izvedena vrijednost i ne sprema se - spremanjem bi se
    /// samo omogucila nekonzistentnost s <see cref="CorrectAnswers"/>.
    /// </summary>
    public double ScorePercentage =>
        QuestionCount == 0 ? 0 : Math.Round(CorrectAnswers * 100.0 / QuestionCount, 1);
}
