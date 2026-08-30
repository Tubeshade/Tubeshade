using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Pages.Playlists;

public sealed class Create : PageModel, IFormLayout, INonLibraryPage
{
    private readonly LibraryRepository _libraryRepository;
    private readonly PlaylistService _playlistService;

    public Create(LibraryRepository libraryRepository, PlaylistService playlistService)
    {
        _libraryRepository = libraryRepository;
        _playlistService = playlistService;
    }

    [BindProperty]
    public CreatePlaylistModel? CreatePlaylist { get; set; }

    /// <inheritdoc />
    public List<LibraryEntity> Libraries { get; private set; } = [];

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        Libraries = await _libraryRepository.GetAsync(User.GetUserId(), cancellationToken);
        CreatePlaylist ??= new();
        CreatePlaylist.Libraries = Libraries;

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        CreatePlaylist ??= new();
        if (!ModelState.IsValid)
        {
            return await OnGet(CancellationToken.None);
        }

        var libraryId = CreatePlaylist.LibraryId;
        var playlist = await _playlistService.Create(
            libraryId,
            User.GetUserId(),
            CreatePlaylist.Name,
            CreatePlaylist.ExternalId,
            CreatePlaylist.ExternalUrl);

        return RedirectToPage("/Playlists/Playlist", new { playlistId = playlist.Id });
    }
}
