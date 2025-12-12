using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class LibrarySettings : LibraryPageBase, ISettingsPage
{
    private readonly NpgsqlConnection _connection;
    private readonly LibraryRepository _repository;
    private readonly PreferencesRepository _preferencesRepository;
    private readonly ScheduleRepository _scheduleRepository;
    private readonly IDateTimeZoneProvider _timeZoneProvider;

    public LibrarySettings(
        NpgsqlConnection connection,
        LibraryRepository repository,
        PreferencesRepository preferencesRepository,
        ScheduleRepository scheduleRepository,
        IDateTimeZoneProvider timeZoneProvider)
    {
        _repository = repository;
        _connection = connection;
        _preferencesRepository = preferencesRepository;
        _scheduleRepository = scheduleRepository;
        _timeZoneProvider = timeZoneProvider;
    }

    public LibraryEntity Entity { get; set; } = null!;

    /// <inheritdoc />
    [BindProperty]
    public UpdatePreferencesModel? UpdatePreferencesModel { get; set; }

    [BindProperty]
    public SchedulesModel Schedules { get; set; } = new();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Entity = await _repository.GetAsync(LibraryId, userId, cancellationToken);

        var preferences = await _preferencesRepository.FindForLibrary(LibraryId, userId, cancellationToken);
        var effective = await _preferencesRepository.GetEffectiveForLibrary(LibraryId, userId, cancellationToken) ?? new();
        effective.ApplyDefaults();

        UpdatePreferencesModel ??= new UpdatePreferencesModel(preferences) { Effective = effective };

        var subscriptionSchedule = await _scheduleRepository.GetForTask(userId, LibraryId, TaskType.ScanSubscriptions, Access.Modify, cancellationToken);
        var reindexSchedule = await _scheduleRepository.GetForTask(userId, LibraryId, TaskType.ReindexVideos, Access.Modify, cancellationToken);

        Schedules.Subscription ??= new UpdateScheduleModel(subscriptionSchedule);
        Schedules.Reindex ??= new UpdateScheduleModel(reindexSchedule);

        Schedules.Subscription.TimeZoneIds = _timeZoneProvider.Ids;
        Schedules.Reindex.TimeZoneIds = _timeZoneProvider.Ids;

        return Page();
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnPostUpdatePreferences()
    {
        UpdatePreferencesModel ??= new();
        if (!ModelState.IsValid)
        {
            await OnGet(CancellationToken.None);
            return Page();
        }

        var userId = User.GetUserId();
        var cancellationToken = CancellationToken.None;

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var preferences = await _preferencesRepository.FindForLibrary(LibraryId, userId, transaction);
        if (preferences is null)
        {
            preferences = UpdatePreferencesModel.ToPreferences() with
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
            };

            var id = await _preferencesRepository.AddAsync(preferences, transaction);
            Trace.Assert(id is not null);

            var count = await _preferencesRepository.LinkToLibrary(id.Value, LibraryId, userId, transaction);

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

    public async Task<IActionResult> OnPostUpdateSchedule()
    {
        if (!ModelState.IsValid)
        {
            await OnGet(CancellationToken.None);
            return Page();
        }

        var model = Schedules.GetModel();
        var userId = User.GetUserId();

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var schedule = await _scheduleRepository.GetAsync(model.Id!.Value, userId, transaction);
        schedule.ModifiedByUserId = userId;
        schedule.CronExpression = model.CronExpression;
        schedule.TimeZoneId = model.TimeZoneId;

        var count = await _scheduleRepository.UpdateAsync(schedule, transaction);
        if (count is not 1)
        {
            throw new InvalidOperationException("Failed to update schedule");
        }

        await transaction.CommitAsync();
        return RedirectToPage();
    }

    public sealed class SchedulesModel : IValidatableObject
    {
        public UpdateScheduleModel? Subscription { get; set; }

        public UpdateScheduleModel? Reindex { get; set; }

        /// <inheritdoc />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => this switch
        {
            { Subscription: not null, Reindex: not null } =>
            [
                new ValidationResult(
                    "Only one schedule can be provided",
                    [nameof(Subscription), nameof(Reindex)])
            ],
            _ => [],
        };

        public UpdateScheduleModel GetModel() => this switch
        {
            { Subscription: { } model, Reindex: null } => model,
            { Subscription: null, Reindex: { } model } => model,
            _ => throw new InvalidOperationException("Model is not valid"),
        };
    }
}
