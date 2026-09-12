using CriticalListeningLab.Api.Domain.Progression;

namespace CriticalListeningLab.Api.Features.Progress;

public interface IProgressionService
{
    Task<SourceTreeState> GetTreeStateAsync(Guid userId, Guid audioSourceId, CancellationToken ct);

    Task<bool> IsUnlockedAsync(Guid userId, Guid audioSourceId, Guid levelId, CancellationToken ct);

    Task<ProgressUpdateResult> ApplyTestResultAsync(
        Guid userId, Guid audioSourceId, Guid levelId, int correctAnswers, CancellationToken ct);
}
