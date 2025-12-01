using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Resources;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Pages.Libraries.Channels;

public sealed class ChannelSettings : LibraryPageBase, ISettingsPage
{
    private readonly NpgsqlConnection _connection;
    private readonly LibraryRepository _libraryRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly TaskRepository _taskRepository;
    private readonly PreferencesRepository _preferencesRepository;
    private readonly SubscriptionsService _subscriptionsService;

    public ChannelSettings(
        ChannelRepository channelRepository,
        LibraryRepository libraryRepository,
        NpgsqlConnection connection,
        TaskRepository taskRepository,
        PreferencesRepository preferencesRepository,
        SubscriptionsService subscriptionsService)
    {
        _channelRepository = channelRepository;
        _libraryRepository = libraryRepository;
        _connection = connection;
        _taskRepository = taskRepository;
        _preferencesRepository = preferencesRepository;
        _subscriptionsService = subscriptionsService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid ChannelId { get; set; }

    public LibraryEntity Library { get; set; } = null!;

    public ChannelEntity Entity { get; set; } = null!;

    [BindProperty]
    public UpdatePreferencesModel UpdatePreferencesModel { get; set; } = new();

    [BindProperty]
    public Guid? NewLibraryId { get; set; }

    public List<LibraryEntity> Libraries { get; set; } = [];

    public List<LibraryEntity> OtherLibraries { get; set; } = [];

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Libraries = await _libraryRepository.GetAsync(userId, cancellationToken);
        Library = Libraries.Single(library => library.Id == LibraryId);
        OtherLibraries = Libraries
            .Except([Library])
            .Where(library => library.StoragePath == Library.StoragePath)
            .ToList();

        Entity = await _channelRepository.GetAsync(ChannelId, userId, cancellationToken);

        var preferences = await _preferencesRepository.FindForChannel(ChannelId, userId, cancellationToken);
        UpdatePreferencesModel = new UpdatePreferencesModel(preferences);

        return Page();
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnPostUpdatePreferences()
    {
        if (!ModelState.IsValid)
        {
            await OnGet(CancellationToken.None);
            return Page();
        }

        var userId = User.GetUserId();
        var cancellationToken = CancellationToken.None;

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var preferences = await _preferencesRepository.FindForChannel(ChannelId, userId, transaction);
        if (preferences is null)
        {
            preferences = UpdatePreferencesModel.ToPreferences() with
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
            };

            var id = await _preferencesRepository.AddAsync(preferences, transaction);
            Trace.Assert(id is not null);

            var count = await _preferencesRepository.LinkToChannel(id.Value, ChannelId, userId, transaction);
            Trace.Assert(count is 1);
        }
        else
        {
            UpdatePreferencesModel.UpdatePreferences(preferences);
            preferences.ModifiedByUserId = userId;

            var count = await _preferencesRepository.UpdateAsync(preferences, transaction);
            Trace.Assert(count is 1);
        }

        await transaction.CommitAsync(cancellationToken);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnsubscribe()
    {
        var userId = User.GetUserId();
        var channel =  await _subscriptionsService.Unsubscribe(ChannelId, userId);
        return Partial("_Subscribe", channel);
    }

    public async Task<IActionResult> OnPostSubscribe()
    {
        var userId = User.GetUserId();
        var channel = await _subscriptionsService.Subscribe(ChannelId, userId);
        return Partial("_Subscribe", channel);
    }

    public async Task<IActionResult> OnPostScan(Guid channelId, bool? all)
    {
        var userId = User.GetUserId();
        var cancellationToken = CancellationToken.None;

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var taskId = await _taskRepository.AddScanChannelTask(LibraryId, channelId, all ?? false, userId, transaction);
        await _taskRepository.TriggerTask(taskId, userId, transaction);
        await transaction.CommitAsync(cancellationToken);

        return StatusCode(StatusCodes.Status204NoContent);
    }

    public async Task<IActionResult> OnPostChangeLibrary()
    {
        var userId = User.GetUserId();
        var cancellationToken = CancellationToken.None;

        if (NewLibraryId is not { } targetLibraryId)
        {
            ModelState.AddModelError(nameof(NewLibraryId), ValidationMessages.RequiredField);
            return await OnGet(cancellationToken);
        }

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var count = await _channelRepository.MoveToLibrary(targetLibraryId, ChannelId, userId, transaction);
        await transaction.CommitAsync(cancellationToken);

        if (count is not 0)
        {
            return RedirectToPage(nameof(ChannelSettings), new { libraryId = NewLibraryId, ChannelId });
        }

        ModelState.AddModelError(nameof(NewLibraryId), ValidationMessages.MissingModifyAccess);
        return await OnGet(cancellationToken);
    }
}
