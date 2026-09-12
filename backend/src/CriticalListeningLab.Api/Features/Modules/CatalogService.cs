using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Data.Seed;
using CriticalListeningLab.Api.Domain;
using CriticalListeningLab.Api.Domain.Entities;
using CriticalListeningLab.Api.Domain.Questions;
using CriticalListeningLab.Api.Features.Progress;
using CriticalListeningLab.Api.Features.TestSessions;
using Microsoft.EntityFrameworkCore;

namespace CriticalListeningLab.Api.Features.Modules;

public class CatalogService(
    AppDbContext db,
    IModuleAccessService access,
    IProgressionService progression) : ICatalogService
{
    public async Task<ModuleListResponse> ListModulesAsync(Guid userId, CancellationToken ct)
    {
        var modules = await LoadModulesAsync(ct);
        var passed = await LoadPassedCountsAsync(userId, ct);
        var items = new List<ModuleListItem>(modules.Count);

        foreach (var module in modules)
        {
            items.Add(ToListItem(module, passed, await UnavailableReasonAsync(userId, module, ct)));
        }

        return new ModuleListResponse(items);
    }

    public async Task<ModuleListItem> GetModuleAsync(Guid userId, string moduleSlug, CancellationToken ct)
    {
        var module = await RequireModuleAsync(moduleSlug, ct);
        var passed = await LoadPassedCountsAsync(userId, ct);
        return ToListItem(module, passed, await UnavailableReasonAsync(userId, module, ct));
    }

    public async Task<SourceListResponse> ListSourcesAsync(Guid userId, string moduleSlug, CancellationToken ct)
    {
        var module = await RequireAvailableModuleAsync(userId, moduleSlug, ct);
        var passed = await LoadPassedCountsAsync(userId, ct);
        var levelCount = module.Segments.SelectMany(s => s.Levels).Count(l => l.IsEnabled);
        var sources = module.AudioSources
            .Where(s => s.IsEnabled)
            .OrderBy(s => s.SortOrder)
            .Select(s => new SourceListItem(
                s.Slug,
                s.Name,
                passed.GetValueOrDefault(s.Id),
                levelCount,
                HasPracticeMode: true))
            .ToList();

        return new SourceListResponse(new ModuleRef(module.Slug, module.Name), sources);
    }

    public async Task<TreeResponse> GetTreeAsync(
        Guid userId, string moduleSlug, string sourceSlug, CancellationToken ct)
    {
        var module = await RequireAvailableModuleAsync(userId, moduleSlug, ct);
        var source = module.AudioSources.FirstOrDefault(s => s.Slug == sourceSlug && s.IsEnabled)
                     ?? throw new NotFoundException("Audio izvor nije pronaden u tom modulu.");

        var tree = await progression.GetTreeStateAsync(userId, source.Id, ct);
        var segments = tree.Segments
            .Select(segment => new TreeSegment(
                segment.Key,
                segment.Name,
                segment.Levels.Select(level => new TreeLevel(
                    level.LevelId,
                    level.LevelNumber,
                    level.Title,
                    level.Status,
                    level.BestScore,
                    level.BestScorePercentage,
                    level.QuestionCount,
                    level.PassThreshold,
                    level.AttemptCount,
                    level.FirstPassedAt,
                    level.RequiredLevelIds)).ToList()))
            .ToList();

        return new TreeResponse(
            new ModuleRef(module.Slug, module.Name),
            new SourceRef(source.Slug, source.Name),
            segments);
    }

    public async Task<PracticeResponse> GetPracticeAsync(
        Guid userId, string moduleSlug, string sourceSlug, CancellationToken ct)
    {
        var module = await RequireAvailableModuleAsync(userId, moduleSlug, ct);
        var listed = module.AudioSources.FirstOrDefault(s => s.Slug == sourceSlug && s.IsEnabled)
                     ?? throw new NotFoundException("Audio izvor nije pronaden u tom modulu.");
        var source = await db.AudioSources
            .Include(s => s.Assets)
            .FirstAsync(s => s.Id == listed.Id, ct);

        var moduleRef = new ModuleRef(module.Slug, module.Name);
        var sourceRef = new SourceRef(source.Slug, source.Name);

        if (module.Slug == "eq")
        {
            return EqPractice(moduleRef, sourceRef, module, source);
        }

        return CompressionPractice(moduleRef, sourceRef, module, source);
    }

    private async Task<Module> RequireModuleAsync(string moduleSlug, CancellationToken ct) =>
        await db.Modules
            .AsSplitQuery()
            .Include(m => m.AudioSources)
            .Include(m => m.Segments)
            .ThenInclude(s => s.Levels)
            .FirstOrDefaultAsync(m => m.Slug == moduleSlug, ct)
        ?? throw new NotFoundException("Modul nije pronaden.");

    private async Task<Module> RequireAvailableModuleAsync(
        Guid userId, string moduleSlug, CancellationToken ct)
    {
        var module = await RequireModuleAsync(moduleSlug, ct);
        if (!await access.IsModuleAvailableAsync(userId, moduleSlug, ct))
        {
            throw new ForbiddenException("Modul nije dostupan.", "module-unavailable");
        }

        return module;
    }

    private async Task<IReadOnlyList<Module>> LoadModulesAsync(CancellationToken ct) =>
        await db.Modules
            .AsSplitQuery()
            .Include(m => m.AudioSources)
            .Include(m => m.Segments)
            .ThenInclude(s => s.Levels)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(ct);

    private async Task<Dictionary<Guid, int>> LoadPassedCountsAsync(Guid userId, CancellationToken ct) =>
        await db.StudentProgress.AsNoTracking()
            .Where(p => p.UserId == userId && p.IsPassed)
            .GroupBy(p => p.AudioSourceId)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), ct);

    private async Task<string?> UnavailableReasonAsync(Guid userId, Module module, CancellationToken ct)
    {
        if (await access.IsModuleAvailableAsync(userId, module.Slug, ct))
        {
            return null;
        }

        if (!module.IsEnabledGlobally)
        {
            return "NotEnabledGlobally";
        }

        return "NotEnabledForCohort";
    }

    private static ModuleListItem ToListItem(
        Module module, IReadOnlyDictionary<Guid, int> passed, string? unavailableReason)
    {
        var sources = module.AudioSources.Where(s => s.IsEnabled).ToList();
        var levelCount = module.Segments.SelectMany(s => s.Levels).Count(l => l.IsEnabled);
        var completed = sources.Sum(s => passed.GetValueOrDefault(s.Id));

        return new ModuleListItem(
            module.Slug,
            module.Name,
            module.Description,
            unavailableReason is null,
            unavailableReason,
            sources.Count,
            levelCount,
            completed,
            levelCount * sources.Count);
    }

    private static PracticeResponse EqPractice(
        ModuleRef moduleRef, SourceRef sourceRef, Module module, AudioSource source)
    {
        var asset = source.Assets.FirstOrDefault(a => a.IsEnabled && a.VariantSlug == "full")
                    ?? throw new NotFoundException("EQ izvor nema audio asset.");

        var configs = module.Segments
            .SelectMany(s => s.Levels)
            .Where(l => l.IsEnabled
                        && (l.ExerciseType == ExerciseType.EqFrequency
                            || l.ExerciseType == ExerciseType.EqFrequencyAndDirection))
            .Select(l => ExerciseConfig.ParseEq(l.ConfigJson))
            .ToList();

        var frequencies = configs.SelectMany(c => c.FrequenciesHz).Distinct().OrderBy(f => f).ToList();
        var gains = configs.SelectMany(c => c.GainsDb).Distinct().OrderByDescending(g => g).ToList();
        var q = configs.Select(c => c.Q).DefaultIfEmpty(ExerciseLimits.DefaultQ).First();

        return new PracticeResponse(
            moduleRef,
            sourceRef,
            "eqBand",
            new PracticeAudio(asset.Id, $"/api/audio/assets/{asset.Id}", asset.DurationMs, asset.MimeType),
            frequencies,
            gains,
            q,
            Variants: null);
    }

    private static PracticeResponse CompressionPractice(
        ModuleRef moduleRef, SourceRef sourceRef, Module module, AudioSource source)
    {
        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var level in module.Segments.SelectMany(s => s.Levels).Where(l => l.IsEnabled))
        {
            if (level.ExerciseType != ExerciseType.CompressionChoice)
            {
                continue;
            }

            foreach (var option in ExerciseConfig.ParseCompression(level.ConfigJson).Options)
            {
                foreach (var variant in option.Variants)
                {
                    if (option.Key == variant || !labels.ContainsKey(variant))
                    {
                        labels[variant] = option.Label;
                    }
                }
            }
        }

        var variants = CatalogSeeder.CompressionVariantOrder
            .Select(slug => source.Assets.FirstOrDefault(a => a.IsEnabled && a.VariantSlug == slug))
            .Where(a => a is not null)
            .Select(a => new PracticeVariant(
                a!.VariantSlug,
                labels.GetValueOrDefault(a.VariantSlug) ?? a.VariantSlug,
                $"/api/audio/assets/{a.Id}",
                a.DurationMs,
                a.MimeType))
            .ToList();

        return new PracticeResponse(
            moduleRef, sourceRef, "compressionVariants",
            Audio: null, FrequenciesHz: null, GainsDb: null, Q: null, variants);
    }
}
