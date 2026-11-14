using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NUnit.Framework;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Server.Tests.Integration.Fixtures;

namespace Tubeshade.Server.Tests.Integration.Media;

public sealed class ChannelRepositoryTests(ServerFixture fixture) : ServerTests(fixture)
{
    private Guid _libraryId;
    private Guid _libraryId2;
    private Guid _libraryId3;
    private Guid _channelId;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();


        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            _libraryId = await CreateLibrary(User.Id, scope.ServiceProvider, transaction);
            _libraryId2 = await CreateLibrary(User.Id, scope.ServiceProvider, transaction);
            _libraryId3 = await CreateLibrary(Guid.NewGuid().ToString(), User.Id, scope.ServiceProvider, transaction);

            var channelRepository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();
            _channelId = (await channelRepository.AddAsync(
                new()
                {
                    CreatedByUserId = User.Id,
                    ModifiedByUserId = User.Id,
                    OwnerId = User.OwnerId,
                    Name = Guid.NewGuid().ToString("N"),
                    StoragePath = string.Empty,
                    ExternalId = string.Empty,
                    ExternalUrl = string.Empty,
                    Availability = ExternalAvailability.Public,
                },
                transaction))!.Value;

            await channelRepository.AddToLibrary(_libraryId, _channelId, User.Id, transaction);

            await transaction.CommitAsync();
        }
    }

    [Test]
    public async Task MoveToLibrary()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();

        (await repository.GetPrimaryLibraryId(_channelId, User.Id)).Should().Be(_libraryId);

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var count = await repository.MoveToLibrary(_libraryId2, _channelId, User.Id, transaction);
            await transaction.CommitAsync();

            count.Should().Be(1);
        }

        (await repository.GetPrimaryLibraryId(_channelId, User.Id)).Should().Be(_libraryId2);

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var count = await repository.MoveToLibrary(_libraryId, _channelId, User.Id, transaction);
            await transaction.CommitAsync();

            count.Should().Be(1);
        }

        (await repository.GetPrimaryLibraryId(_channelId, User.Id)).Should().Be(_libraryId);
    }

    [Test]
    public async Task MoveOnlyIfStoragePathMatches()
    {
        await using var scope = Fixture.Services.CreateAsyncScope();
        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        var repository = scope.ServiceProvider.GetRequiredService<ChannelRepository>();

        var currentId = await repository.GetPrimaryLibraryId(_channelId, User.Id);

        await using (var transaction = await connection.OpenAndBeginTransaction())
        {
            var count = await repository.MoveToLibrary(_libraryId3, _channelId, User.Id, transaction);
            await transaction.CommitAsync();

            count.Should().Be(0);
        }

        (await repository.GetPrimaryLibraryId(_channelId, User.Id)).Should().Be(currentId);
    }
}
