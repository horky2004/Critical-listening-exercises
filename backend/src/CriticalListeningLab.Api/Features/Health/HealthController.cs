using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CriticalListeningLab.Api.Features.Health;

[ApiController]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Jedini javni endpoint. Ne dira bazu - provjerava samo da proces radi,
    /// pa je koristan i kad je baza nedostupna.
    /// </summary>
    [HttpGet("/health")]
    [AllowAnonymous]
    public IActionResult Get() => Ok(new { status = "ok" });
}
