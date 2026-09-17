using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Playlists;

namespace Tubeshade.Server.Services;

public sealed class PlaylistService
{
    private readonly ILogger<PlaylistService> _logger;
    private readonly NpgsqlConnection _connection;
    private readonly IClock _clock;
    private readonly LibraryRepository _libraryRepository;
    private readonly PlaylistRepository _playlistRepository;

    public PlaylistService(
        ILogger<PlaylistService> logger,
        NpgsqlConnection connection,
        IClock clock,
        LibraryRepository libraryRepository,
        PlaylistRepository playlistRepository)
    {
        _logger = logger;
        _connection = connection;
        _clock = clock;
        _libraryRepository = libraryRepository;
        _playlistRepository = playlistRepository;
    }

    /// <summary>Subscribes to the playlist so that it is refreshed as part of the scheduled subscription scans.</summary>
    public async ValueTask<PlaylistEntity> Subscribe(Guid playlistId, Guid userId)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction();
        var playlist = await SetSubscribed(playlistId, userId, _clock.GetCurrentInstant(), transaction);
        await transaction.CommitAsync();

        return playlist;
    }

    public async ValueTask<PlaylistEntity> Unsubscribe(Guid playlistId, Guid userId)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction();
        var playlist = await SetSubscribed(playlistId, userId, null, transaction);
        await transaction.CommitAsync();

        return playlist;
    }

    private async ValueTask<PlaylistEntity> SetSubscribed(
        Guid playlistId,
        Guid userId,
        Instant? subscribedAt,
        NpgsqlTransaction transaction)
    {
        var playlist = await _playlistRepository.FindAsync(playlistId, userId, Access.Modify, transaction);
        if (playlist is null)
        {
            throw new InvalidOperationException("Missing access to modify playlist");
        }

        playlist.SubscribedAt = subscribedAt;
        playlist.ModifiedByUserId = userId;
        playlist.ModifiedAt = _clock.GetCurrentInstant();

        var updatedCount = await _playlistRepository.UpdateAsync(playlist, transaction);
        Trace.Assert(updatedCount is not 0);

        return playlist;
    }

    public async ValueTask<PlaylistEntity> Create(
        Guid libraryId,
        Guid userId,
        string name,
        string externalId,
        string externalUrl,
        NpgsqlTransaction transaction)
    {
        var library = await _libraryRepository.GetAsync(libraryId, userId, transaction);
        _logger.CreatingPlaylist(name, externalId);

        var playlistId = await _playlistRepository.AddAsync(
            new PlaylistEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                LibraryId = libraryId,
                StoragePath = library.StoragePath,
                ExternalId = externalId,
                ExternalUrl = externalUrl,
                Name = name,
                RefreshedAt = _clock.GetCurrentInstant(),
            },
            transaction);

        var playlist = await _playlistRepository.GetAsync(playlistId!.Value, userId, transaction);

        playlist.StoragePath = Path.Combine(library.StoragePath, $"playlist_{playlist.Id}");
        await _playlistRepository.UpdateAsync(playlist, transaction);

        Directory.CreateDirectory(playlist.StoragePath);

        return playlist;
    }
}
