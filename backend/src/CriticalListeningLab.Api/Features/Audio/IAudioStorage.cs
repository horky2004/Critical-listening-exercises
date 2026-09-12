namespace CriticalListeningLab.Api.Features.Audio;

public interface IAudioStorage
{
    Task<AudioDelivery> GetAsync(string storageKey, CancellationToken ct);
}

public abstract record AudioDelivery
{
    public sealed record Stream(System.IO.Stream Content, string MimeType, long Length, string ETag)
        : AudioDelivery;

    public sealed record Redirect(string Url, TimeSpan ExpiresIn) : AudioDelivery;
}
