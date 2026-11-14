using System;
using Tubeshade.Data.Abstractions;

namespace Tubeshade.Data.Tasks;

public sealed record TaskEntity : ModifiableEntity, IOwnableEntity
{
    /// <inheritdoc />
    /// <remarks>
    /// A task for a library always belongs to the library's owner, which is set when the task is added;
    /// this is only used for tasks without a library.
    /// </remarks>
    public long OwnerId { get; set; }

    public required TaskType Type { get; init; }

    public long? UserId { get; init; }

    public Guid? LibraryId { get; set; }

    public Guid? ChannelId { get; set; }

    public Guid? PlaylistId { get; set; }

    public Guid? VideoId { get; set; }

    public string? Url { get; init; }

    public bool AllVideos { get; init; }

    public string? Payload { get; init; }

    public (Guid LibraryId, long UserId) DestructureLibraryTask()
    {
        return this is { LibraryId: { } libraryId, UserId: { } userId }
            ? (libraryId, userId)
            : throw new InvalidOperationException("Task is not for a library");
    }

    public static TaskEntity Index(Guid libraryId, long userId, string url) =>
        Index(libraryId, userId, null, null, url);

    public static TaskEntity Index(Guid libraryId, long userId, Guid? channelId, Guid? videoId, string url) => new()
    {
        CreatedByUserId = userId,
        ModifiedByUserId = userId,
        Type = TaskType.Index,
        UserId = userId,
        LibraryId = libraryId,
        ChannelId = channelId,
        PlaylistId = null,
        VideoId = videoId,
        Url = url,
        AllVideos = false,
        Payload = null,
    };

    public static TaskEntity YouTubeFeedUpdate(Guid libraryId, long userId, Guid channelId, string payload) => new()
    {
        CreatedByUserId = userId,
        ModifiedByUserId = userId,
        Type = TaskType.YouTubeFeedUpdate,
        UserId = userId,
        LibraryId = libraryId,
        ChannelId = channelId,
        PlaylistId = null,
        VideoId = null,
        Url = null,
        AllVideos = false,
        Payload = payload,
    };

    public static TaskEntity Download(Guid libraryId, long userId, Guid videoId) => new()
    {
        CreatedByUserId = userId,
        ModifiedByUserId = userId,
        Type = TaskType.DownloadVideo,
        UserId = userId,
        LibraryId = libraryId,
        ChannelId = null,
        PlaylistId = null,
        VideoId = videoId,
        Url = null,
        AllVideos = false,
        Payload = null,
    };

    public static TaskEntity ScanChannel(Guid libraryId, long userId, Guid channelId, bool allVideos) => new()
    {
        CreatedByUserId = userId,
        ModifiedByUserId = userId,
        Type = TaskType.ScanChannel,
        UserId = userId,
        LibraryId = libraryId,
        ChannelId = channelId,
        PlaylistId = null,
        VideoId = null,
        Url = null,
        AllVideos = allVideos,
        Payload = null,
    };

    public static TaskEntity ScanPlaylist(Guid libraryId, long userId, Guid playlistId, bool allVideos) => new()
    {
        CreatedByUserId = userId,
        ModifiedByUserId = userId,
        Type = TaskType.ScanPlaylist,
        UserId = userId,
        LibraryId = libraryId,
        ChannelId = null,
        PlaylistId = playlistId,
        VideoId = null,
        Url = null,
        AllVideos = allVideos,
        Payload = null,
    };

    public static TaskEntity ScanSubscriptions(Guid libraryId, long userId) =>
        LibraryTask(libraryId, userId, TaskType.ScanSubscriptions);

    public static TaskEntity ScanSegments(Guid libraryId, long userId) =>
        LibraryTask(libraryId, userId, TaskType.ScanSponsorBlockSegments);

    public static TaskEntity UpdateSegments(Guid libraryId, long userId) =>
        LibraryTask(libraryId, userId, TaskType.UpdateSponsorBlockSegments);

    public static TaskEntity RefreshMetadata(Guid libraryId, long userId) =>
        LibraryTask(libraryId, userId, TaskType.RefreshFileMetadata);

    public static TaskEntity RefreshTracks(Guid libraryId, long userId) =>
        LibraryTask(libraryId, userId, TaskType.RefreshTrackFiles);

    public static TaskEntity RemuxVideos(Guid libraryId, long userId) =>
        LibraryTask(libraryId, userId, TaskType.RemuxVideoFiles);

    private static TaskEntity LibraryTask(Guid libraryId, long userId, TaskType type) => new()
    {
        CreatedByUserId = userId,
        ModifiedByUserId = userId,
        Type = type,
        UserId = userId,
        LibraryId = libraryId,
        ChannelId = null,
        PlaylistId = null,
        VideoId = null,
        Url = null,
        AllVideos = false,
        Payload = null,
    };
}
