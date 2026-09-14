using System.Text.Json;
using System.Text.Json.Serialization;
using CriticalListeningLab.Api.Auth;
using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Data.Seed;
using CriticalListeningLab.Api.Domain.Questions;
using CriticalListeningLab.Api.Features.Admin;
using CriticalListeningLab.Api.Features.Audio;
using CriticalListeningLab.Api.Features.Errors;
using CriticalListeningLab.Api.Features.Modules;
using CriticalListeningLab.Api.Features.Progress;
using CriticalListeningLab.Api.Features.TestSessions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("Database"))
        .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IProgressionService, ProgressionService>();
builder.Services.AddScoped<IModuleAccessService, ModuleAccessService>();
builder.Services.AddSingleton<IQuestionGenerator, EqFrequencyGenerator>();
builder.Services.AddSingleton<IQuestionGenerator, EqFrequencyAndDirectionGenerator>();
builder.Services.AddSingleton<IQuestionGenerator, CompressionChoiceGenerator>();
builder.Services.AddSingleton<QuestionGeneratorResolver>();
builder.Services.AddScoped<ITestSessionService, TestSessionService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.Configure<AudioStorageOptions>(builder.Configuration.GetSection(AudioStorageOptions.SectionName));
var audioStorage = builder.Configuration.GetSection(AudioStorageOptions.SectionName).Get<AudioStorageOptions>()
                   ?? new AudioStorageOptions();
if (!string.Equals(audioStorage.Provider, "Local", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        $"AudioStorage provider '{audioStorage.Provider}' nije podrzan. Trenutno je implementiran samo Local.");
}

builder.Services.AddSingleton<IAudioStorage, LocalFileAudioStorage>();

builder.Services.AddAppAuthentication(builder.Configuration, builder.Environment);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        // Enumi kao stringovi: klijentu je "Student" jasnije od 0, i dodavanje
        // novog clana ne mijenja znacenje postojecih vrijednosti u API-ju.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));
// Bez AllowCredentials - koristimo Bearer tokene, ne cookieje.

builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

var app = builder.Build();

await SeedCatalogAsync(app);

// Neobradene greske kao ProblemDetails, bez internih detalja prema klijentu.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    var forwarded = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwarded.KnownIPNetworks.Clear();
    forwarded.KnownProxies.Clear();
    app.UseForwardedHeaders(forwarded);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task SeedCatalogAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");

    try
    {
        await CatalogSeeder.EnsureAsync(db);
        logger.LogInformation("Katalog je uskladjen.");
    }
    catch (Exception ex) when (app.Environment.IsDevelopment())
    {
        logger.LogWarning(ex, "Seed preskocen - baza nije dostupna.");
    }
}

// Potrebno za WebApplicationFactory u testovima.
public partial class Program;
