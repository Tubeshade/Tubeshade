using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Pages.Channels;

public sealed class Create : PageModel, IFormLayout, INonLibraryPage
{
    private readonly LibraryRepository _libraryRepository;
    private readonly ChannelService _channelService;

    public Create(LibraryRepository libraryRepository, ChannelService channelService)
    {
        _libraryRepository = libraryRepository;
        _channelService = channelService;
    }

    [BindProperty]
    public CreateChannelModel? CreateChannel { get; set; }

    /// <inheritdoc />
    public List<LibraryEntity> Libraries { get; private set; } = [];

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        Libraries = await _libraryRepository.GetAsync(User.GetUserId(), cancellationToken);
        CreateChannel ??= new();
        CreateChannel.Libraries = Libraries;

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        CreateChannel ??= new();
        if (!ModelState.IsValid)
        {
            return await OnGet(CancellationToken.None);
        }

        var libraryId = CreateChannel.LibraryId!.Value;
        var channel = await _channelService.Create(
            libraryId,
            User.GetUserId(),
            CreateChannel.Name,
            CreateChannel.ExternalId,
            CreateChannel.ExternalUrl,
            CreateChannel.Availability);

        return RedirectToPage("/Libraries/Channels/Channel", new { libraryId, channelId = channel.Id });
    }
}
