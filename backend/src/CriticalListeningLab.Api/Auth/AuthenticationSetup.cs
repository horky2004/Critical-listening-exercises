using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

namespace CriticalListeningLab.Api.Auth;

public static class AuthenticationSetup
{
    /// <summary>
    /// Registrira autentikaciju i autorizaciju. Ovisno o
    /// <c>Auth:UseDevBypass</c> aktivna je dev shema ili prava Entra ID
    /// validacija tokena; sve ostalo je identicno u oba slucaja.
    /// </summary>
    public static IServiceCollection AddAppAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
                          ?? new AuthOptions();

        GuardAgainstDevBypassInProduction(authOptions, environment);

        if (authOptions.UseDevBypass)
        {
            services
                .AddAuthentication(DevAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(
                    DevAuthenticationHandler.SchemeName, displayName: "Dev bypass", configureOptions: null);
        }
        else
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(configuration, "AzureAd");
        }

        services.AddHttpContextAccessor();
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        services.AddScoped<IClaimsTransformation, UserProvisioningClaimsTransformation>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.RequireStudent, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(AppClaims.Role, nameof(Domain.UserRole.Student), nameof(Domain.UserRole.Admin)))
            .AddPolicy(AuthPolicies.RequireAdmin, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(AppClaims.Role, nameof(Domain.UserRole.Admin)))
            // Fallback trazi domenski 'app:role' claim, a ne samo prijavljenog
            // korisnika. Zato je svaki endpoint po defaultu i autentificiran i
            // provjeren na dopustenu e-mail domenu; racun izvan dopustenih
            // domena dobije 403 bez ijedne posebne provjere u endpointu.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireClaim(AppClaims.Role)
                .Build());

        return services;
    }

    /// <summary>
    /// Bolje je da deploy padne nego da produkcija radi bez autentikacije.
    /// </summary>
    private static void GuardAgainstDevBypassInProduction(
        AuthOptions options, IHostEnvironment environment)
    {
        if (options.UseDevBypass && environment.IsProduction())
        {
            throw new InvalidOperationException(
                "Auth:UseDevBypass je ukljucen u Production okolini. " +
                "Aplikacija bi radila bez autentikacije, pa je start prekinut. " +
                "Postavi Auth:UseDevBypass na false.");
        }
    }
}
