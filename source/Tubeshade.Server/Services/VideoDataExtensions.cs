using System.Linq;
using YoutubeDLSharp.Metadata;

namespace Tubeshade.Server.Services;

public static class VideoDataExtensions
{
    public static ThumbnailData[] GetOrderedThumbnails(this VideoData videoData) => videoData
        .Thumbnails!
        .OrderByDescending(data => data.Preference)
        .ThenByDescending(data => data.Width)
        .ToArray();
}
