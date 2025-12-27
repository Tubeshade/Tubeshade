using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;

namespace Tubeshade.Server.Services;

public sealed class ChannelService
{
    private readonly ILogger<ChannelService> _logger;
    private readonly NpgsqlConnection _connection;
    private readonly LibraryRepository _libraryRepository;
    private readonly ChannelRepository _channelRepository;

    public ChannelService(
        ILogger<ChannelService> logger,
        NpgsqlConnection connection,
        LibraryRepository libraryRepository,
        ChannelRepository channelRepository)
    {
        _logger = logger;
        _connection = connection;
        _libraryRepository = libraryRepository;
        _channelRepository = channelRepository;
    }

    public async ValueTask<ChannelEntity> Create(
        Guid libraryId,
        Guid userId,
        string name,
        string externalId,
        string externalUrl,
        ExternalAvailability availability)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction();
        var channel = await Create(libraryId, userId, name, externalId, externalUrl, availability, transaction);
        await transaction.CommitAsync();

        return channel;
    }

    public async ValueTask<ChannelEntity> Create(
        Guid libraryId,
        Guid userId,
        string name,
        string externalId,
        string externalUrl,
        ExternalAvailability availability,
        NpgsqlTransaction transaction)
    {
        var library = await _libraryRepository.GetAsync(libraryId, userId, transaction);
        _logger.CreatingChannel(name, externalId);

        var channelId = await _channelRepository.AddAsync(
            new ChannelEntity
            {
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                OwnerId = library.OwnerId,
                Name = name,
                StoragePath = library.StoragePath,
                ExternalId = externalId,
                ExternalUrl = externalUrl,
                Availability = availability,
            },
            transaction);

        var channel = await _channelRepository.GetAsync(channelId!.Value, userId, transaction);

        channel.StoragePath = Path.Combine(library.StoragePath, $"channel_{channel.Id}");
        await _channelRepository.UpdateAsync(channel, transaction);
        await _channelRepository.AddToLibrary(libraryId, channel.Id, transaction);

        Directory.CreateDirectory(channel.StoragePath);

        return channel;
    }
}
