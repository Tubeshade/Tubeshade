using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;

namespace Tubeshade.Server.Pages.Videos;

public sealed class Edit : PageModel
{
    private readonly VideoRepository _videoRepository;
    private readonly ChannelRepository _channelRepository;

    public Edit(VideoRepository videoRepository, ChannelRepository channelRepository)
    {
        _videoRepository = videoRepository;
        _channelRepository = channelRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid VideoId { get; set; }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var video = await _videoRepository.GetAsync(VideoId, User.GetUserId(), cancellationToken);
        var libraryId = await _channelRepository.GetPrimaryLibraryId(video.ChannelId, cancellationToken);
        return RedirectToPage("/Libraries/Videos/Edit", new { libraryId, VideoId });
    }
}
