namespace CriticalListeningLab.Api.Domain.Entities;

/// <summary>
/// Jedna audio varijanta izvora. <c>StorageKey</c> je jedina veza s
/// filesystemom i zna je isklucivo <c>IAudioStorage</c>; poslovna logika
/// i API rade samo s <c>Id</c>.
/// </summary>
public class AudioAsset
{
    public Guid Id { get; set; }

    public Guid AudioSourceId { get; set; }
    public AudioSource AudioSource { get; set; } = null!;

    /// <summary>
    /// "full" za EQ; "uncompressed", "light", "heavy", "ratio-2", "ratio-4",
    /// "ratio-12" za Compression.
    /// </summary>
    public required string VariantSlug { get; set; }

    /// <summary>Relativni put, npr. "compression/drums/ratio-12.mp3".</summary>
    public required string StorageKey { get; set; }

    /// <summary>"audio/flac" za EQ, "audio/mpeg" za Compression.</summary>
    public required string MimeType { get; set; }

    public int DurationMs { get; set; }

    public int SampleRate { get; set; }

    /// <summary>
    /// Izmjerena integrirana glasnoca. Sluzi kao dokumentacija gain-matchinga
    /// da admin moze provjeriti jesu li varijante izjednacene.
    /// NE koristi se za runtime normalizaciju.
    /// </summary>
    public double? IntegratedLufs { get; set; }

    public bool IsEnabled { get; set; }
}
