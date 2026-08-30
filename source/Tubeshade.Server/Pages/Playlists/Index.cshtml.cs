using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Htmx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Playlists;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Playlists;

public sealed class Index : PageModel, IPlaylistPage, INonLibraryPage
{
    private readonly NpgsqlConnection _connection;
    private readonly PlaylistRepository _repository;
    private readonly LibraryRepository _libraryRepository;

    public Index(
        NpgsqlConnection connection,
        PlaylistRepository repository,
        LibraryRepository libraryRepository)
    {
        _connection = connection;
        _repository = repository;
        _libraryRepository = libraryRepository;
    }

    /// <inheritdoc />
    public List<LibraryEntity> Libraries { get; private set; } = [];

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public SortPlaylistBy? SortBy { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public SortDirection? SortDirection { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageSize { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageIndex { get; set; }

    /// <inheritdoc />
    public PaginatedData<DetailedPlaylist> PageData { get; private set; } = null!;

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var parameters = this.GetPlaylistParameters(userId, null);

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);

        Libraries = await _libraryRepository.GetAsync(userId, transaction);
        var playlists = await _repository.GetFiltered(parameters, transaction, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var totalCount = playlists is [] ? 0 : playlists[0].TotalCount;
        PageData = new PaginatedData<DetailedPlaylist>
        {
            LibraryId = null,
            Data = playlists,
            Page = PageIndex ?? Defaults.PageIndex,
            PageSize = parameters.Limit,
            TotalCount = totalCount,
        };

        return Request.IsHtmx()
            ? Partial("Playlists/_FilteredPlaylists", this)
            : Page();
    }
}
