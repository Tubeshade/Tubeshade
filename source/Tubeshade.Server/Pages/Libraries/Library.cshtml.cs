using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class Library : LibraryPageBase, IPaginatedDataPage<VideoModel>
{
    private readonly NpgsqlConnection _connection;
    private readonly LibraryRepository _repository;
    private readonly VideoRepository _videoRepository;
    private readonly ChannelRepository _channelRepository;

    public Library(LibraryRepository repository, VideoRepository videoRepository, NpgsqlConnection connection, ChannelRepository channelRepository)
    {
        _repository = repository;
        _videoRepository = videoRepository;
        _connection = connection;
        _channelRepository = channelRepository;
    }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageSize { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageIndex { get; set; }

    /// <inheritdoc />
    public PaginatedData<VideoModel> PageData { get; set; } = null!;

    public LibraryEntity Entity { get; set; } = null!;

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var pageSize = PageSize ?? 20;
        var page = PageIndex ?? 0;
        var offset = pageSize * page;

        Entity = await _repository.GetAsync(LibraryId, userId, cancellationToken);
        var videos = await _videoRepository.GetLibraryVideosAsync(
            LibraryId,
            userId,
            pageSize,
            offset,
            cancellationToken);

        var channels = await _channelRepository.GetAsync(userId, cancellationToken);
        var models = videos.Select(video => new VideoModel
        {
            Video = video,
            Channel = channels.Single(channel => video.ChannelId == channel.Id), // todo
        }).ToList();

        var totalCount = videos is [] ? 0 : videos[0].TotalCount;

        PageData = new PaginatedData<VideoModel>
        {
            LibraryId = LibraryId,
            Data = models,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<IActionResult> OnPostUpdatePreferences(UpdatePreferencesModel model)
    {
        if (!ModelState.IsValid)
        {
            await OnGet(CancellationToken.None);
            return Page();
        }

        await using var transaction = await _connection.OpenAndBeginTransaction();
        return RedirectToPage();
    }
}
