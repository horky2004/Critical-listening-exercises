using CriticalListeningLab.Api.Features.Audio;
using CriticalListeningLab.Api.Features.TestSessions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CriticalListeningLab.Tests.Audio;

public class LocalFileAudioStorageTests
{
    [Fact]
    public async Task Reads_a_file_inside_the_root()
    {
        var root = CreateRoot();
        try
        {
            var path = Path.Combine(root, "eq", "drums.flac");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, [1, 2, 3, 4]);

            var storage = CreateStorage(root);
            var delivery = await storage.GetAsync("eq/drums.flac", CancellationToken.None);

            var stream = delivery.ShouldBeOfType<AudioDelivery.Stream>();
            stream.Length.ShouldBe(4);
            stream.MimeType.ShouldBe("audio/flac");
            using var content = stream.Content;
            var bytes = new byte[4];
            (await content.ReadAsync(bytes)).ShouldBe(4);
            bytes.ShouldBe([1, 2, 3, 4]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Rejects_path_traversal()
    {
        var root = CreateRoot();
        try
        {
            var storage = CreateStorage(root);
            Should.Throw<BadRequestException>(() => storage.ResolvePath("../secret.flac"));
            Should.Throw<BadRequestException>(() => storage.ResolvePath("eq/../../secret.flac"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static LocalFileAudioStorage CreateStorage(string root) =>
        new(Options.Create(new AudioStorageOptions { LocalRoot = root }), new StubHost(root));

    private static string CreateRoot() =>
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cll-storage-" + Guid.NewGuid())).FullName;

    private sealed class StubHost(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
