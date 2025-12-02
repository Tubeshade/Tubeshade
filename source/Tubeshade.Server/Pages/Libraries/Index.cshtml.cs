using System;
using System.Collections.Generic;
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
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class Index : PageModel, INonLibraryPage
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

    /// <inheritdoc />
    public IEnumerable<LibraryEntity> Libraries => Entities;

    public List<LibraryEntity> Entities { get; set; } = [];

    public IEnumerable<string> TimeZoneIds { get; set; } = [];

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        TimeZoneIds = _timeZoneProvider.Ids;
        Entities = await _repository.GetAsync(userId, cancellationToken);

        return Page();
    }

    public async Task<IActionResult> OnPost(AddLibraryModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await OnGet(cancellationToken);
        }

        var userId = User.GetUserId();

        if (!Directory.Exists(model.StoragePath))
        {
            ModelState.AddModelError(nameof(AddLibraryModel.StoragePath), "Directory does not exist");
            return await OnGet(cancellationToken);
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
            return await OnGet(cancellationToken);
        }

        // Once we know that the request is valid we don't want to stop while saving the data,
        // even if the client has disconnected
        cancellationToken = CancellationToken.None;

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);

        var taskId = await _taskRepository.AddAsync(
            new TaskEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = userId,
                Type = TaskType.ScanSubscriptions,
                UserId = userId,
            },
            transaction);
        var task = await _taskRepository.GetAsync(taskId!.Value, userId, transaction);
        var scheduleId = await _scheduleRepository.AddAsync(new ScheduleEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = userId,
                TaskId = task.Id,
                CronExpression = model.CronExpression,
                TimeZoneId = model.TimeZoneId,
            },
            transaction);

        var libraryId = await _repository.AddAsync(new LibraryEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = userId,
                Name = model.Name,
                StoragePath = model.StoragePath,
                SubscriptionsScheduleId = scheduleId!.Value,
            },
            transaction);

        task.LibraryId = libraryId!.Value;
        await _taskRepository.UpdateAsync(task, transaction);

        var rescanTaskId = await _taskRepository.AddAsync(
            new TaskEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = userId,
                Type = TaskType.ReindexVideos,
                UserId = userId,
                LibraryId = libraryId.Value,
            },
            transaction);

        await _scheduleRepository.AddAsync(
            new ScheduleEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = userId,
                TaskId = rescanTaskId!.Value,
                CronExpression = "*/15 * * * *",
                TimeZoneId = model.TimeZoneId
            },
            transaction);

        await transaction.CommitAsync(cancellationToken);

        return RedirectToPage(nameof(Library), new { libraryId });
    }
}
