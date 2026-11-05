using System;
using System.Linq;
using YoutubeDLSharp.Metadata;

namespace Tubeshade.Server.Services;

/// <summary>Detects yt-dlp failures that are only reported as warnings, because errors are ignored.</summary>
public static class YtdlpErrorOutput
{
    // The rest of the message contains a typographic apostrophe ("you’re"), so only match the start
    private const string SignInRequired = "Sign in to confirm you";
    private const string InvalidCookies = "cookies are no longer valid";

    /// <summary>
    /// Throws if YouTube blocked fetching video data with a sign-in check,
    /// so that the incomplete data is not mistaken for an unavailable video.
    /// </summary>
    /// <param name="errorOutput">Standard error output of yt-dlp.</param>
    /// <param name="data">The data returned by yt-dlp, if any.</param>
    /// <exception cref="InvalidOperationException">YouTube requires signing in.</exception>
    public static void ThrowIfSignInRequired(string?[] errorOutput, VideoData? data)
    {
        if (data?.Formats is { Length: > 0 })
        {
            return;
        }

        var output = string.Join('\n', errorOutput.OfType<string>());
        if (!output.Contains(SignInRequired, StringComparison.Ordinal))
        {
            return;
        }

        var message = output.Contains(InvalidCookies, StringComparison.Ordinal)
            ? "YouTube requires signing in to confirm you're not a bot, and the YouTube cookies for this library are no longer valid. Export fresh cookies and try again."
            : "YouTube requires signing in to confirm you're not a bot. Provide valid YouTube cookies for this library and try again.";

        throw new InvalidOperationException(message);
    }
}