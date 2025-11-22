using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Startup;

namespace Tubeshade.Server.Services.Background;

public sealed class FileHashMigrationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DatabaseMigrationStartupFilter _migrationStartupFilter;
    private readonly ILogger<FileHashMigrationService> _logger;
    private readonly IClock _clock;

    public FileHashMigrationService(
        ILogger<FileHashMigrationService> logger,
        IServiceProvider serviceProvider,
        DatabaseMigrationStartupFilter migrationStartupFilter,
        IClock clock)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _clock = clock;
        _migrationStartupFilter = migrationStartupFilter;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _migrationStartupFilter.MigrationTask;

        await using var scope = _serviceProvider.CreateAsyncScope();

        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        Guid systemUserId;

        await using (var transaction = await connection.OpenAndBeginTransaction(stoppingToken))
        {
            var userRepository = scope.ServiceProvider.GetRequiredService<UserRepository>();
            systemUserId = await userRepository.GetSystemUserId(transaction);
        }

        var videoRepository = scope.ServiceProvider.GetRequiredService<VideoRepository>();
        var videoFileRepository = scope.ServiceProvider.GetRequiredService<VideoFileRepository>();

        var hashAlgorithm = HashAlgorithm.Default;

        foreach (var videoFile in await videoFileRepository.GetUnsafeAsync(stoppingToken))
        {
            await using var transaction = await connection.OpenAndBeginTransaction(stoppingToken);

            var file = await videoFileRepository.GetUnsafeAsync(videoFile.Id, transaction);
            if (file.DownloadedAt is null ||
                (file.HashAlgorithm is not null && file.HashAlgorithm != HashAlgorithm.Placeholder))
            {
                _logger.SkippingVideoFile(file.Id);
                continue;
            }

            var video = await videoRepository.GetUnsafeAsync(file.VideoId, transaction);
            var path = Path.Combine(video.StoragePath, file.StoragePath);

            _logger.ReplacingVideoPlaceholderHash(hashAlgorithm.Name, file.Id, path);
            var hashData = await hashAlgorithm.ComputeHashAsync(path, stoppingToken);

            file.ModifiedAt = _clock.GetCurrentInstant();
            file.ModifiedByUserId = systemUserId;
            file.Hash = hashData;
            file.HashAlgorithm = hashAlgorithm;

            var count = await videoFileRepository.UpdateUnsafeAsync(file, transaction);
            Trace.Assert(count is 1);
            await transaction.CommitAsync(stoppingToken);
        }

        var imageFileRepository = scope.ServiceProvider.GetRequiredService<ImageFileRepository>();

        foreach (var imageFile in await imageFileRepository.GetUnsafeAsync(stoppingToken))
        {
            await using var transaction = await connection.OpenAndBeginTransaction(stoppingToken);

            var file = await imageFileRepository.GetUnsafeAsync(imageFile.Id, transaction);
            var basePath = await imageFileRepository.FindBasePathUnsafe(file.Id, transaction);
            if (file.HashAlgorithm != HashAlgorithm.Placeholder || basePath is null)
            {
                _logger.SkippingImageFile(file.Id);
                continue;
            }

            var path = Path.Combine(basePath, file.StoragePath);

            _logger.ReplacingImagePlaceholderHash(hashAlgorithm.Name, file.Id, path);
            var hashData = await hashAlgorithm.ComputeHashAsync(path, stoppingToken);

            await imageFileRepository.UpdateHashAsync(file.Id, hashData, hashAlgorithm, transaction);
            await transaction.CommitAsync(stoppingToken);
        }
    }
}
