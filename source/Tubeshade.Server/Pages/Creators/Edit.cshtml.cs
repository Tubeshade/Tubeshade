using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Data.Media.Creators;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Pages.Creators;

public sealed class Edit : PageModel, IFormLayout, INonLibraryPage
{
    private readonly NpgsqlConnection _connection;
    private readonly CreatorRepository _repository;
    private readonly LibraryRepository _libraryRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly SubscriptionsService _subscriptionsService;
    private readonly TaskRepository _taskRepository;

    public Edit(
        NpgsqlConnection connection,
        CreatorRepository repository,
        LibraryRepository libraryRepository,
        ChannelRepository channelRepository,
        SubscriptionsService subscriptionsService,
        TaskRepository taskRepository)
    {
        _connection = connection;
        _repository = repository;
        _libraryRepository = libraryRepository;
        _channelRepository = channelRepository;
        _subscriptionsService = subscriptionsService;
        _taskRepository = taskRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid CreatorId { get; set; }

    [BindProperty]
    public EditCreatorModel? EditCreator { get; set; }

    /// <inheritdoc />
    public List<LibraryEntity> Libraries { get; private set; } = [];

    public CreatorEntity Entity { get; private set; } = null!;

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Libraries = await _libraryRepository.GetAsync(userId, cancellationToken);
        Entity = await _repository.GetAsync(CreatorId, userId, cancellationToken);
        var x = await _repository.GetChannels(CreatorId, userId, cancellationToken);
        var channels = await _channelRepository.GetAsync(userId, cancellationToken);

        EditCreator ??= new()
        {
            Name = Entity.Name,
            PrimaryChannelId = x.Single(y => y.Primary).ChannelId,
            ChannelIds = x.Where(y => !y.Primary).Select(y => y.ChannelId).ToArray(),
            Channels = channels,
        };

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        EditCreator ??= new();
        if (!ModelState.IsValid)
        {
            return await OnGet(CancellationToken.None);
        }

        if (EditCreator is not { Name: { } name, PrimaryChannelId: { } channelId })
        {
            throw new InvalidOperationException("Model does not contain required values after validation");
        }

        var userId = User.GetUserId();

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var creator = await _repository.GetAsync(CreatorId, userId, transaction);
        creator.ModifiedByUserId = userId;
        creator.Name = name;

        await _repository.UpdateAsync(creator, transaction);

        await _repository.UpdateChannels(CreatorId, channelId, EditCreator.ChannelIds ?? [], userId, transaction);
        await transaction.CommitAsync();

        return RedirectToPage("/Creators/Creator", new { CreatorId });
    }
}
