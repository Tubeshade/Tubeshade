using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Libraries;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Channels;

public sealed class Channel : LibraryPageBase, IPaginatedDataPage<VideoEntity>
{
    private readonly ChannelRepository _channelRepository;
    private readonly VideoRepository _videoRepository;

    public Channel(ChannelRepository channelRepository, VideoRepository videoRepository)
    {
        _channelRepository = channelRepository;
        _videoRepository = videoRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid ChannelId { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageSize { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageIndex { get; set; }

    /// <inheritdoc />
    public PaginatedData<VideoEntity> PageData { get; set; } = null!;

    public ChannelEntity Entity { get; set; } = null!;

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var pageSize = PageSize ?? Defaults.PageSize;
        var page = PageIndex ?? Defaults.PageIndex;
        var offset = pageSize * page;

        Entity = await _channelRepository.GetAsync(ChannelId, userId, cancellationToken);
        var videos = await _videoRepository.GetForChannel(ChannelId, userId, pageSize, offset, cancellationToken);
        var totalCount = videos is [] ? 0 : videos[0].TotalCount;

        PageData = new PaginatedData<VideoEntity>
        {
            LibraryId = LibraryId,
            Data = videos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
