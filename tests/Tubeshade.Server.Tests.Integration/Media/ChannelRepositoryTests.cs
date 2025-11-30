using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NUnit.Framework;
using Tubeshade.Data;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Media;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Media;

public sealed class ChannelRepositoryTests(ServerFixture fixture) : ServerTests(fixture)
{
    private Guid _userId;
    private Guid _libraryId;
    private Guid _libraryId2;
    private Guid _channelId;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var userRepository = scope.ServiceProvider.GetRequiredService<UserRepository>();
            _userId = await userRepository.GetSystemUserId(transaction);
            await transaction.CommitAsync();
        }

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            _libraryId = await CreateLibrary(_userId, scope.ServiceProvider, transaction);
            _libraryId2 = await CreateLibrary(_userId, scope.ServiceProvider, transaction);

            var channelRepository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();
            _channelId = (await channelRepository.AddAsync(
                new()
                {
                    CreatedByUserId = _userId,
                    ModifiedByUserId = _userId,
                    OwnerId = _userId,
                    Name = Guid.NewGuid().ToString("N"),
                    StoragePath = string.Empty,
                    ExternalId = string.Empty,
                    ExternalUrl = string.Empty,
                    Availability = ExternalAvailability.Public,
                },
                transaction))!.Value;

            await channelRepository.AddToLibrary(_libraryId, _channelId, transaction);

            await transaction.CommitAsync();
        }
    }

    [Test]
    public async Task MoveToLibrary()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();

        var primaryLibraryId = await repository.GetPrimaryLibraryId(_channelId);
        primaryLibraryId.Should().Be(_libraryId);

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var count = await repository.MoveToLibrary(_libraryId2, _channelId, _userId, transaction);
            await transaction.CommitAsync();

            count.Should().Be(1);
        }

        primaryLibraryId = await repository.GetPrimaryLibraryId(_channelId);
        primaryLibraryId.Should().Be(_libraryId2);
    }
}
