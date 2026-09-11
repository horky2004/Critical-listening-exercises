namespace CriticalListeningLab.Api.Domain.Entities;

/// <summary>Jedno pitanje unutar test sesije.</summary>
public class TestSessionQuestion
{
    public Guid Id { get; set; }

    public Guid TestSessionId { get; set; }
    public TestSession TestSession { get; set; } = null!;

    /// <summary>1-based pozicija u testu.</summary>
    public int QuestionIndex { get; set; }

    /// <summary>
    /// Ono sto frontend dobije: tekst pitanja, ponudene opcije i - kod EQ-a -
    /// parametri filtra. Jsonb.
    /// </summary>
    public required string PromptJson { get; set; }

    /// <summary>
    /// Tocan odgovor. NIKAD ne izlazi iz backenda prije nego je pitanje
    /// odgovoreno. Format: "2000", "2000:boost", "ratio-4".
    /// </summary>
    public required string CorrectAnswerKey { get; set; }

    /// <summary>Null kod EQ pitanja - tamo se koristi asset izvora.</summary>
    public Guid? AudioAssetId { get; set; }
    public AudioAsset? AudioAsset { get; set; }

    /// <summary>
    /// Slucajan Guid po pitanju. Nema HMAC ni expiry - token je nepogodiv,
    /// vezan na jedno pitanje jedne sesije, a endpoint uz njega provjerava
    /// pripada li sesija pozivatelju. Time frontend nikad ne vidi naziv
    /// compression varijante.
    /// </summary>
    public Guid AudioToken { get; set; }

    public string? StudentAnswerKey { get; set; }

    /// <summary>Null znaci neodgovoreno.</summary>
    public bool? IsCorrect { get; set; }

    public DateTimeOffset? AnsweredAt { get; set; }
}
