using CriticalListeningLab.Api.Auth;
using CriticalListeningLab.Api.Data;
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

    public ApiFactory(bool useDevBypass = true, Action<AuthOptions>? configureAuth = null)
    {
        _useDevBypass = useDevBypass;
        _configureAuth = configureAuth;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Auth:UseDevBypass", _useDevBypass ? "true" : "false");
        builder.UseSetting("AzureAd:Instance", "https://login.microsoftonline.com/");
        builder.UseSetting("AzureAd:TenantId", "00000000-0000-0000-0000-000000000001");
        builder.UseSetting("AzureAd:ClientId", "00000000-0000-0000-0000-000000000002");
        builder.UseSetting("AzureAd:Audience", "api://test");
        builder.UseSetting("ConnectionStrings:Database", "Host=localhost;Database=unused");

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
}
