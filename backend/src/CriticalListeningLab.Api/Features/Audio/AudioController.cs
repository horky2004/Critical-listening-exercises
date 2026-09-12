using System.Diagnostics;
using CriticalListeningLab.Api.Auth;
using CriticalListeningLab.Api.Data;
using CriticalListeningLab.Api.Features.Modules;
using CriticalListeningLab.Api.Features.TestSessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CriticalListeningLab.Api.Features.Audio;

[ApiController]
[Route("api/audio")]
[Authorize(AuthPolicies.RequireStudent)]
public class AudioController(
    AppDbContext db,
    IModuleAccessService access,
    IAudioStorage storage,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("assets/{assetId:guid}")]
    public async Task<IActionResult> Asset(Guid assetId, CancellationToken ct)
    {
        var asset = await db.AudioAssets
            .Include(a => a.AudioSource)
            .ThenInclude(s => s.Module)
            .FirstOrDefaultAsync(a => a.Id == assetId && a.IsEnabled, ct)
                    ?? throw new NotFoundException("Audio asset nije pronaden.");

        if (!await access.IsModuleAvailableAsync(currentUser.Id, asset.AudioSource.Module.Slug, ct))
        {
            throw new ForbiddenException("Modul nije dostupan.", "module-unavailable");
        }

        return await DeliverAsync(asset.StorageKey, asset.MimeType, cachePublicly: true, ct);
    }

    [HttpGet("questions/{audioToken:guid}")]
    public async Task<IActionResult> Question(Guid audioToken, CancellationToken ct)
    {
        var question = await db.TestSessionQuestions
            .Include(q => q.TestSession)
            .Include(q => q.AudioAsset)
            .FirstOrDefaultAsync(q => q.AudioToken == audioToken, ct)
                       ?? throw new NotFoundException("Audio pitanja nije pronaden.");

        if (question.TestSession.UserId != currentUser.Id)
        {
            throw new ForbiddenException("Sesija nije tvoja.", "session-not-owned");
        }

        if (question.AudioAsset is null || !question.AudioAsset.IsEnabled)
        {
            throw new NotFoundException("Audio pitanja nije pronaden.");
        }

        return await DeliverAsync(question.AudioAsset.StorageKey, question.AudioAsset.MimeType, cachePublicly: false, ct);
    }

    private async Task<IActionResult> DeliverAsync(
        string storageKey, string mimeType, bool cachePublicly, CancellationToken ct)
    {
        var delivery = await storage.GetAsync(storageKey, ct);
        switch (delivery)
        {
            case AudioDelivery.Stream stream:
                Response.Headers.ETag = stream.ETag;
                Response.Headers.CacheControl = cachePublicly
                    ? "private, max-age=604800, immutable"
                    : "no-store";
                Response.Headers.AcceptRanges = "bytes";
                return File(stream.Content, mimeType, enableRangeProcessing: true);
            case AudioDelivery.Redirect redirect:
                return Redirect(redirect.Url);
            default:
                throw new UnreachableException();
        }
    }
}
