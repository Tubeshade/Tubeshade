using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Playlists;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Playlists;

public sealed class Edit : PageModel, IFormLayout, INonLibraryPage
{
    private readonly NpgsqlConnection _connection;
    private readonly PlaylistRepository _repository;
    private readonly LibraryRepository _libraryRepository;
    private readonly TaskRepository _taskRepository;

    public Edit(
        NpgsqlConnection connection,
        PlaylistRepository repository,
        LibraryRepository libraryRepository,
        TaskRepository taskRepository)
    {
        _connection = connection;
        _repository = repository;
        _libraryRepository = libraryRepository;
        _taskRepository = taskRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid PlaylistId { get; set; }

    [BindProperty]
    public EditPlaylistModel? EditPlaylist { get; set; }

    /// <inheritdoc />
    public List<LibraryEntity> Libraries { get; private set; } = [];

    public PlaylistEntity Entity { get; private set; } = null!;

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Libraries = await _libraryRepository.GetAsync(userId, cancellationToken);
        Entity = await _repository.GetAsync(PlaylistId, userId, cancellationToken);

        EditPlaylist ??= new()
        {
            Name = Entity.Name,
        };

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        EditPlaylist ??= new();
        if (!ModelState.IsValid)
        {
            return await OnGet(CancellationToken.None);
        }

        var userId = User.GetUserId();

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var playlist = await _repository.GetAsync(PlaylistId, userId, transaction);
        playlist.ModifiedByUserId = userId;
        playlist.Name = EditPlaylist.Name;

        await _repository.UpdateAsync(playlist, transaction);
        await transaction.CommitAsync();

        return RedirectToPage("/Playlists/Playlist", new { playlistId = PlaylistId });
    }
}
