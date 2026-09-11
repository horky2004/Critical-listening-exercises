namespace CriticalListeningLab.Api.Domain.Entities;

/// <summary>
/// Audio izvor unutar modula, npr. EQ/drums ili Compression/drums.
/// "drums" postoji dvaput jer su to razliciti materijali i neovisan napredak.
/// </summary>
public class AudioSource
{
    public Guid Id { get; set; }

    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = null!;

    /// <summary>Koristi se u URL-ovima: "pink-noise", "drums", "acoustic-guitar", "vocal".</summary>
    public required string Slug { get; set; }

    public required string Name { get; set; }

    public int SortOrder { get; set; }

    public bool IsEnabled { get; set; }

    public ICollection<AudioAsset> Assets { get; set; } = [];
    public ICollection<StudentProgress> Progress { get; set; } = [];
    public ICollection<TestSession> TestSessions { get; set; } = [];
}
