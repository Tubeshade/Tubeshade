using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using SponsorBlock;
using Tubeshade.Data.Media;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Videos;

public static class VideoEntityExtensions
{
    public static string GetSubtitlesFilePath(this VideoEntity video, string language = "en")
    {
        return video.GetFilePath($"subtitles.{language}.vtt");
    }

    public static string GetChaptersFilePath(this VideoEntity video)
    {
        return video.GetFilePath("chapters.vtt");
    }

    public static string GetDirectoryPath(this VideoEntity video)
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

    public static string GetFilePath(this VideoEntity video, string fileName)
    {
        var attributes = File.GetAttributes(video.StoragePath);
        return (attributes & FileAttributes.Directory) is FileAttributes.Directory
            ? Path.Combine(video.StoragePath, fileName)
            : Path.Combine(Path.GetDirectoryName(video.StoragePath) ?? string.Empty, fileName);
    }

    [LinqTunnel]
    public static IEnumerable<VideoModel> MapToModels(
        this IEnumerable<VideoEntity> videos,
        List<SponsorBlockSegmentEntity> segments,
        List<ChannelEntity> channels)
    {
        var channelDictionary = channels.ToDictionary(channel => channel.Id);
        return videos.Select(video => video.MapToModel(segments, channelDictionary));
    }

    private static VideoModel MapToModel(
        this VideoEntity video,
        List<SponsorBlockSegmentEntity> segments,
        Dictionary<Guid, ChannelEntity> channels)
    {
        var skippedDuration = segments
            .Where(segment => segment.VideoId == video.Id && segment.Category != SegmentCategory.Filler)
            .GetTotalDuration();

        var actualDuration = video.Duration is { } duration
            ? (duration - skippedDuration).Normalize()
            : null;

        return new VideoModel
        {
            Video = video,
            ActualDuration = actualDuration,
            Channel = channels[video.ChannelId], // todo: this probably should be done in SQL instead of in-memory
        };
    }
}
