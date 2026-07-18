using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Data.Media.Videos;
using Tubeshade.Server.Configuration.Auth;

namespace Tubeshade.Server.Pages.Videos;

public sealed class Edit : PageModel
{
    private readonly NpgsqlConnection _connection;
    private readonly VideoRepository _videoRepository;
    private readonly ChannelRepository _channelRepository;

    public Edit(NpgsqlConnection connection, VideoRepository videoRepository, ChannelRepository channelRepository)
    {
        _connection = connection;
        _videoRepository = videoRepository;
        _channelRepository = channelRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid VideoId { get; set; }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var video = await _videoRepository.GetAsync(VideoId, User.GetUserId(), transaction);
        var libraryId = await _channelRepository.GetPrimaryLibraryId(video.ChannelId, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return RedirectToPage("/Libraries/Videos/Edit", new { libraryId, VideoId });
    }
}
