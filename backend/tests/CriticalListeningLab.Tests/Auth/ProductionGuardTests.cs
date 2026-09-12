using CriticalListeningLab.Api.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace CriticalListeningLab.Tests.Auth;

public class ProductionGuardTests
{
    [Fact]
    public void UseDevBypass_in_Production_throws_before_the_app_starts()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:UseDevBypass"] = "true"
            })
            .Build();
        var environment = new StubHostEnvironment { EnvironmentName = Environments.Production };

        var error = Should.Throw<InvalidOperationException>(() =>
            services.AddAppAuthentication(config, environment));

        error.Message.ShouldContain("UseDevBypass");
        error.Message.ShouldContain("Production");
    }

    [Fact]
    public void UseDevBypass_in_Development_is_allowed()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:UseDevBypass"] = "true"
            })
            .Build();
        var environment = new StubHostEnvironment { EnvironmentName = Environments.Development };

        Should.NotThrow(() => services.AddAppAuthentication(config, environment));
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
