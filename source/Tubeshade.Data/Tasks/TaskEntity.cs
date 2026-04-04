using System;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Tasks;

public sealed record TaskEntity : ModifiableEntity, IOwnableEntity
{
    /// <inheritdoc />
    public required Guid OwnerId { get; set; }

    public required TaskType Type { get; init; }

    public Guid? UserId { get; init; }

    public Guid? LibraryId { get; set; }

    public Guid? ChannelId { get; set; }

    public Guid? VideoId { get; set; }

    public string? Url { get; init; }

    public bool AllVideos { get; init; }

    public (Guid LibraryId, Guid UserId) DestructureLibraryTask()
    {
        return this is { LibraryId: { } libraryId, UserId: { } userId }
            ? (libraryId, userId)
            : throw new InvalidOperationException("Task is not for a library");
    }

    public static TaskEntity Index(Guid libraryId, Guid userId, string url) => new()
    {
        CreatedByUserId = userId,
        ModifiedByUserId = userId,
        OwnerId = userId,
        Type = TaskType.Index,
        UserId = userId,
        LibraryId = libraryId,
        ChannelId = null,
        VideoId = null,
        Url = url,
        AllVideos = false,
    };

    public static TaskEntity Index(Guid libraryId, Guid userId, Guid channelId, Guid videoId, string url) => new()
    {
        CreatedByUserId = userId,
        ModifiedByUserId = userId,
        OwnerId = userId,
        Type = TaskType.Index,
        UserId = userId,
        LibraryId = libraryId,
        ChannelId = channelId,
        VideoId = videoId,
        Url = url,
        AllVideos = false,
    };

    public static TaskEntity Download(Guid libraryId, Guid userId, Guid videoId) => new()
    {
        CreatedByUserId = userId,
        ModifiedByUserId = userId,
        OwnerId = userId,
        Type = TaskType.DownloadVideo,
        UserId = userId,
        LibraryId = libraryId,
        ChannelId = null,
        VideoId = videoId,
        Url = null,
        AllVideos = false,
    };

    public static TaskEntity ScanChannel(Guid libraryId, Guid userId, Guid channelId, bool allVideos) => new()
    {
        CreatedByUserId = userId,
        ModifiedByUserId = userId,
        OwnerId = userId,
        Type = TaskType.ScanChannel,
        UserId = userId,
        LibraryId = libraryId,
        ChannelId = channelId,
        VideoId = null,
        Url = null,
        AllVideos = allVideos,
    };

    public static TaskEntity ScanSubscriptions(Guid libraryId, Guid userId) =>
        LibraryTask(libraryId, userId, TaskType.ScanSubscriptions);

    public static TaskEntity ScanSegments(Guid libraryId, Guid userId) =>
        LibraryTask(libraryId, userId, TaskType.ScanSponsorBlockSegments);

    public static TaskEntity UpdateSegments(Guid libraryId, Guid userId) =>
        LibraryTask(libraryId, userId, TaskType.UpdateSponsorBlockSegments);

    private static TaskEntity LibraryTask(Guid libraryId, Guid userId, TaskType type) => new()
    {
        CreatedByUserId = userId,
        ModifiedByUserId = userId,
        OwnerId = userId,
        Type = type,
        UserId = userId,
        LibraryId = libraryId,
        ChannelId = null,
        VideoId = null,
        Url = null,
        AllVideos = false,
    };
}
