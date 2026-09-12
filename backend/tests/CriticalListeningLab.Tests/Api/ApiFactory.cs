using CriticalListeningLab.Api.Auth;
using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Data.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CriticalListeningLab.Tests.Api;

internal sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly bool _useDevBypass;
    private readonly Action<AuthOptions>? _configureAuth;
    private readonly string _audioRoot = Path.Combine(Path.GetTempPath(), "cll-audio-" + Guid.NewGuid());

    public ApiFactory(bool useDevBypass = true, Action<AuthOptions>? configureAuth = null)
    {
        _useDevBypass = useDevBypass;
        _configureAuth = configureAuth;
        WriteDummyAudio(_audioRoot);
    }

    public string AudioRoot => _audioRoot;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Auth:UseDevBypass", _useDevBypass ? "true" : "false");
        builder.UseSetting("AzureAd:Instance", "https://login.microsoftonline.com/");
        builder.UseSetting("AzureAd:TenantId", "00000000-0000-0000-0000-000000000001");
        builder.UseSetting("AzureAd:ClientId", "00000000-0000-0000-0000-000000000002");
        builder.UseSetting("AzureAd:Audience", "api://test");
        builder.UseSetting("ConnectionStrings:Database", "Host=localhost;Database=unused");
        builder.UseSetting("AudioStorage:Provider", "Local");
        builder.UseSetting("AudioStorage:LocalRoot", _audioRoot);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            if (_configureAuth is not null)
            {
                services.PostConfigure(_configureAuth);
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_audioRoot))
        {
            Directory.Delete(_audioRoot, recursive: true);
        }
    }

    private static void WriteDummyAudio(string root)
    {
        foreach (var slug in new[] { "pink-noise", "drums", "acoustic-guitar", "vocal" })
        {
            Write(root, $"eq/{slug}.flac");
        }

        foreach (var source in new[] { "drums", "vocal" })
        {
            foreach (var variant in CatalogSeeder.CompressionVariantOrder)
            {
                Write(root, $"compression/{source}/{variant}.mp3");
            }
        }
    }

    private static void Write(string root, string relative)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, "CLL-TEST-AUDIO"u8.ToArray());
    }
}
