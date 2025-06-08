using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;

namespace Tubeshade.Server.Pages.Videos;

public sealed class Video : PageModel
{
    private readonly VideoRepository _videoRepository;
    private readonly ChannelRepository _channelRepository;

    public Video(VideoRepository videoRepository, ChannelRepository channelRepository)
    {
        _videoRepository = videoRepository;
        _channelRepository = channelRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid VideoId { get; set; }

    public VideoEntity Entity { get; set; } = null!;

    public ChannelEntity Channel { get; set; } = null!;

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Entity = await _videoRepository.GetAsync(VideoId, userId, cancellationToken);
        Channel = await _channelRepository.GetAsync(Entity.ChannelId, userId, cancellationToken);
    }
}
