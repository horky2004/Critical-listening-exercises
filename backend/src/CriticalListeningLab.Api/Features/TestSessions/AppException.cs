namespace CriticalListeningLab.Api.Features.TestSessions;

public abstract class AppException(int statusCode, string message, string errorCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string ErrorCode { get; } = errorCode;
}

public sealed class NotFoundException(string message, string errorCode = "not-found")
    : AppException(404, message, errorCode);

public sealed class ForbiddenException(string message, string errorCode = "forbidden")
    : AppException(403, message, errorCode);

public sealed class ConflictException(string message, string errorCode = "conflict")
    : AppException(409, message, errorCode);

public sealed class BadRequestException(string message, string errorCode = "bad-request")
    : AppException(400, message, errorCode);
