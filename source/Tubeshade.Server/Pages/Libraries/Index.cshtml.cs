using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class Index : PageModel
{
    private readonly ILogger<Index> _logger;
    private readonly NpgsqlConnection _connection;
    private readonly LibraryRepository _repository;

    public Index(LibraryRepository repository, NpgsqlConnection connection, ILogger<Index> logger)
    {
        _repository = repository;
        _connection = connection;
        _logger = logger;
    }

    public List<LibraryEntity> Entities { get; set; } = [];

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Entities = await _repository.GetAsync(userId, cancellationToken);
    }

    public async Task<IActionResult> OnPost(AddLibraryModel model, CancellationToken cancellationToken)
    {
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
                ModifiedAt = default,
                ModifiedByUserId = userId,
                OwnerId = userId,
                Name = model.Name,
                StoragePath = model.StoragePath,
            },
            transaction);

        await transaction.CommitAsync(cancellationToken);

        return RedirectToPage(nameof(Library), new { libraryId = id });
    }
}
