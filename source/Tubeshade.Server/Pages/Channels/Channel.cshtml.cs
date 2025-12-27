using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Pages.Channels;

public sealed class Channel : PageModel
{
    private readonly ChannelRepository _channelRepository;

    public Channel(ChannelRepository channelRepository)
    {
        _channelRepository = channelRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid ChannelId { get; set; }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var libraryId = await _channelRepository.GetPrimaryLibraryId(ChannelId, cancellationToken);
        return RedirectToPage("/Libraries/Channels/Channel", new { libraryId, ChannelId });
    }
}
