using CriticalListeningLab.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CriticalListeningLab.Api.Features.TestSessions;

[ApiController]
[Route("api/test-sessions")]
[Authorize(AuthPolicies.RequireStudent)]
public class TestSessionsController(ITestSessionService sessions, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SessionView>> Start(StartSessionRequest request, CancellationToken ct)
    {
        var session = await sessions.StartAsync(
            currentUser.Id, request.ModuleSlug, request.SourceSlug, request.LevelId, ct);
        return CreatedAtAction(nameof(Get), new { sessionId = session.SessionId }, session);
    }

    [HttpGet("{sessionId:guid}")]
    public Task<SessionView> Get(Guid sessionId, CancellationToken ct) =>
        sessions.GetAsync(currentUser.Id, sessionId, ct);

    [HttpPost("{sessionId:guid}/questions/{index:int}/answer")]
    public Task<AnswerView> Answer(
        Guid sessionId, int index, AnswerRequest request, CancellationToken ct) =>
        sessions.AnswerAsync(currentUser.Id, sessionId, index, request.AnswerKey, ct);

    [HttpPost("{sessionId:guid}/abandon")]
    public async Task<IActionResult> Abandon(Guid sessionId, CancellationToken ct)
    {
        await sessions.AbandonAsync(currentUser.Id, sessionId, ct);
        return NoContent();
    }
}

public sealed record StartSessionRequest(string ModuleSlug, string SourceSlug, Guid LevelId);

public sealed record AnswerRequest(string AnswerKey);
