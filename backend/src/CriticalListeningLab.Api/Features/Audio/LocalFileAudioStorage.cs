using CriticalListeningLab.Api.Features.TestSessions;
using Microsoft.Extensions.Options;

namespace CriticalListeningLab.Api.Features.Audio;

public class LocalFileAudioStorage : IAudioStorage
{
    private readonly string _root;

    public LocalFileAudioStorage(IOptions<AudioStorageOptions> options, IHostEnvironment environment)
    {
        var configured = options.Value.LocalRoot;
        _root = NormalizeRoot(
            Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(environment.ContentRootPath, configured));
    }

    public Task<AudioDelivery> GetAsync(string storageKey, CancellationToken ct)
    {
        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
        {
            throw new NotFoundException("Audio fajl nije pronaden.");
        }

        var info = new FileInfo(path);
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var etag = $"\"{info.Length:x}-{info.LastWriteTimeUtc.Ticks:x}\"";
        return Task.FromResult<AudioDelivery>(
            new AudioDelivery.Stream(stream, MimeFromExtension(path), info.Length, etag));
    }

    internal string ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)
            || storageKey.Contains("..", StringComparison.Ordinal)
            || Path.IsPathRooted(storageKey))
        {
            throw new BadRequestException("Neispravan storage kljuc.");
        }

        var combined = Path.GetFullPath(Path.Combine(_root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!combined.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Neispravan storage kljuc.");
        }

        return combined;
    }

    private static string NormalizeRoot(string root)
    {
        var full = Path.GetFullPath(root);
        return full.EndsWith(Path.DirectorySeparatorChar)
            ? full
            : full + Path.DirectorySeparatorChar;
    }

    private static string MimeFromExtension(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".flac" => "audio/flac",
        ".mp3" => "audio/mpeg",
        ".wav" => "audio/wav",
        _ => "application/octet-stream"
    };
}
