using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class LibrarySettings : LibraryPageBase, ISettingsPage
{
    private readonly NpgsqlConnection _connection;
    private readonly LibraryRepository _repository;
    private readonly PreferencesRepository _preferencesRepository;

    public LibrarySettings(
        LibraryRepository repository,
        NpgsqlConnection connection,
        PreferencesRepository preferencesRepository)
    {
        _repository = repository;
        _connection = connection;
        _preferencesRepository = preferencesRepository;
    }

    public LibraryEntity Entity { get; set; } = null!;

    [BindProperty]
    public UpdatePreferencesModel UpdatePreferencesModel { get; set; } = new();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Entity = await _repository.GetAsync(LibraryId, userId, cancellationToken);

        var preferences = await _preferencesRepository.FindForLibrary(LibraryId, userId, cancellationToken);
        UpdatePreferencesModel = new UpdatePreferencesModel
        {
            PlaybackSpeed = preferences?.PlaybackSpeed,
            VideosCount = preferences?.VideosCount,
            LiveStreamsCount = preferences?.LiveStreamsCount,
            ShortsCount = preferences?.ShortsCount,
            PlayerClient = preferences?.PlayerClient?.Name,
            DownloadAutomatically = preferences?.DownloadAutomatically,
        };

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
        var client = !string.IsNullOrWhiteSpace(UpdatePreferencesModel.PlayerClient)
            ? PlayerClient.FromName(UpdatePreferencesModel.PlayerClient, true)
            : null;

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var preferences = await _preferencesRepository.FindForLibrary(LibraryId, userId, transaction);
        if (preferences is null)
        {
            var id = await _preferencesRepository.AddAsync(
                new PreferencesEntity
                {
                    CreatedByUserId = userId,
                    ModifiedByUserId = userId,
                    PlaybackSpeed = UpdatePreferencesModel.PlaybackSpeed,
                    VideosCount = UpdatePreferencesModel.VideosCount,
                    LiveStreamsCount = UpdatePreferencesModel.LiveStreamsCount,
                    ShortsCount = UpdatePreferencesModel.ShortsCount,
                    SubscriptionScheduleId = null,
                    PlayerClient = client,
                    DownloadAutomatically = preferences?.DownloadAutomatically,
                },
                transaction);

            Trace.Assert(id is not null);

            var count = await _preferencesRepository.LinkToLibrary(id.Value, LibraryId, userId, transaction);

            Trace.Assert(count is 1);
        }
        else
        {
            preferences.PlaybackSpeed = UpdatePreferencesModel.PlaybackSpeed;
            preferences.VideosCount = UpdatePreferencesModel.VideosCount;
            preferences.LiveStreamsCount = UpdatePreferencesModel.LiveStreamsCount;
            preferences.ShortsCount = UpdatePreferencesModel.ShortsCount;
            preferences.PlayerClient = client;
            preferences.DownloadAutomatically = UpdatePreferencesModel.DownloadAutomatically;

            var count = await _preferencesRepository.UpdateAsync(
                preferences,
                transaction);

            Trace.Assert(count is 1);
        }

        await transaction.CommitAsync(cancellationToken);

        return RedirectToPage();
    }
}
