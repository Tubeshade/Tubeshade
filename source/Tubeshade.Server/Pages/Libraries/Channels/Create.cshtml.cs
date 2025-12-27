using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Pages.Libraries.Channels;

public sealed class Create : LibraryPageBase, IFormLayout
{
    private readonly LibraryRepository _libraryRepository;
    private readonly ChannelService _channelService;

    public Create(LibraryRepository libraryRepository, ChannelService channelService)
    {
        _libraryRepository = libraryRepository;
        _channelService = channelService;
    }

    public LibraryEntity Library { get; set; } = null!;

    [BindProperty]
    public CreateChannelModel? CreateChannel { get; set; }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        Library = await _libraryRepository.GetAsync(LibraryId, User.GetUserId(), cancellationToken);
        CreateChannel ??= new();

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        CreateChannel ??= new();
        if (!ModelState.IsValid)
        {
            return await OnGet(CancellationToken.None);
        }

        var channel = await _channelService.Create(
            LibraryId,
            User.GetUserId(),
            CreateChannel.Name,
            CreateChannel.ExternalId,
            CreateChannel.ExternalUrl,
            CreateChannel.Availability);

        return RedirectToPage("/Libraries/Channels/Channel", new { LibraryId, channelId = channel.Id });
    }
}
