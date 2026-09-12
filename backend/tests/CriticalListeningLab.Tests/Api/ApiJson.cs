using System.Text.Json;
using System.Text.Json.Serialization;

namespace CriticalListeningLab.Tests.Api;

internal static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}
