using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using NodaTime;
using SponsorBlock;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Data.Media.Videos;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Videos;

public static class VideoEntityExtensions
{
    extension(VideoEntity video)
    {
        public Period? GetActualDuration(IEnumerable<SponsorBlockSegmentEntity> segments)
        {
            if (video.Duration is not { } duration)
            {
                return null;
            }

            var skippedDuration = segments
                .Where(segment =>
                    segment.VideoId == video.Id &&
                    segment.Category != SegmentCategory.Filler &&
                    segment.Action != SegmentAction.Mute)
                .GetTotalDuration();

            return (duration - skippedDuration).Normalize();
        }

        public string GetChaptersFilePath()
        {
            return video.GetFilePath("chapters.vtt");
        }

        public string GetDirectoryPath()
        {
            var attributes = File.GetAttributes(video.StoragePath);
            var targetDirectory = (attributes & FileAttributes.Directory) is FileAttributes.Directory
                ? video.StoragePath
                : Path.GetDirectoryName(video.StoragePath);

            if (targetDirectory is null)
            {
                throw new InvalidOperationException("Video storage path does not contain a valid directory");
            }

            return targetDirectory;
        }

        public string GetFilePath(string fileName)
        {
            var attributes = File.GetAttributes(video.StoragePath);
            return (attributes & FileAttributes.Directory) is FileAttributes.Directory
                ? Path.Combine(video.StoragePath, fileName)
                : Path.Combine(Path.GetDirectoryName(video.StoragePath) ?? string.Empty, fileName);
        }
    }

    private static VideoModel MapToModel(
        this DetailedVideo video,
        IReadOnlyList<SponsorBlockSegmentEntity> segments,
        Dictionary<Guid, ChannelEntity> channels)
    {
        return new VideoModel
        {
            Video = video,
            ActualDuration = video.GetActualDuration(segments),
            Channel = channels[video.ChannelId], // todo: this probably should be done in SQL instead of in-memory
        };
    }

    [LinqTunnel]
    public static IEnumerable<VideoModel> MapToModels(
        this IEnumerable<DetailedVideo> videos,
        IReadOnlyList<SponsorBlockSegmentEntity> segments,
        IReadOnlyList<ChannelEntity> channels)
    {
        var channelDictionary = channels.ToDictionary(channel => channel.Id);
        return videos.Select(video => video.MapToModel(segments, channelDictionary));
    }
}
