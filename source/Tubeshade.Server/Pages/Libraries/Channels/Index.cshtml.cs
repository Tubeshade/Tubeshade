using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;

namespace Tubeshade.Server.Pages.Libraries.Channels;

public sealed class Index : LibraryPageBase
{
    private readonly ChannelRepository _channelRepository;
    private readonly LibraryRepository _libraryRepository;

    public Index(ChannelRepository channelRepository, LibraryRepository libraryRepository)
    {
        _channelRepository = channelRepository;
        _libraryRepository = libraryRepository;
    }

    public LibraryEntity Library { get; set; } = null!;

    public List<ChannelEntity> Entities { get; set; } = [];

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Library = await _libraryRepository.GetAsync(LibraryId, userId, cancellationToken);
        Entities = await _channelRepository.GetForLibrary(LibraryId, userId, cancellationToken);
    }
}
