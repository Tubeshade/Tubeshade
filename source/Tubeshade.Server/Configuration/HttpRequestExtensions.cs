using Microsoft.AspNetCore.Http;

namespace Tubeshade.Server.Configuration;

internal static class HttpRequestExtensions
{
    private static readonly PathString Prefix = PathString.FromUriComponent("/api");

    internal static bool IsApiRequest(this HttpRequest request) => request.Path.StartsWithSegments(Prefix);
}
