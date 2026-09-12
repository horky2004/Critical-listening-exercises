namespace CriticalListeningLab.Api.Features.TestSessions;

public abstract class AppException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class NotFoundException(string message) : AppException(404, message);

public sealed class ForbiddenException(string message) : AppException(403, message);

public sealed class ConflictException(string message) : AppException(409, message);

public sealed class BadRequestException(string message) : AppException(400, message);
