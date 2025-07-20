using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
using Tubeshade.Data.Tasks.Payloads;
using Tubeshade.Server.Configuration.Auth;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class Index : PageModel
{
    private readonly ILogger<Index> _logger;
    private readonly NpgsqlConnection _connection;
    private readonly LibraryRepository _repository;
    private readonly TaskRepository _taskRepository;
    private readonly ScheduleRepository _scheduleRepository;
    private readonly IDateTimeZoneProvider _timeZoneProvider;

    public Index(
        ILogger<Index> logger,
        NpgsqlConnection connection,
        LibraryRepository repository,
        TaskRepository taskRepository,
        ScheduleRepository scheduleRepository,
        IDateTimeZoneProvider timeZoneProvider)
    {
        _repository = repository;
        _connection = connection;
        _logger = logger;
        _taskRepository = taskRepository;
        _scheduleRepository = scheduleRepository;
        _timeZoneProvider = timeZoneProvider;
    }

    public List<LibraryEntity> Entities { get; set; } = [];

    public IEnumerable<string> TimeZoneIds { get; set; } = [];

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        TimeZoneIds = _timeZoneProvider.Ids;
        Entities = await _repository.GetAsync(userId, cancellationToken);
    }

    public async Task<IActionResult> OnPost(AddLibraryModel model, CancellationToken cancellationToken)
    {
        if (!_timeZoneProvider.Ids.Contains(model.TimeZoneId))
        {
            ModelState.AddModelError(nameof(model.TimeZoneId), "Invalid time zone id");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = User.GetUserId();

        if (!Directory.Exists(model.StoragePath))
        {
            ModelState.AddModelError(nameof(AddLibraryModel.StoragePath), "Directory does not exist");
            await OnGet(cancellationToken);
            return Page();
        }

        try
        {
            var testFilePath = Path.Combine(model.StoragePath, Guid.NewGuid().ToString("D"));
            await using (System.IO.File.Create(testFilePath))
            {
            }

            System.IO.File.Delete(testFilePath);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not write to library storage path");
            ModelState.AddModelError(nameof(AddLibraryModel.StoragePath), "Could not create a file in directory");
            await OnGet(cancellationToken);
            return Page();
        }

        // Once we know that the request is valid we don't want to stop while saving the data,
        // even if the client has disconnected
        cancellationToken = CancellationToken.None;

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var id = await _repository.AddAsync(new LibraryEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = userId,
                Name = model.Name,
                StoragePath = model.StoragePath,
            },
            transaction);

        var payload = new ScanSubscriptionsPayload { LibraryId = id!.Value, UserId = userId };
        var taskId = await _taskRepository.AddScanSubscriptionsTask(payload, userId, transaction);
        var scheduleId = await _scheduleRepository.AddAsync(new ScheduleEntity
            {
                ModifiedAt = default,
                ModifiedByUserId = userId,
                TaskId = taskId,
                CronExpression = model.CronExpression,
                TimeZoneId = model.TimeZoneId,
            },
            transaction);

        Trace.Assert(scheduleId is not null);
        await transaction.CommitAsync(cancellationToken);

        return RedirectToPage(nameof(Library), new { libraryId = id });
    }
}
