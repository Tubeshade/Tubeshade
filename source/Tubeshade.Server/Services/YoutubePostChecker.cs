using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static System.Net.Http.HttpCompletionOption;
using static System.Net.HttpStatusCode;
using static System.StringComparison;

namespace Tubeshade.Server.Services;

public sealed class YoutubePostChecker
{
    private readonly ILogger<YoutubePostChecker> _logger;
    private readonly HttpClient _httpClient;

    public YoutubePostChecker(ILogger<YoutubePostChecker> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async ValueTask<bool> IsYouTubePost(string videoUrl, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!uri.Host.Equals("youtube.com", OrdinalIgnoreCase) &&
            !uri.Host.Equals("youtu.be", OrdinalIgnoreCase) &&
            !uri.Host.EndsWith(".youtube.com", OrdinalIgnoreCase))
        {
            return false;
        }

        if (uri.PathAndQuery.Contains("/post/", OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            using var response = await _httpClient.GetAsync(videoUrl, ResponseHeadersRead, cancellationToken);
            return response.StatusCode is Moved or Found or SeeOther or TemporaryRedirect or PermanentRedirect &&
                   response.Headers.Location is { } location &&
                   location.PathAndQuery.Contains("/post/", OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            _logger.YouTubeUriCheckFailed(exception, videoUrl);
            return false;
        }
    }
}
