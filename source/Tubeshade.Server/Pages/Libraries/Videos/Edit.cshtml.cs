using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;

namespace Tubeshade.Server.Pages.Libraries.Videos;

public sealed class Edit : LibraryPageBase
{
    private readonly NpgsqlConnection _connection;
    private readonly VideoRepository _videoRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly LibraryRepository _libraryRepository;

    public Edit(
        NpgsqlConnection connection,
        VideoRepository videoRepository,
        ChannelRepository channelRepository,
        LibraryRepository libraryRepository)
    {
        _connection = connection;
        _videoRepository = videoRepository;
        _channelRepository = channelRepository;
        _libraryRepository = libraryRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid VideoId { get; set; }

    public VideoEntity Entity { get; set; } = null!;

    public List<VideoFileEntity> Files { get; set; } = [];

    public ChannelEntity Channel { get; set; } = null!;

    public LibraryEntity Library { get; set; } = null!;

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        await using var transaction = await _connection.OpenAndBeginTransaction(IsolationLevel.RepeatableRead, cancellationToken);

        Entity = await _videoRepository.GetAsync(VideoId, userId, transaction);
        Files = await _videoRepository.GetFilesAsync(VideoId, userId, transaction, cancellationToken);

        Channel = await _channelRepository.GetAsync(Entity.ChannelId, userId, transaction);
        Library = await _libraryRepository.GetAsync(LibraryId, userId, transaction);

        await transaction.CommitAsync(cancellationToken);
    }
}
