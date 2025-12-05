using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Channels;

public sealed class Index : PageModel, INonLibraryPage
{
    private readonly ChannelRepository _channelRepository;
    private readonly LibraryRepository _libraryRepository;

    public Index(ChannelRepository channelRepository, LibraryRepository libraryRepository)
    {
        _channelRepository = channelRepository;
        _libraryRepository = libraryRepository;
    }

    /// <inheritdoc />
    public List<LibraryEntity> Libraries { get; private set; } = [];

    public List<ChannelEntity> Channels { get; private set; } = [];

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Libraries = await _libraryRepository.GetAsync(userId, cancellationToken);
        Channels = (await _channelRepository.GetAsync(userId, cancellationToken))
            .OrderBy(channel => channel.Name)
            .ToList();
    }
}
