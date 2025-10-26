using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Data.Tasks;
using Tubeshade.Data.Tasks.Payloads;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;
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

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Library = await _libraryRepository.GetAsync(LibraryId, userId, cancellationToken);
        Entity = await _channelRepository.GetAsync(ChannelId, userId, cancellationToken);

        var preferences = await _preferencesRepository.FindForChannel(ChannelId, userId, cancellationToken);
        UpdatePreferencesModel = new UpdatePreferencesModel(preferences);

        return Page();
    }

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

        var payload = new ScanChannelPayload
        {
            LibraryId = LibraryId,
            ChannelId = channelId,
            UserId = userId,
            All = all ?? false,
        };

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var taskId = await _taskRepository.AddScanChannelTask(payload, userId, transaction);
        await _taskRepository.TriggerTask(taskId, userId, transaction);
        await transaction.CommitAsync(cancellationToken);

        return StatusCode(StatusCodes.Status204NoContent);
    }
}
