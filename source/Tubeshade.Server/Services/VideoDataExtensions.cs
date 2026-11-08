using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NodaTime;
using Tubeshade.Data.Media.Videos;
using YoutubeDLSharp.Metadata;
using static YoutubeDLSharp.Metadata.LiveStatus;

namespace Tubeshade.Server.Services;

public static partial class VideoDataExtensions
{
    extension(VideoData data)
    {
        public VideoType GetVideoType() => data switch
        {
            { LiveStatus: not (null or None or NotLive) } => VideoType.Livestream,
            _ => VideoType.Video,
        };

        public bool IsPlaylist() =>
            data is { ResultType: MetadataType.Playlist, WebpageUrl: { } webpageUrl } &&
            Uri.TryCreate(webpageUrl, UriKind.Absolute, out var uri) &&
            uri.AbsolutePath is "/playlist";

        /// <summary>Gets the earliest time at which the urls of the specified formats expire.</summary>
        /// <param name="formatIds">The ids of the formats.</param>
        /// <returns>The time at which the urls expire, or <see langword="null"/> if it is not known for all formats.</returns>
        public Instant? GetExpiry(IEnumerable<string> formatIds)
        {
            Instant? expiry = null;

            foreach (var formatId in formatIds)
            {
                if (data.Formats?.FirstOrDefault(format => format.FormatId == formatId) is not { } format ||
                    ExpiryRegex().Match(format.Url) is not { Success: true } match ||
                    !long.TryParse(match.Groups[1].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
                {
                    return null;
                }

                var formatExpiry = Instant.FromUnixTimeSeconds(seconds);
                if (expiry is null || formatExpiry < expiry)
                {
                    expiry = formatExpiry;
                }
            }

            return expiry;
        }

        public FormatData[] GetSelectedFormats()
        {
            var formats = data.FormatId!
                .Split('+')
                .Distinct()
                .Select(formatId => data.Formats!.Single(format => format.FormatId == formatId))
                .ToArray();

            // with multiple audio streams allowed yt-dlp also selects separate audio for a format that already has its own
            if (formats.FirstOrDefault(FormatDataExtensions.HasVideoAndAudio) is { } combinedFormat)
            {
                return [combinedFormat];
            }

            if (formats.All(format => format.IsAudioOnly() || format.Extension is not VideoContainerType.Names.WebM))
            {
                return formats;
            }

            // WebM can only contain Opus or Vorbis, so other audio cannot be merged in
            var compatibleFormats = formats.Where(format => !format.IsAudioOnly() || format.IsWebMAudio()).ToArray();
            if (compatibleFormats.Length == formats.Length || compatibleFormats.Any(FormatDataExtensions.IsAudioOnly))
            {
                return compatibleFormats;
            }

            // formats are sorted from worst to best
            var language = formats.First(FormatDataExtensions.IsAudioOnly).Language;
            return data.Formats!.LastOrDefault(format => format.IsWebMAudio() && format.Language == language) is { } replacement
                ? [.. compatibleFormats, replacement]
                : throw new InvalidOperationException($"Could not find audio in language '{language}' that can be merged into a WebM video");
        }
    }

    // YouTube format urls contain the expiry as a query parameter, and HLS manifest urls as a path segment
    [GeneratedRegex(@"(?:[?&]expire=|/expire/)(\d+)", RegexOptions.None, 100)]
    private static partial Regex ExpiryRegex();
}
