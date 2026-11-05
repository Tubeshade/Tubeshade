using System;
using System.Linq;
using System.Text.RegularExpressions;
using YoutubeDLSharp.Metadata;

namespace Tubeshade.Server.Services;

/// <summary>Detects yt-dlp failures that are only reported as warnings, because errors are ignored.</summary>
public static partial class YtdlpErrorOutput
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

        throw new InvalidOperationException(GetSignInMessage(output));
    }

    /// <summary>Gets a message describing why yt-dlp could not get the requested video.</summary>
    /// <param name="errorOutput">Standard error output of yt-dlp.</param>
    /// <returns>
    /// An explanation for known reasons, otherwise the warnings and errors reported by yt-dlp,
    /// without the verbose debug output.
    /// </returns>
    public static string GetErrorMessage(string?[] errorOutput)
    {
        var lines = errorOutput
            .OfType<string>()
            .SelectMany(line => line.Split('\n'))
            .Select(line => line.Trim())
            .Where(line => line is not "")
            .ToArray();

        if (lines.Select(line => MembersOnlyRegex().Match(line)).FirstOrDefault(match => match.Success) is
            { } membersOnly)
        {
            var level = membersOnly.Groups["level"] is { Success: true } group
                ? $" on at least the \"{group.Value}\" level"
                : string.Empty;

            return
                $"This video is only available to members of the channel. The YouTube account in the cookies for this library needs to be a member{level} to download it.";
        }

        var output = string.Join('\n', lines);
        if (output.Contains(SignInRequired, StringComparison.Ordinal))
        {
            return GetSignInMessage(output);
        }

        var messages = lines
            .Where(line => line.StartsWith("ERROR:", StringComparison.Ordinal) ||
                           line.StartsWith("WARNING:", StringComparison.Ordinal))
            .Distinct()
            .ToArray();

        return messages is []
            ? string.Join(Environment.NewLine, lines)
            : string.Join(Environment.NewLine, messages);
    }

    private static string GetSignInMessage(string output) => output.Contains(InvalidCookies, StringComparison.Ordinal)
        ? "YouTube requires signing in to confirm you're not a bot, and the YouTube cookies for this library are no longer valid. Export fresh cookies and try again."
        : "YouTube requires signing in to confirm you're not a bot. Provide valid YouTube cookies for this library and try again.";

    // The apostrophe in "channel's" may be typographic, so match any character
    [GeneratedRegex(@"available to this channel.s members(?: on level: (?<level>.+?) \(or any higher level\))?",
        RegexOptions.CultureInvariant)]
    private static partial Regex MembersOnlyRegex();
}
