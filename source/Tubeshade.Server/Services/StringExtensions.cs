using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using NodaTime;
using NodaTime.Text;

namespace Tubeshade.Server.Services;

public static partial class StringExtensions
{
    public static readonly IPattern<Duration> Pattern = new CompositePatternBuilder<Duration>
    {
        { DurationPattern.CreateWithInvariantCulture("h:mm:ss"), duration => duration.Days is 0 },
        { DurationPattern.CreateWithInvariantCulture("M:ss"), duration => duration.Hours is 0 }
    }.Build();

    public static bool TryExtractChapters(
        [NotNullWhen(true)] this string? description,
        Period videoDuration,
        [MaybeNullWhen(false)] out TextTrackCue[] chapters)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            chapters = null;
            return false;
        }

        var regex = TimestampRegex();
        var timestamps = new List<(Duration Timestamp, string Label)>();

        foreach (var line in description.Split(Environment.NewLine))
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                timestamps.Add((Pattern.Parse(match.Groups[1].Value).Value, match.Groups[2].Value));
            }
            else if (timestamps.Count is 1)
            {
                timestamps.Clear();
            }
            else if (timestamps.Count > 1)
            {
                break;
            }
        }

        if (timestamps.Count <= 1)
        {
            chapters = null;
            return false;
        }

        chapters = new TextTrackCue[timestamps.Count];

        for (var index = 0; index < timestamps.Count; index++)
        {
            var (timestamp, label) = timestamps[index];
            var nextTimestamp = index == timestamps.Count - 1
                ? videoDuration.ToDuration()
                : timestamps[index + 1].Timestamp;

            chapters[index] = new TextTrackCue(timestamp, nextTimestamp, label);
        }

        return true;
    }

    [GeneratedRegex(@"^\s*(\d{0,2}:?\d{1,2}:\d{2})\s{1,}(.*)$")]
    private static partial Regex TimestampRegex();
}
