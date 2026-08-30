using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql;
using Tubeshade.Data;
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

    public async ValueTask<PlaylistEntity> Create(
        Guid libraryId,
        Guid userId,
        string name,
        string externalId,
        string externalUrl)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction();
        var playlist = await Create(libraryId, userId, name, externalId, externalUrl, transaction);
        await transaction.CommitAsync();

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
