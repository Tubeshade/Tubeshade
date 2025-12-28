using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NodaTime;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Pages.Videos;

public sealed class Create : PageModel, INonLibraryPage, IFormLayout
{
    private readonly IDateTimeZoneProvider _timeZoneProvider;
    private readonly IClock _clock;
    private readonly NpgsqlConnection _connection;
    private readonly LibraryRepository _libraryRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly VideoService _videoService;

    public Create(
        IDateTimeZoneProvider timeZoneProvider,
        IClock clock,
        NpgsqlConnection connection,
        LibraryRepository libraryRepository,
        ChannelRepository channelRepository,
        VideoService videoService)
    {
        _timeZoneProvider = timeZoneProvider;
        _clock = clock;
        _connection = connection;
        _libraryRepository = libraryRepository;
        _channelRepository = channelRepository;
        _videoService = videoService;
    }

    /// <inheritdoc />
    public List<LibraryEntity> Libraries { get; private set;  } = [];

    [BindProperty]
    public CreateVideoModel? CreateVideo { get; set; }

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Libraries = await _libraryRepository.GetAsync(userId, cancellationToken);

        CreateVideo ??= new();
        CreateVideo.Channels = await _channelRepository.GetAsync(userId, cancellationToken);
        CreateVideo.TimeZoneIds = _timeZoneProvider.Ids;

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        CreateVideo ??= new();
        if (!ModelState.IsValid)
        {
            return await OnGet(CancellationToken.None);
        }

        var userId = User.GetUserId();

        await using var transaction = await _connection.OpenAndBeginTransaction();

        var channel = await _channelRepository.GetAsync(CreateVideo.ChannelId!.Value, userId, transaction);
        var libraryId = await _channelRepository.GetPrimaryLibraryId(channel.Id, transaction);
        var library = await _libraryRepository.GetAsync(libraryId, userId, transaction);

        var timeZone = _timeZoneProvider.GetZoneOrNull(CreateVideo.PublishedAtTimeZone) ??
                       throw new("Invalid time zone id");

        var publishedAt = CreateVideo.PublishedAt!.Value.InZoneStrictly(timeZone).ToInstant();
        var duration = Period.FromTicks((long)Duration.FromSeconds((double)CreateVideo.DurationInSeconds!.Value).TotalTicks);

        var video = await _videoService.Create(
            userId,
            channel,
            library.OwnerId,
            CreateVideo.Name,
            CreateVideo.Description ?? string.Empty,
            CreateVideo.Categories?.Split(',') ?? [],
            CreateVideo.Tags?.Split(',') ?? [],
            CreateVideo.Type,
            CreateVideo.ExternalId,
            CreateVideo.ExternalUrl,
            publishedAt,
            _clock.GetCurrentInstant(),
            CreateVideo.Availability,
            duration,
            null,
            null,
            transaction);

        await transaction.CommitAsync();

        return RedirectToPage("/Libraries/Videos/Video", new { libraryId, videoId = video.Id });
    }
}
