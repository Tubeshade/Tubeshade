using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;

namespace Tubeshade.Server.Pages.Channels;

public sealed class Index : PageModel
{
    private readonly ChannelRepository _channelRepository;

    public Index(ChannelRepository channelRepository)
    {
        _channelRepository = channelRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? LibraryId { get; set; }

    public List<ChannelEntity> Channels { get; set; } = [];

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Channels = await _channelRepository.GetAsync(userId, cancellationToken);
    }
}
