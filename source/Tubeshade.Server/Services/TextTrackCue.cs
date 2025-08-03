using NodaTime;
using Tubeshade.Data.Media;
using YoutubeDLSharp.Metadata;

namespace Tubeshade.Server.Services;

public sealed class TextTrackCue
{
    public TextTrackCue(Duration startTime, Duration endTime, string text)
    {
        StartTime = startTime;
        EndTime = endTime;
        Text = text;
    }

    public Duration StartTime { get; }

    public Duration EndTime { get; }

    public string Text { get; }

    public static TextTrackCue FromYouTubeChapter(ChapterData chapter) => new(
        Duration.FromSeconds(chapter.StartTime ?? 0),
        Duration.FromSeconds(chapter.EndTime ?? 0),
        chapter.Title);

    public static TextTrackCue FromSponsorBlockSegment(SponsorBlockSegmentEntity segment) => new(
        Duration.FromSeconds((double)segment.StartTime),
        Duration.FromSeconds((double)segment.EndTime),
        segment.Category.Name);
}
