using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Data.Media.Creators;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Creators;

public sealed class Create : PageModel, IFormLayout, INonLibraryPage
{
    private readonly NpgsqlConnection _connection;
    private readonly CreatorRepository _repository;
    private readonly LibraryRepository _libraryRepository;
    private readonly ChannelRepository _channelRepository;

    public Create(
        NpgsqlConnection connection,
        CreatorRepository repository,
        LibraryRepository libraryRepository,
        ChannelRepository channelRepository)
    {
        _connection = connection;
        _repository = repository;
        _libraryRepository = libraryRepository;
        _channelRepository = channelRepository;
    }

    [BindProperty]
    public CreateCreatorModel? CreateCreator { get; set; }

    /// <inheritdoc />
    public List<LibraryEntity> Libraries { get; private set; } = [];

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Libraries = await _libraryRepository.GetAsync(userId, cancellationToken);
        var channels = await _channelRepository.GetAsync(userId, cancellationToken);

        CreateCreator ??= new() { Channels = channels };

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        CreateCreator ??= new();
        if (!ModelState.IsValid)
        {
            return await OnGet(CancellationToken.None);
        }

        if (CreateCreator is not { Name: { } name, PrimaryChannelId: { } channelId })
        {
            throw new InvalidOperationException("Model does not contain required values after validation");
        }

        var userId = User.GetUserId();

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var creator = new CreatorEntity
        {
            CreatedByUserId = userId,
            ModifiedByUserId = userId,
            OwnerId = userId,
            Name = name,
        };

        var id = await _repository.AddAsync(creator, transaction);
        if (id is not { } creatorId)
        {
            throw new InvalidOperationException("Missing access to create a creator");
        }

        await _repository.UpdateChannels(creatorId, channelId, [], userId, transaction);
        await transaction.CommitAsync();

        return RedirectToPage("/Creators/Creator", new { creatorId });
    }
}
