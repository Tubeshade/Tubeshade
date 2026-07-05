using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Tubeshade.Server.V1;

internal static class ResponseExtensions
{
    /// <summary>Replaces any existing cache headers with no-cache, no-store.</summary>
    /// <remarks>Taken from <see cref="ResponseCacheAttribute"/> implementation.</remarks>
    internal static void NoCache(this HttpResponse response)
    {
        response.Headers.Remove(HeaderNames.Vary);
        response.Headers.Remove(HeaderNames.CacheControl);
        response.Headers.Remove(HeaderNames.Pragma);

        // this header is not used by ResponseCacheAttribute, and Cache-Control: max-age is preferred, but is removed for correctness
        response.Headers.Remove(HeaderNames.Expires);

        response.Headers.CacheControl = "no-store, no-cache";
        response.Headers.Pragma = "no-cache";
    }
}
