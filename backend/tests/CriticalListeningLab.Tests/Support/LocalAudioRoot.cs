namespace CriticalListeningLab.Tests.Support;

internal static class LocalAudioRoot
{
    public static string? Find()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                foreach (var relative in new[]
                {
                    Path.Combine("src", "CriticalListeningLab.Api", "wwwroot", "audio"),
                    Path.Combine("backend", "src", "CriticalListeningLab.Api", "wwwroot", "audio"),
                    Path.Combine("App", "backend", "src", "CriticalListeningLab.Api", "wwwroot", "audio")
                })
                {
                    var candidate = Path.Combine(dir.FullName, relative);
                    if (File.Exists(Path.Combine(candidate, "compression", "drums", "uncompressed.mp3")))
                    {
                        return candidate;
                    }
                }
            }
        }

        return null;
    }
}
