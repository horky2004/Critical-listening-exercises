using CriticalListeningLab.Api.Features.TestSessions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CriticalListeningLab.Api.Features.Errors;

public sealed class AppExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not AppException app)
        {
            return false;
        }

        httpContext.Response.StatusCode = app.StatusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Type = $"https://cll.algebra.hr/errors/{app.ErrorCode}",
                Title = TitleFor(app),
                Status = app.StatusCode,
                Detail = app.Message,
                Instance = httpContext.Request.Path
            },
            cancellationToken);

        return true;
    }

    private static string TitleFor(AppException exception) => exception switch
    {
        NotFoundException => "Nije pronadeno",
        ForbiddenException => "Zabranjeno",
        ConflictException => "Sukob",
        BadRequestException => "Neispravan zahtjev",
        _ => "Greska"
    };
}
