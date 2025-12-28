using System;
using System.IO;
using System.Threading.Tasks;
using NodaTime;
using Npgsql;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Services;

public sealed class VideoService
{
    private readonly VideoRepository _videoRepository;

    public VideoService(VideoRepository videoRepository)
    {
        _videoRepository = videoRepository;
    }

    public async ValueTask<VideoEntity> Create(
        Guid userId,
        ChannelEntity channel,
        Guid ownerId,
        string name,
        string description,
        string[] categories,
        string[] tags,
        VideoType type,
        string externalId,
        string externalUrl,
        Instant publishedAt,
        Instant refreshedAt,
        ExternalAvailability availability,
        Period duration,
        long? views,
        long? likes,
        NpgsqlTransaction transaction)
    {
        var videoId = await _videoRepository.AddAsync(
            new VideoEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = ownerId,
                Name = name,
                Description = description,
                Categories = categories,
                Tags = tags,
                Type = type,
                ViewCount = views,
                LikeCount = likes,
                ChannelId = channel.Id,
                StoragePath = channel.StoragePath,
                ExternalId = externalId,
                ExternalUrl = externalUrl,
                PublishedAt = publishedAt,
                RefreshedAt = refreshedAt,
                Availability = availability,
                Duration = duration,
            },
            transaction);

        var video = await _videoRepository.GetAsync(videoId!.Value, userId, transaction);

        video.StoragePath = Path.Combine(channel.StoragePath, $"video_{video.Id}");
        await _videoRepository.UpdateAsync(video, transaction);

        Directory.CreateDirectory(video.StoragePath);

        return video;
    }
}
