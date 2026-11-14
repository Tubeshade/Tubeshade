using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Identity.Stores;
using Tubeshade.Data.Migrations;

namespace Tubeshade.Server.Tests.Integration.Fixtures;

public sealed class ServerFixture : IAsyncDisposable
{
    private readonly PostgreSqlContainer _postgreSqlContainer;
    private readonly List<IContainer> _containers;

    internal string Name { get; }

    internal IServiceProvider Services { get; private set; } = null!;

    internal TestUser User { get; private set; } = null!;

    internal TestUser OtherUser { get; private set; } = null!;

    internal string ConnectionString => $"{_postgreSqlContainer.GetConnectionString()};Include Error Detail=true";

    public ServerFixture(string version)
    {
        Name = version;

        var network = new NetworkBuilder().Build();

        _postgreSqlContainer = new PostgreSqlBuilder($"postgres:{version}")
            .WithTmpfsMount("/var/lib/postgresql/data")
            .WithEnvironment("PGDATA", "/var/lib/postgresql/data")
            .WithNetwork(network)
            .WithHostname("database")
            .WithPortBinding(5432, true)
            .WithCommand("-c", "fsync=off")
            .WithCommand("-c", "synchronous_commit=off")
            .WithCommand("-c", "full_page_writes=off")
            .Build();

        _containers = [_postgreSqlContainer];
    }

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_containers.Select(container => container.StartAsync()));

        Services = CreateServices(ConnectionString);

        await using (var scope = Services.CreateAsyncScope())
        {
            var migrationService = scope.ServiceProvider.GetRequiredService<DatabaseMigrationService>();
            migrationService.Migrate();
        }

        User = await CreateUserAsync(Services, "test");
        OtherUser = await CreateUserAsync(Services, "other");
    }

    internal static IServiceProvider CreateServices(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new("Database:ConnectionString", connectionString)])
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging(builder => builder.AddSimpleConsole())
            .AddDatabase()
            .AddScoped<UserOnlyStore>()
            .BuildServiceProvider(true);
    }

    internal static async Task<TestUser> CreateUserAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<UserOnlyStore>();
        var user = new UserEntity { Id = Guid.NewGuid(), Name = name, NormalizedName = name.ToUpperInvariant() };
        var result = await store.CreateAsync(user, default);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create user {name}");
        }

        var connection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
        await using var transaction = await connection.OpenAndBeginTransaction();
        var ownerId = await scope.ServiceProvider.GetRequiredService<OwnerRepository>().GetPersonalOwnerIdAsync(user.Id, transaction);
        return new TestUser(user.Id, ownerId);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_containers.Select(container => container.StopAsync()));
    }
}
