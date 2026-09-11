using CriticalListeningLab.Api.Auth;
using CriticalListeningLab.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CriticalListeningLab.Api.Features.Me;

[ApiController]
[Route("api/me")]
[Authorize(AuthPolicies.RequireStudent)]
public class MeController(AppDbContext db, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Profil prijavljenog korisnika. Ujedno dokazuje da je JIT provisioning
    /// odradio svoje - ako ovaj endpoint vrati podatke, korisnik postoji u bazi.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken ct)
    {
        var user = await db.Users
            .Where(u => u.Id == currentUser.Id)
            .Select(u => new MeResponse(
                u.Id,
                u.Email,
                u.DisplayName,
                u.Role.ToString(),
                u.Cohort == null ? null : new CohortSummary(u.Cohort.Id, u.Cohort.Name)))
            .FirstOrDefaultAsync(ct);

        // Korisnik je autentificiran i provisioniran, pa ovo znaci da je red
        // obrisan izmedu transformacije i ovog upita.
        return user is null ? NotFound() : Ok(user);
    }
}

public record MeResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    CohortSummary? Cohort);

public record CohortSummary(Guid Id, string Name);
