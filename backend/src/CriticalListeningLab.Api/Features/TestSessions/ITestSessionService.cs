namespace CriticalListeningLab.Api.Features.TestSessions;

public interface ITestSessionService
{
    Task<SessionView> StartAsync(
        Guid userId, string moduleSlug, string sourceSlug, Guid levelId, CancellationToken ct);

    Task<SessionView> GetAsync(Guid userId, Guid sessionId, CancellationToken ct);

    Task<AnswerView> AnswerAsync(
        Guid userId, Guid sessionId, int questionIndex, string answerKey, CancellationToken ct);

    Task AbandonAsync(Guid userId, Guid sessionId, CancellationToken ct);
}
