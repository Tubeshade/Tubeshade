using Tubeshade.Data.Media;
using YoutubeDLSharp.Metadata;
using static YoutubeDLSharp.Metadata.LiveStatus;

namespace Tubeshade.Server.Services;

public static class VideoDataExtensions
{
    extension(VideoData data)
    {
        public VideoType GetVideoType() => data switch
        {
            { LiveStatus: not (null or None or NotLive) } => VideoType.Livestream,
            _ => VideoType.Video,
        };
    }
}
