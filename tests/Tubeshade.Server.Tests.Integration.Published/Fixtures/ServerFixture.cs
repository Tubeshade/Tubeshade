using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace Tubeshade.Server.Tests.Integration.Published.Fixtures;

public sealed partial class ServerFixture : IServerFixture
{
    public const string TestDirectory = "/test";

    private readonly bool _shutdown;
    private readonly INetwork _network;
    private readonly IContainer _serverContainer;
    private readonly List<IContainer> _containers;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Uri BaseAddress => new UriBuilder
    {
        Scheme = "http",
        Host = "localhost",
        Port = _serverContainer.GetMappedPublicPort(),
    }.Uri;

    /// <inheritdoc />
    public HttpClient HttpClient => new() { BaseAddress = BaseAddress };

    public ServerFixture(string name, string serverImageName, string postgresqlVersion, bool shutdown)
    {
        _shutdown = shutdown;
        Name = name;

        _network = new NetworkBuilder().Build();

        var databaseContainer = new PostgreSqlBuilder()
            .WithImage($"postgres:{postgresqlVersion}")
            .WithTmpfsMount("/var/lib/postgresql/data")
            .WithEnvironment("PGDATA", "/var/lib/postgresql/data")
            .WithNetwork(_network)
            .WithHostname("database")
            .WithPortBinding(5432, true)
            .WithCommand("-c", "fsync=off")
            .WithCommand("-c", "synchronous_commit=off")
            .WithCommand("-c", "full_page_writes=off")
            .Build();

        var reportsDirectory = Path.Combine(CommonDirectoryPath.GetProjectDirectory().DirectoryPath, "TestResults");
        if (!Directory.Exists(reportsDirectory))
        {
            Directory.CreateDirectory(reportsDirectory);
        }

        _serverContainer = new ContainerBuilder()
            .WithImage(serverImageName)
            .WithNetwork(_network)
            .WithBindMount(reportsDirectory, "/reports", AccessMode.ReadWrite)
            .WithTmpfsMount(TestDirectory)
            .WithEnvironment(
                "Database__ConnectionString",
                "Host=database; Port=5432; Username=postgres; Password=postgres; Include Error Detail=true")
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait
                .ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(8080)
                .UntilMessageIsLogged(StartedRegex()))
            .DependsOn(databaseContainer)
            .Build();

        _containers = [databaseContainer, _serverContainer];
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await Task.WhenAll(_containers.Select(container => container.StartAsync(cancellationToken)));

        // the release image runs with a non-root user, which does not have write access to mounted paths
        // weirdly, this only is needed in GitHub actions and not locally
        using var client = new DockerClientConfiguration().CreateClient();
        var createResponse = await client.Exec.CreateContainerExecAsync(
            _serverContainer.Id,
            new ContainerExecCreateParameters
            {
                User = "root",
                Cmd = ["chown", "app:app", TestDirectory],
            },
            cancellationToken);

        var stream = await client.Exec.StartContainerExecAsync(createResponse.ID, new ContainerExecStartParameters(), cancellationToken);
        var (output, error) = await stream.ReadOutputToEndAsync(cancellationToken);
        await TestContext.Out.WriteLineAsync(output);
        await TestContext.Out.WriteLineAsync(error);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_shutdown)
        {
            try
            {
                using var httpClient = HttpClient;
                using var response = await httpClient.GetAsync("/api/v1.0/Stop");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            _ = await _serverContainer.GetExitCodeAsync();
        }

        await Task.WhenAll(_containers.Select(container => container.StopAsync()));
        await _network.DeleteAsync();
    }

    [GeneratedRegex(".*Now listening on.*")]
    private static partial Regex StartedRegex();
}
