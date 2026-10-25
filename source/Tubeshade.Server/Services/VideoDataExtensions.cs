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
    }

    // YouTube format urls contain the expiry as a query parameter, and HLS manifest urls as a path segment
    [GeneratedRegex(@"(?:[?&]expire=|/expire/)(\d+)", RegexOptions.None, 100)]
    private static partial Regex ExpiryRegex();
}
