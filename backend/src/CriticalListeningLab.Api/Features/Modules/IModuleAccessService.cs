using CriticalListeningLab.Api.Domain.Entities;

namespace CriticalListeningLab.Api.Features.Modules;

public interface IModuleAccessService
{
    Task<bool> IsModuleAvailableAsync(Guid userId, string moduleSlug, CancellationToken ct);

    Task<AudioSource?> FindSourceAsync(string moduleSlug, string sourceSlug, CancellationToken ct);
}
