using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Tubeshade.Data;
using Tubeshade.Data.Migrations;

namespace Tubeshade.Server.Tests.Integration.Fixtures;

public sealed class ServerFixture : IAsyncDisposable
{
    private readonly PostgreSqlContainer _postgreSqlContainer;
    private readonly List<IContainer> _containers;

    internal string Name { get; }

    internal IServiceProvider Services { get; private set; } = null!;

    public ServerFixture(string version)
    {
        Name = version;

        var network = new NetworkBuilder().Build();

        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage($"postgres:{version}")
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

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new("Database:ConnectionString", _postgreSqlContainer.GetConnectionString())])
            .Build();

        Services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging(builder => builder.AddSimpleConsole())
            .AddDatabase()
            .BuildServiceProvider(true);

        await using var scope = Services.CreateAsyncScope();
        var migrationService = scope.ServiceProvider.GetRequiredService<DatabaseMigrationService>();
        migrationService.Migrate();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_containers.Select(container => container.StopAsync()));
    }
}
